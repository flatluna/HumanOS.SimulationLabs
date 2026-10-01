using HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Services;

public sealed class AttemptService : IAttemptService
{
    private readonly SimulationLabsDbContext _db;
    public AttemptService(SimulationLabsDbContext db) => _db = db;

    public async Task<AttemptResponse> StartAsync(Guid tenantId, Guid scenarioId, Guid participantId, string user, StartAttemptRequest request, CancellationToken ct)
    {
        var scenario = await _db.Scenarios.AsNoTracking().FirstOrDefaultAsync(s => s.SCN_IdScenario == scenarioId, ct) ?? throw new AttemptScenarioNotFoundException();
        if (scenario.SEG_IdTenant != tenantId)
        {
            var isGlobal = await _db.Labs.AsNoTracking()
                .Join(_db.LabVersions.AsNoTracking(), l => l.LAB_IdLab, v => v.LAB_IdLab, (l, v) => new { l.LAB_EsGlobal, v.LAB_IdVersion })
                .AnyAsync(x => x.LAB_IdVersion == scenario.LAB_IdVersion && x.LAB_EsGlobal, ct);
            if (!isGlobal) throw new AttemptScenarioNotFoundException();
        }
        if (!string.Equals(scenario.SCN_Estatus, ScenarioEstatus.Published, StringComparison.OrdinalIgnoreCase)) throw new ScenarioNotPublishedException();
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.LAB_IdVersion == scenario.LAB_IdVersion, ct);
        var now = DateTime.UtcNow;
        if (scenario.SCN_VigenciaDesde is { } desde && now < desde) throw new ScenarioNotPublishedException();
        if (scenario.SCN_VigenciaHasta is { } hasta && now > hasta) throw new ScenarioNotPublishedException();

        var previousCount = await _db.Attempts.CountAsync(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenarioId && a.USR_IdParticipant == participantId, ct);
        if (previousCount > 0 && !scenario.SCN_PermiteReintento) throw new AttemptLimitReachedException();
        if (scenario.SCN_MaximoIntentos is { } max && previousCount >= max) throw new AttemptLimitReachedException();

        var modalidad = request.Modalidad!.Trim().ToUpperInvariant();

        // Retries the count+insert on a unique-index collision (SqlException 2601/2627) — two
        // near-simultaneous Attempt_Start calls (e.g. a stale double-invoke, two browser tabs)
        // can both read the same previousCount and race to insert the same NumeroIntento. The
        // Idempotency-Key header (see AttemptStartFunction) avoids this for a single call site,
        // but this loop makes the endpoint itself safe regardless of caller behavior.
        for (var attemptNumber = previousCount + 1; ; attemptNumber++)
        {
            var attempt = new LAB_Attempt
            {
                ATT_IdAttempt = Guid.NewGuid(),
                SEG_IdTenant = tenantId,
                LAB_IdVersion = scenario.LAB_IdVersion,
                SCN_IdScenario = scenarioId,
                USR_IdParticipant = participantId,
                ATT_NumeroIntento = attemptNumber,
                ATT_Estatus = AttemptEstatus.InProgress,
                ATT_FechaInicio = DateTimeOffset.UtcNow,
                ATT_Modalidad = modalidad,
                ATT_HashConfiguracion = version?.LAB_HashConfiguracion,
                ATT_Idioma = request.Idioma!.Trim(),
                ATT_UsaVoz = modalidad is AttemptModalidad.Voice or AttemptModalidad.Hybrid,
                ATT_UsaEscritorio = modalidad is AttemptModalidad.Desktop or AttemptModalidad.Hybrid,
                ATT_UsaArtefactos = modalidad is AttemptModalidad.Desktop or AttemptModalidad.Hybrid,
                FechaCreacion = DateTimeOffset.UtcNow,
                CreadoPor = user,
            };
            _db.Attempts.Add(attempt);
            try
            {
                await _db.SaveChangesAsync(ct);
                return ToResponse(attempt);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex) && attemptNumber < previousCount + 10)
            {
                _db.Entry(attempt).State = EntityState.Detached;
            }
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number is 2601 or 2627;

