using HumanOS.SimulationLabs.Api.Features.UserActions.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.UserActions.Services;

public sealed class UserActionService : IUserActionService
{
    private readonly SimulationLabsDbContext _db;
    public UserActionService(SimulationLabsDbContext db) => _db = db;

    public async Task<UserActionResponse> CreateAsync(Guid tenantId, Guid attemptId, string user, CreateUserActionRequest request, CancellationToken ct)
    {
        var attempt = await _db.Attempts.AsNoTracking().FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct) ?? throw new UserActionAttemptNotFoundException();
        if (attempt.ATT_Estatus != AttemptEstatus.InProgress) throw new UserActionAttemptNotEditableException();

        if (request.ExpectedMomentId is { } momentId)
        {
            var momentExists = await _db.ExpectedMoments.AnyAsync(m => m.MOM_IdExpectedMoment == momentId && m.LAB_IdVersion == attempt.LAB_IdVersion, ct);
            if (!momentExists) throw new ActionExpectedMomentMismatchException();
        }

        var sequence = (await _db.UserActions.Where(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId).MaxAsync(a => (int?)a.ACT_NumeroSecuencia, ct) ?? 0) + 1;
        var now = DateTimeOffset.UtcNow;
        var action = new LAB_UserAction
        {
            ACT_IdUserAction = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = attempt.LAB_IdVersion,
            ATT_IdAttempt = attemptId,
            ACT_NumeroSecuencia = sequence,
            ACT_Codigo = request.Codigo!.Trim().ToUpperInvariant(),
            ACT_Nombre = request.Nombre!.Trim(),
            ACT_Tipo = request.Tipo!.Trim().ToUpperInvariant(),
            ACT_Pagina = Clean(request.Pagina),
            ACT_Componente = Clean(request.Componente),
            ACT_EntidadTipo = Clean(request.EntidadTipo),
            ACT_EntidadId = Clean(request.EntidadId),
            ACT_ValorAnterior = Clean(request.ValorAnterior),
            ACT_ValorNuevo = Clean(request.ValorNuevo),
            ACT_InputJson = Clean(request.InputJson),
            ACT_OutputJson = Clean(request.OutputJson),
            ACT_Resultado = request.Resultado!.Trim().ToUpperInvariant(),
            ACT_MensajeResultado = Clean(request.MensajeResultado),
            ACT_EraPermitida = request.EraPermitida ?? true,
            ACT_EraEsperada = request.EraEsperada ?? true,
            ACT_EsCritica = request.EsCritica ?? false,
            ACT_Severidad = request.Severidad!.Trim().ToUpperInvariant(),
            ACT_MotivoEvaluacion = Clean(request.MotivoEvaluacion),
            MOM_IdExpectedMoment = request.ExpectedMomentId,
            ACT_FechaInicio = request.FechaInicio ?? now,
            ACT_FechaFin = request.FechaFin,
            ACT_DuracionMs = request.DuracionMs,
            ACT_Origen = request.Origen!.Trim().ToUpperInvariant(),
            ACT_Estatus = UserActionEstatus.Started,
            FechaCreacion = now,
            CreadoPor = user,
        };
        _db.UserActions.Add(action);
        await _db.SaveChangesAsync(ct);
        return ToResponse(action);
    }

    public async Task<UserActionResponse?> GetByIdAsync(Guid tenantId, Guid userActionId, Guid participantId, bool canReadAll, CancellationToken ct)
    {
        var action = await _db.UserActions.AsNoTracking().FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ACT_IdUserAction == userActionId, ct);
        if (action is null) return null;
        if (!canReadAll && !await OwnsAttemptAsync(tenantId, action.ATT_IdAttempt, participantId, ct)) return null;
        return ToResponse(action);
    }

    public async Task<UserActionListResponse> ListByAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, string? tipo, string? resultado, string? severidad, bool? critica, Guid? expectedMomentId, int page, int pageSize, CancellationToken ct)
    {
        if (!await _db.Attempts.AsNoTracking().AnyAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct)) throw new UserActionAttemptNotFoundException();
        if (!canReadAll && !await OwnsAttemptAsync(tenantId, attemptId, participantId, ct)) throw new UserActionAttemptNotFoundException();
        var query = _db.UserActions.AsNoTracking().Where(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(a => a.ACT_Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(resultado)) query = query.Where(a => a.ACT_Resultado == resultado);
        if (!string.IsNullOrWhiteSpace(severidad)) query = query.Where(a => a.ACT_Severidad == severidad);
        if (critica.HasValue) query = query.Where(a => a.ACT_EsCritica == critica.Value);
        if (expectedMomentId.HasValue) query = query.Where(a => a.MOM_IdExpectedMoment == expectedMomentId.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.ACT_NumeroSecuencia).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new UserActionListItemResponse { IdUserAction = a.ACT_IdUserAction, NumeroSecuencia = a.ACT_NumeroSecuencia, Codigo = a.ACT_Codigo, Nombre = a.ACT_Nombre, Tipo = a.ACT_Tipo, Resultado = a.ACT_Resultado, Severidad = a.ACT_Severidad, EsCritica = a.ACT_EsCritica, ExpectedMomentId = a.MOM_IdExpectedMoment, Estatus = a.ACT_Estatus, RowVersion = Convert.ToBase64String(a.RowVersion) })
            .ToListAsync(ct);
        return new UserActionListResponse { Items = items, Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public Task<UserActionResponse> FinalizeAsync(Guid tenantId, Guid userActionId, string etag, string user, CancellationToken ct) =>
        SetStatusAsync(tenantId, userActionId, etag, user, UserActionEstatus.Final, [UserActionEstatus.Started], ct);

    public Task<UserActionResponse> ReverseAsync(Guid tenantId, Guid userActionId, string etag, string user, CancellationToken ct) =>
        SetStatusAsync(tenantId, userActionId, etag, user, UserActionEstatus.Reversed, [UserActionEstatus.Started, UserActionEstatus.Final], ct);

    public Task<UserActionResponse> ExcludeAsync(Guid tenantId, Guid userActionId, string etag, string user, CancellationToken ct) =>
        SetStatusAsync(tenantId, userActionId, etag, user, UserActionEstatus.Excluded, [UserActionEstatus.Started, UserActionEstatus.Final, UserActionEstatus.Reversed], ct);

    private async Task<UserActionResponse> SetStatusAsync(Guid tenantId, Guid userActionId, string etag, string user, string targetStatus, string[] allowedFrom, CancellationToken ct)
    {
        var action = await _db.UserActions.FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ACT_IdUserAction == userActionId, ct) ?? throw new UserActionNotFoundException();
        EnsureEtag(action.RowVersion, etag);
        if (!allowedFrom.Contains(action.ACT_Estatus)) throw new UserActionNotEditableException();
        action.ACT_Estatus = targetStatus;
        Touch(action, user);
        await SaveAsync(ct);
        return ToResponse(action);
    }

    private async Task<bool> OwnsAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, CancellationToken ct) =>
        await _db.Attempts.AsNoTracking().AnyAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId && a.USR_IdParticipant == participantId, ct);

    private async Task SaveAsync(CancellationToken ct) { try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new UserActionConcurrencyException(); } }
    private static void EnsureEtag(byte[] current, string? expected) { if (string.IsNullOrWhiteSpace(expected)) throw new UserActionPreconditionException("ETAG_REQUIRED"); if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal)) throw new UserActionPreconditionException("ETAG_MISMATCH"); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Touch(LAB_UserAction a, string user) { a.FechaActualizacion = DateTimeOffset.UtcNow; a.ActualizadoPor = user; }

    private static UserActionResponse ToResponse(LAB_UserAction a) => new()
    {
        IdUserAction = a.ACT_IdUserAction,
        IdVersion = a.LAB_IdVersion,
        IdAttempt = a.ATT_IdAttempt,
        NumeroSecuencia = a.ACT_NumeroSecuencia,
        Codigo = a.ACT_Codigo,
        Nombre = a.ACT_Nombre,
        Tipo = a.ACT_Tipo,
        Pagina = a.ACT_Pagina,
        Componente = a.ACT_Componente,
        EntidadTipo = a.ACT_EntidadTipo,
        EntidadId = a.ACT_EntidadId,
        ValorAnterior = a.ACT_ValorAnterior,
        ValorNuevo = a.ACT_ValorNuevo,
        InputJson = a.ACT_InputJson,
        OutputJson = a.ACT_OutputJson,
        Resultado = a.ACT_Resultado,
        MensajeResultado = a.ACT_MensajeResultado,
        EraPermitida = a.ACT_EraPermitida,
        EraEsperada = a.ACT_EraEsperada,
        EsCritica = a.ACT_EsCritica,
        Severidad = a.ACT_Severidad,
        MotivoEvaluacion = a.ACT_MotivoEvaluacion,
        ExpectedMomentId = a.MOM_IdExpectedMoment,
        FechaInicio = a.ACT_FechaInicio,
        FechaFin = a.ACT_FechaFin,
        DuracionMs = a.ACT_DuracionMs,
        Origen = a.ACT_Origen,
        Estatus = a.ACT_Estatus,
        FechaCreacion = a.FechaCreacion,
        CreadoPor = a.CreadoPor,
        FechaActualizacion = a.FechaActualizacion,
        ActualizadoPor = a.ActualizadoPor,
        RowVersion = Convert.ToBase64String(a.RowVersion),
    };
}
