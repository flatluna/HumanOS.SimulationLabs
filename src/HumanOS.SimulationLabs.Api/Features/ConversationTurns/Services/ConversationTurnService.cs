using HumanOS.SimulationLabs.Api.Features.ConversationTurns.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.ConversationTurns.Services;

public sealed class ConversationTurnService : IConversationTurnService
{
    private readonly SimulationLabsDbContext _db;
    public ConversationTurnService(SimulationLabsDbContext db) => _db = db;

    public async Task<ConversationTurnResponse> CreateAsync(Guid tenantId, Guid attemptId, string user, CreateConversationTurnRequest request, CancellationToken ct)
    {
        var attempt = await _db.Attempts.AsNoTracking().FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct) ?? throw new ConversationTurnAttemptNotFoundException();
        if (attempt.ATT_Estatus is not (AttemptEstatus.InProgress or AttemptEstatus.Paused)) throw new ConversationTurnAttemptNotEditableException();

        var speakerType = request.SpeakerType!.Trim().ToUpperInvariant();
        if (speakerType == TurnSpeakerType.SimulatedActor)
        {
            var actorExists = await _db.SimulatedActors.AnyAsync(a => a.SEG_IdTenant == tenantId && a.ACT_IdActor == request.ActorId!.Value && a.SCN_IdScenario == attempt.SCN_IdScenario, ct);
            if (!actorExists) throw new ActorScenarioMismatchException();
        }
        if (request.IdExpectedMoment is { } momentId)
        {
            var momentExists = await _db.ExpectedMoments.AnyAsync(m => m.SEG_IdTenant == tenantId && m.MOM_IdExpectedMoment == momentId && m.LAB_IdVersion == attempt.LAB_IdVersion, ct);
            if (!momentExists) throw new ExpectedMomentVersionMismatchException();
        }

        var turnNumber = (await _db.ConversationTurns.Where(t => t.SEG_IdTenant == tenantId && t.ATT_IdAttempt == attemptId).MaxAsync(t => (int?)t.TRN_NumeroTurno, ct) ?? 0) + 1;
        var now = DateTimeOffset.UtcNow;
        var turn = new LAB_ConversationTurn
        {
            TRN_IdTurn = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            ATT_IdAttempt = attemptId,
            LAB_IdVersion = attempt.LAB_IdVersion,
            TRN_NumeroTurno = turnNumber,
            TRN_SpeakerType = speakerType,
            ACT_IdActor = request.ActorId,
            TRN_Texto = request.Texto!.Trim(),
            TRN_TipoEntrada = request.TipoEntrada!.Trim().ToUpperInvariant(),
            TRN_FechaInicio = request.FechaInicio ?? now,
            TRN_FechaFin = request.FechaFin,
            TRN_DuracionMs = request.DuracionMs,
            TRN_ConfianzaTranscripcion = request.ConfianzaTranscripcion,
            TRN_FueInterrumpido = request.FueInterrumpido ?? false,
            TRN_InterrumpioTurnoAnterior = request.InterrumpioTurnoAnterior ?? false,
            TRN_EsRespuesta = request.EsRespuesta ?? false,
            TRN_IdTurnoRespondido = request.IdTurnoRespondido,
            TRN_IdExpectedMoment = request.IdExpectedMoment,
            TRN_Estatus = TurnEstatus.Final,
            FechaCreacion = now,
            CreadoPor = user,
        };