    public async Task<AttemptResponse?> GetByIdAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, CancellationToken ct)
    {
        var attempt = await _db.Attempts.AsNoTracking().FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct);
        if (attempt is null) return null;
        if (!canReadAll && attempt.USR_IdParticipant != participantId) return null;
        return ToResponse(attempt);
    }

    public async Task<AttemptListResponse> ListMineAsync(Guid tenantId, Guid participantId, Guid? scenarioId, string? status, string? result, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Attempts.AsNoTracking().Where(a => a.SEG_IdTenant == tenantId && a.USR_IdParticipant == participantId);
        if (scenarioId.HasValue) query = query.Where(a => a.SCN_IdScenario == scenarioId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.ATT_Estatus == status);
        if (!string.IsNullOrWhiteSpace(result)) query = query.Where(a => a.ATT_Resultado == result);
        var total = await query.CountAsync(ct);
        var joined =
            from a in query
            join s in _db.Scenarios.AsNoTracking() on a.SCN_IdScenario equals s.SCN_IdScenario
            join v in _db.LabVersions.AsNoTracking() on s.LAB_IdVersion equals v.LAB_IdVersion
            join l in _db.Labs.AsNoTracking() on v.LAB_IdLab equals l.LAB_IdLab
            orderby a.FechaCreacion descending
            select new AttemptListItemResponse
            {
                IdAttempt = a.ATT_IdAttempt,
                IdScenario = a.SCN_IdScenario,
                NumeroIntento = a.ATT_NumeroIntento,
                Estatus = a.ATT_Estatus,
                Resultado = a.ATT_Resultado,
                FechaInicio = a.ATT_FechaInicio,
                FechaFin = a.ATT_FechaFin,
                RowVersion = Convert.ToBase64String(a.RowVersion),
                IdLab = l.LAB_IdLab,
                LabNombre = l.LAB_Nombre,
                ScenarioNombre = s.SCN_Nombre,
                ScoreFinal = a.ATT_ScoreFinal,
            };
        var items = await joined.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new AttemptListResponse { Items = items, Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public Task<AttemptResponse> PauseAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct) =>
        TransitionAsync(tenantId, attemptId, participantId, etag, user, [AttemptEstatus.InProgress], AttemptEstatus.Paused, false, ct);

    public Task<AttemptResponse> ResumeAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct) =>
        TransitionAsync(tenantId, attemptId, participantId, etag, user, [AttemptEstatus.Paused], AttemptEstatus.InProgress, false, ct);

    public Task<AttemptResponse> CompleteAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct) =>
        TransitionAsync(tenantId, attemptId, participantId, etag, user, [AttemptEstatus.InProgress, AttemptEstatus.Paused], AttemptEstatus.Completed, true, ct);

    public Task<AttemptResponse> AbandonAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct) =>
        TransitionAsync(tenantId, attemptId, participantId, etag, user, [AttemptEstatus.InProgress, AttemptEstatus.Paused], AttemptEstatus.Abandoned, true, ct);

    public Task<AttemptResponse> CancelAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct) =>
        TransitionAsync(tenantId, attemptId, participantId, etag, user, [AttemptEstatus.InProgress, AttemptEstatus.Paused], AttemptEstatus.Cancelled, true, ct);

    private async Task<AttemptResponse> TransitionAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, string[] allowedFrom, string toStatus, bool isFinal, CancellationToken ct)
    {
        var attempt = await _db.Attempts.FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct) ?? throw new AttemptNotFoundException();
        if (attempt.USR_IdParticipant != participantId) throw new AttemptNotFoundException();
        EnsureEtag(attempt.RowVersion, etag);
        if (!allowedFrom.Contains(attempt.ATT_Estatus)) throw new InvalidAttemptTransitionException();
        attempt.ATT_Estatus = toStatus;
        if (isFinal)
        {
            attempt.ATT_FechaFin = DateTimeOffset.UtcNow;
            if (attempt.ATT_FechaInicio.HasValue) attempt.ATT_DuracionSegundos = (int)(attempt.ATT_FechaFin.Value - attempt.ATT_FechaInicio.Value).TotalSeconds;
        }
        attempt.FechaActualizacion = DateTimeOffset.UtcNow;
        attempt.ActualizadoPor = user;
        try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new AttemptConcurrencyException(); }
        return ToResponse(attempt);
    }

    private static void EnsureEtag(byte[] current, string? expected) { if (string.IsNullOrWhiteSpace(expected)) throw new AttemptPreconditionException("ETAG_REQUIRED"); if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal)) throw new AttemptPreconditionException("ETAG_MISMATCH"); }

    public async Task DeleteAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canManageAll, CancellationToken ct)
    {
        var attempt = await _db.Attempts.FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct) ?? throw new AttemptNotFoundException();
        if (!canManageAll && attempt.USR_IdParticipant != participantId) throw new AttemptNotFoundException();

        // All related rows share ATT_IdAttempt; delete them plus the attempt in a single transaction so a failure leaves nothing partially removed.
        // SqlServerRetryingExecutionStrategy forbids a plain BeginTransactionAsync — retries must wrap the whole transaction via CreateExecutionStrategy.
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            await _db.ConversationTurns.Where(t => t.SEG_IdTenant == tenantId && t.ATT_IdAttempt == attemptId).ExecuteDeleteAsync(ct);
            await _db.UserActions.Where(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId).ExecuteDeleteAsync(ct);
            await _db.ArtifactSubmissions.Where(s => s.SEG_IdTenant == tenantId && s.ATT_IdAttempt == attemptId).ExecuteDeleteAsync(ct);
            await _db.AttemptEvaluations.Where(e => e.SEG_IdTenant == tenantId && e.ATT_IdAttempt == attemptId).ExecuteDeleteAsync(ct);
            await _db.Attempts.Where(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId).ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }

    private static AttemptResponse ToResponse(LAB_Attempt a) => new()
    {
        IdAttempt = a.ATT_IdAttempt,
        IdVersion = a.LAB_IdVersion,
        IdScenario = a.SCN_IdScenario,
        IdParticipant = a.USR_IdParticipant,
        NumeroIntento = a.ATT_NumeroIntento,
        Estatus = a.ATT_Estatus,
        FechaInicio = a.ATT_FechaInicio,
        FechaFin = a.ATT_FechaFin,
        DuracionSegundos = a.ATT_DuracionSegundos,
        Modalidad = a.ATT_Modalidad,
        SeedEjecucion = a.ATT_SeedEjecucion,
        HashConfiguracion = a.ATT_HashConfiguracion,
        ScoreFinal = a.ATT_ScoreFinal,
        Resultado = a.ATT_Resultado,
        Idioma = a.ATT_Idioma,
        UsaVoz = a.ATT_UsaVoz,
        UsaEscritorio = a.ATT_UsaEscritorio,
        UsaArtefactos = a.ATT_UsaArtefactos,
        ErrorCodigo = a.ATT_ErrorCodigo,
        ErrorDescripcion = a.ATT_ErrorDescripcion,
        FechaCreacion = a.FechaCreacion,
        CreadoPor = a.CreadoPor,
        FechaActualizacion = a.FechaActualizacion,
        ActualizadoPor = a.ActualizadoPor,
        RowVersion = Convert.ToBase64String(a.RowVersion),
    };
}