        // Live realtime turns from PARTICIPANT and SIMULATED_ACTOR can arrive within
        // milliseconds of each other, so a plain MAX+1 read-then-write races under
        // concurrent requests and can violate the (tenant, attempt, numero) unique index.
        // Retry with a freshly recomputed number, with jitter so racing requests desync.
        const int maxAttempts = 10;
        for (var attemptNumber = 1; ; attemptNumber++)
        {
            try
            {
                _db.ConversationTurns.Add(turn);
                await _db.SaveChangesAsync(ct);
                return ToResponse(turn);
            }
            catch (DbUpdateException) when (attemptNumber < maxAttempts)
            {
                _db.Entry(turn).State = EntityState.Detached;
                await Task.Delay(Random.Shared.Next(15, 60) * attemptNumber, ct);
                turn.TRN_NumeroTurno = (await _db.ConversationTurns
                    .Where(t => t.SEG_IdTenant == tenantId && t.ATT_IdAttempt == attemptId)
                    .MaxAsync(t => (int?)t.TRN_NumeroTurno, ct) ?? 0) + 1;
            }
        }
    }

    public async Task<ConversationTurnResponse?> GetByIdAsync(Guid tenantId, Guid turnId, Guid participantId, bool canReadAll, CancellationToken ct)
    {
        var turn = await _db.ConversationTurns.AsNoTracking().FirstOrDefaultAsync(t => t.SEG_IdTenant == tenantId && t.TRN_IdTurn == turnId, ct);
        if (turn is null) return null;
        if (!canReadAll && !await OwnsAttemptAsync(tenantId, turn.ATT_IdAttempt, participantId, ct)) return null;
        return ToResponse(turn);
    }

    public async Task<ConversationTurnListResponse> ListByAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, int page, int pageSize, CancellationToken ct)
    {
        if (!await _db.Attempts.AsNoTracking().AnyAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct)) throw new ConversationTurnAttemptNotFoundException();
        if (!canReadAll && !await OwnsAttemptAsync(tenantId, attemptId, participantId, ct)) throw new ConversationTurnAttemptNotFoundException();
        var query = _db.ConversationTurns.AsNoTracking().Where(t => t.SEG_IdTenant == tenantId && t.ATT_IdAttempt == attemptId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.TRN_NumeroTurno).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new ConversationTurnListResponse { Items = items.Select(ToResponse).ToList(), Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public async Task<ConversationTurnResponse> FinalizeAsync(Guid tenantId, Guid turnId, string etag, string user, CancellationToken ct)
    {
        var turn = await _db.ConversationTurns.FirstOrDefaultAsync(t => t.SEG_IdTenant == tenantId && t.TRN_IdTurn == turnId, ct) ?? throw new ConversationTurnNotFoundException();
        EnsureEtag(turn.RowVersion, etag);
        if (turn.TRN_Estatus == TurnEstatus.Excluded) throw new TranscriptNotEditableException();
        turn.TRN_Estatus = TurnEstatus.Final;
        Touch(turn, user);
        await SaveAsync(ct);
        return ToResponse(turn);
    }

    public async Task<ConversationTurnResponse> CorrectAsync(Guid tenantId, Guid turnId, string etag, string user, CorrectConversationTurnRequest request, CancellationToken ct)
    {
        var turn = await _db.ConversationTurns.FirstOrDefaultAsync(t => t.SEG_IdTenant == tenantId && t.TRN_IdTurn == turnId, ct) ?? throw new ConversationTurnNotFoundException();
        EnsureEtag(turn.RowVersion, etag);
        if (turn.TRN_Estatus == TurnEstatus.Excluded) throw new TranscriptNotEditableException();
        if (!turn.TRN_TextoFueEditado) turn.TRN_TextoOriginal = turn.TRN_Texto;
        turn.TRN_Texto = request.CorrectedText!.Trim();
        turn.TRN_TextoFueEditado = true;
        turn.TRN_EditadoPor = user;
        turn.TRN_FechaEdicion = DateTimeOffset.UtcNow;
        turn.TRN_Estatus = TurnEstatus.Corrected;
        Touch(turn, user);
        await SaveAsync(ct);
        return ToResponse(turn);
    }

    public async Task<ConversationTurnResponse> ExcludeAsync(Guid tenantId, Guid turnId, string etag, string user, CancellationToken ct)
    {
        var turn = await _db.ConversationTurns.FirstOrDefaultAsync(t => t.SEG_IdTenant == tenantId && t.TRN_IdTurn == turnId, ct) ?? throw new ConversationTurnNotFoundException();
        EnsureEtag(turn.RowVersion, etag);
        turn.TRN_Estatus = TurnEstatus.Excluded;
        Touch(turn, user);
        await SaveAsync(ct);
        return ToResponse(turn);
    }

    private async Task<bool> OwnsAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, CancellationToken ct) =>
        await _db.Attempts.AsNoTracking().AnyAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId && a.USR_IdParticipant == participantId, ct);

    private async Task SaveAsync(CancellationToken ct) { try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConversationTurnConcurrencyException(); } }
    private static void EnsureEtag(byte[] current, string? expected) { if (string.IsNullOrWhiteSpace(expected)) throw new ConversationTurnPreconditionException("ETAG_REQUIRED"); if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal)) throw new ConversationTurnPreconditionException("ETAG_MISMATCH"); }
    private static void Touch(LAB_ConversationTurn t, string user) { t.FechaActualizacion = DateTimeOffset.UtcNow; t.ActualizadoPor = user; }

    private static ConversationTurnResponse ToResponse(LAB_ConversationTurn t) => new()
    {
        IdTurn = t.TRN_IdTurn,
        IdAttempt = t.ATT_IdAttempt,
        IdVersion = t.LAB_IdVersion,
        NumeroTurno = t.TRN_NumeroTurno,
        SpeakerType = t.TRN_SpeakerType,
        ActorId = t.ACT_IdActor,
        Texto = t.TRN_Texto,
        TipoEntrada = t.TRN_TipoEntrada,
        FechaInicio = t.TRN_FechaInicio,
        FechaFin = t.TRN_FechaFin,
        DuracionMs = t.TRN_DuracionMs,
        ConfianzaTranscripcion = t.TRN_ConfianzaTranscripcion,
        FueInterrumpido = t.TRN_FueInterrumpido,
        InterrumpioTurnoAnterior = t.TRN_InterrumpioTurnoAnterior,
        EsRespuesta = t.TRN_EsRespuesta,
        IdTurnoRespondido = t.TRN_IdTurnoRespondido,
        IdExpectedMoment = t.TRN_IdExpectedMoment,
        TextoFueEditado = t.TRN_TextoFueEditado,
        TextoOriginal = t.TRN_TextoOriginal,
        EditadoPor = t.TRN_EditadoPor,
        FechaEdicion = t.TRN_FechaEdicion,
        Estatus = t.TRN_Estatus,
        FechaCreacion = t.FechaCreacion,
        CreadoPor = t.CreadoPor,
        FechaActualizacion = t.FechaActualizacion,
        ActualizadoPor = t.ActualizadoPor,
        RowVersion = Convert.ToBase64String(t.RowVersion),
    };
}
