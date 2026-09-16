using HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Services;

public sealed class ExpectedMomentService : IExpectedMomentService
{
    private readonly SimulationLabsDbContext _db;

    public ExpectedMomentService(SimulationLabsDbContext db) => _db = db;

    public async Task<ExpectedMomentResponse> CreateAsync(Guid tenantId, Guid stageId, string user, CreateExpectedMomentRequest request, CancellationToken ct)
    {
        var stage = await _db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId, ct)
            ?? throw new ExpectedMomentStageNotFoundException();

        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == stage.LAB_IdVersion, ct)
            ?? throw new ExpectedMomentVersionNotFoundException();
        EnsureDraft(version.LAB_Estatus);

        if (request.ObjectiveId.HasValue)
        {
            await EnsureObjectiveInStage(tenantId, stage.LAB_IdVersion, stageId, request.ObjectiveId.Value, ct);
        }

        var code = request.Codigo!.Trim().ToUpperInvariant();
        if (await _db.ExpectedMoments.AnyAsync(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == stage.LAB_IdVersion && m.MOM_Codigo == code, ct))
        {
            throw new ExpectedMomentCodeDuplicateException();
        }

        var order = (await _db.ExpectedMoments
            .Where(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == stage.LAB_IdVersion && m.STG_IdStage == stageId)
            .MaxAsync(m => (int?)m.MOM_OrdenSugerido, ct) ?? 0) + 1;

        var moment = new LAB_ExpectedMoment
        {
            MOM_IdExpectedMoment = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = stage.LAB_IdVersion,
            STG_IdStage = stageId,
            OBJ_IdObjective = request.ObjectiveId,
            MOM_Codigo = code,
            MOM_Nombre = request.Nombre!.Trim(),
            MOM_Tipo = request.Tipo!.Trim().ToUpperInvariant(),
            MOM_Trigger = request.Trigger!.Trim(),
            MOM_IntencionEsperada = request.IntencionEsperada!.Trim(),
            MOM_RespuestaEjemplar = request.RespuestaEjemplar?.Trim(),
            MOM_InformacionDescubrible = request.InformacionDescubrible?.Trim(),
            MOM_ErrorFrecuente = request.ErrorFrecuente?.Trim(),
            MOM_Recomendacion = request.Recomendacion?.Trim(),
            MOM_EsCritico = request.EsCritico!.Value,
            MOM_OrdenSugerido = order,
            MOM_PermiteOrdenFlexible = request.PermiteOrdenFlexible!.Value,
            MOM_RequiereRespuesta = request.RequiereRespuesta!.Value,
            MOM_Estatus = MomentEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = user
        };

        _db.ExpectedMoments.Add(moment);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UQ_LAB_ExpectedMoment_SEG_IdTenant_LAB_IdVersion_MOM_Codigo"))
        {
            throw new ExpectedMomentCodeDuplicateException();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UQ_LAB_ExpectedMoment_Tenant_Version_Stage_Orden"))
        {
            throw new ExpectedMomentOrderDuplicateException();
        }

        return ToResponse(moment);
    }

    public async Task<ExpectedMomentResponse?> GetByIdAsync(Guid tenantId, Guid momentId, CancellationToken ct) =>
        (await _db.ExpectedMoments.AsNoTracking().FirstOrDefaultAsync(m => m.SEG_IdTenant == tenantId && m.MOM_IdExpectedMoment == momentId, ct)) is { } moment
            ? ToResponse(moment)
            : null;

    public async Task<ExpectedMomentListResponse> ListByVersionAsync(Guid tenantId, Guid versionId, Guid? stageId, Guid? objectiveId, string? tipo, bool? critico, bool? requiereRespuesta, string? estatus, int page, int pageSize, CancellationToken ct)
    {
        if (!await _db.LabVersions.AsNoTracking().AnyAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct))
        {
            throw new ExpectedMomentVersionNotFoundException();
        }

        var query = _db.ExpectedMoments.AsNoTracking().Where(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == versionId);
        if (stageId.HasValue) query = query.Where(m => m.STG_IdStage == stageId.Value);
        if (objectiveId.HasValue) query = query.Where(m => m.OBJ_IdObjective == objectiveId.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(m => m.MOM_Tipo == tipo);
        if (critico.HasValue) query = query.Where(m => m.MOM_EsCritico == critico.Value);
        if (requiereRespuesta.HasValue) query = query.Where(m => m.MOM_RequiereRespuesta == requiereRespuesta.Value);
        if (!string.IsNullOrWhiteSpace(estatus)) query = query.Where(m => m.MOM_Estatus == estatus);

        var total = await query.CountAsync(ct);
        query = query.OrderBy(m => m.STG_IdStage).ThenBy(m => m.MOM_OrdenSugerido);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(ToListItem).ToListAsync(ct);

        return new ExpectedMomentListResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<ExpectedMomentListResponse> ListByStageAsync(Guid tenantId, Guid stageId, Guid? objectiveId, string? tipo, bool? critico, string? estatus, int page, int pageSize, CancellationToken ct)
    {
        if (!await _db.Stages.AsNoTracking().AnyAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId, ct))
        {
            throw new ExpectedMomentStageNotFoundException();
        }

        var query = _db.ExpectedMoments.AsNoTracking().Where(m => m.SEG_IdTenant == tenantId && m.STG_IdStage == stageId);
        if (objectiveId.HasValue) query = query.Where(m => m.OBJ_IdObjective == objectiveId.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(m => m.MOM_Tipo == tipo);
        if (critico.HasValue) query = query.Where(m => m.MOM_EsCritico == critico.Value);
        if (!string.IsNullOrWhiteSpace(estatus)) query = query.Where(m => m.MOM_Estatus == estatus);

        var total = await query.CountAsync(ct);
        query = query.OrderBy(m => m.MOM_OrdenSugerido);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(ToListItem).ToListAsync(ct);

        return new ExpectedMomentListResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<ExpectedMomentResponse> UpdateAsync(Guid tenantId, Guid momentId, string etag, string user, UpdateExpectedMomentRequest request, CancellationToken ct)
    {
        var moment = await _db.ExpectedMoments.Include(m => m.LabVersion).FirstOrDefaultAsync(m => m.SEG_IdTenant == tenantId && m.MOM_IdExpectedMoment == momentId, ct)
            ?? throw new ExpectedMomentNotFoundException();
        EnsureDraft(moment.LabVersion?.LAB_Estatus);
        EnsureEtag(moment.RowVersion, etag);

        if (request.StageId.HasValue && request.StageId.Value != moment.STG_IdStage)
        {
            var targetStage = await _db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == request.StageId.Value && s.LAB_IdVersion == moment.LAB_IdVersion, ct)
                ?? throw new ExpectedMomentStageNotFoundException();
            var newOrder = (await _db.ExpectedMoments
                .Where(m => m.SEG_IdTenant == tenantId && m.STG_IdStage == targetStage.STG_IdStage)
                .MaxAsync(m => (int?)m.MOM_OrdenSugerido, ct) ?? 0) + 1;
            moment.STG_IdStage = targetStage.STG_IdStage;
            moment.MOM_OrdenSugerido = newOrder;
            moment.OBJ_IdObjective = null;
        }

        if (request.ObjectiveId.HasValue)
        {
            await EnsureObjectiveInStage(tenantId, moment.LAB_IdVersion, moment.STG_IdStage, request.ObjectiveId.Value, ct);
            moment.OBJ_IdObjective = request.ObjectiveId;
        }

        if (request.Nombre is not null) moment.MOM_Nombre = request.Nombre.Trim();
        if (request.Tipo is not null) moment.MOM_Tipo = request.Tipo.Trim().ToUpperInvariant();
        if (request.Trigger is not null) moment.MOM_Trigger = request.Trigger.Trim();
        if (request.IntencionEsperada is not null) moment.MOM_IntencionEsperada = request.IntencionEsperada.Trim();
        if (request.RespuestaEjemplar is not null) moment.MOM_RespuestaEjemplar = request.RespuestaEjemplar.Trim();
        if (request.InformacionDescubrible is not null) moment.MOM_InformacionDescubrible = request.InformacionDescubrible.Trim();
        if (request.ErrorFrecuente is not null) moment.MOM_ErrorFrecuente = request.ErrorFrecuente.Trim();
        if (request.Recomendacion is not null) moment.MOM_Recomendacion = request.Recomendacion.Trim();
        if (request.EsCritico.HasValue) moment.MOM_EsCritico = request.EsCritico.Value;
        if (request.PermiteOrdenFlexible.HasValue) moment.MOM_PermiteOrdenFlexible = request.PermiteOrdenFlexible.Value;
        if (request.RequiereRespuesta.HasValue) moment.MOM_RequiereRespuesta = request.RequiereRespuesta.Value;

        Touch(moment, user);
        await SaveAsync(ct);
        return ToResponse(moment);
    }

    public async Task<ReorderExpectedMomentsResponse> ReorderAsync(Guid tenantId, Guid stageId, string user, ReorderExpectedMomentsRequest request, CancellationToken ct)
    {
        var stage = await _db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId, ct)
            ?? throw new ExpectedMomentStageNotFoundException();

        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == stage.LAB_IdVersion, ct)
            ?? throw new ExpectedMomentVersionNotFoundException();
        EnsureDraft(version.LAB_Estatus);

        var input = request.Moments!;
        if (input.Select(x => x.IdExpectedMoment).Distinct().Count() != input.Count ||
            input.Select(x => x.Orden).Distinct().Count() != input.Count ||
            input.Any(x => x.Orden <= 0))
        {
            throw new ExpectedMomentOrderDuplicateException();
        }

        var ids = input.Select(x => x.IdExpectedMoment).ToArray();
        var moments = await _db.ExpectedMoments
            .Where(m => m.SEG_IdTenant == tenantId && m.STG_IdStage == stageId && ids.Contains(m.MOM_IdExpectedMoment))
            .ToListAsync(ct);

        if (moments.Count != input.Count)
        {
            throw new ExpectedMomentNotFoundException();
        }

        foreach (var item in input)
        {
            EnsureEtag(moments.Single(m => m.MOM_IdExpectedMoment == item.IdExpectedMoment).RowVersion, item.RowVersion);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var offset = Math.Max(moments.Max(m => m.MOM_OrdenSugerido), input.Max(x => x.Orden)) + moments.Count + 1;
            foreach (var moment in moments)
            {
                moment.MOM_OrdenSugerido += offset;
                Touch(moment, user);
            }
            await _db.SaveChangesAsync(ct);

            foreach (var item in input)
            {
                moments.Single(m => m.MOM_IdExpectedMoment == item.IdExpectedMoment).MOM_OrdenSugerido = item.Orden;
            }
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new ExpectedMomentConcurrencyException();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UQ_LAB_ExpectedMoment_Tenant_Version_Stage_Orden"))
        {
            await tx.RollbackAsync(ct);
            throw new ExpectedMomentOrderDuplicateException();
        }

        return new ReorderExpectedMomentsResponse
        {
            Items = moments.OrderBy(m => m.MOM_OrdenSugerido).Select(m => new ReorderExpectedMomentResponseItem
            {
                IdExpectedMoment = m.MOM_IdExpectedMoment,
                Orden = m.MOM_OrdenSugerido,
                RowVersion = Convert.ToBase64String(m.RowVersion)
            }).ToList()
        };
    }

    public async Task<ExpectedMomentResponse> SetStatusAsync(Guid tenantId, Guid momentId, string etag, string user, string targetStatus, CancellationToken ct)
    {
        var moment = await _db.ExpectedMoments.Include(m => m.LabVersion).FirstOrDefaultAsync(m => m.SEG_IdTenant == tenantId && m.MOM_IdExpectedMoment == momentId, ct)
            ?? throw new ExpectedMomentNotFoundException();
        EnsureDraft(moment.LabVersion?.LAB_Estatus);
        EnsureEtag(moment.RowVersion, etag);

        if (moment.MOM_Estatus != targetStatus &&
            !((moment.MOM_Estatus == MomentEstatus.Draft && (targetStatus == MomentEstatus.Active || targetStatus == MomentEstatus.Inactive)) ||
              (moment.MOM_Estatus == MomentEstatus.Active && targetStatus == MomentEstatus.Inactive) ||
              (moment.MOM_Estatus == MomentEstatus.Inactive && targetStatus == MomentEstatus.Active)))
        {
            throw new ExpectedMomentPreconditionException("INVALID_STATUS_TRANSITION");
        }

        if (moment.MOM_Estatus != targetStatus)
        {
            moment.MOM_Estatus = targetStatus;
            Touch(moment, user);
            await SaveAsync(ct);
        }

        return ToResponse(moment);
    }

    private async Task EnsureObjectiveInStage(Guid tenantId, Guid versionId, Guid stageId, Guid objectiveId, CancellationToken ct)
    {
        var objective = await _db.Objectives.AsNoTracking().FirstOrDefaultAsync(o => o.SEG_IdTenant == tenantId && o.OBJ_IdObjective == objectiveId, ct)
            ?? throw new ExpectedMomentObjectiveNotFoundException();

        if (objective.LAB_IdVersion != versionId || objective.STG_IdStage != stageId)
        {
            throw new ExpectedMomentObjectiveStageMismatchException();
        }
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ExpectedMomentConcurrencyException();
        }
    }

    private static void EnsureDraft(string? status)
    {
        if (!string.Equals(status, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new ExpectedMomentVersionNotEditableException();
        }
    }

    private static void EnsureEtag(byte[] current, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            throw new ExpectedMomentPreconditionException("ETAG_REQUIRED");
        }

        if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal))
        {
            throw new ExpectedMomentPreconditionException("ETAG_MISMATCH");
        }
    }

    private static void Touch(LAB_ExpectedMoment m, string user)
    {
        m.FechaActualizacion = DateTimeOffset.UtcNow;
        m.ActualizadoPor = user;
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string indexName) =>
        ex.InnerException?.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase) == true;

    private static readonly System.Linq.Expressions.Expression<Func<LAB_ExpectedMoment, ExpectedMomentListItemResponse>> ToListItem = m => new ExpectedMomentListItemResponse
    {
        IdExpectedMoment = m.MOM_IdExpectedMoment,
        IdVersion = m.LAB_IdVersion,
        StageId = m.STG_IdStage,
        ObjectiveId = m.OBJ_IdObjective,
        Codigo = m.MOM_Codigo,
        Nombre = m.MOM_Nombre,
        Tipo = m.MOM_Tipo,
        Trigger = m.MOM_Trigger,
        IntencionEsperada = m.MOM_IntencionEsperada,
        RespuestaEjemplar = m.MOM_RespuestaEjemplar,
        ErrorFrecuente = m.MOM_ErrorFrecuente,
        Recomendacion = m.MOM_Recomendacion,
        EsCritico = m.MOM_EsCritico,
        RequiereRespuesta = m.MOM_RequiereRespuesta,
        OrdenSugerido = m.MOM_OrdenSugerido,
        Estatus = m.MOM_Estatus,
        RowVersion = Convert.ToBase64String(m.RowVersion)
    };

    private static ExpectedMomentResponse ToResponse(LAB_ExpectedMoment m) => new()
    {
        IdExpectedMoment = m.MOM_IdExpectedMoment,
        IdVersion = m.LAB_IdVersion,
        StageId = m.STG_IdStage,
        ObjectiveId = m.OBJ_IdObjective,
        Codigo = m.MOM_Codigo,
        Nombre = m.MOM_Nombre,
        Tipo = m.MOM_Tipo,
        Trigger = m.MOM_Trigger,
        IntencionEsperada = m.MOM_IntencionEsperada,
        RespuestaEjemplar = m.MOM_RespuestaEjemplar,
        InformacionDescubrible = m.MOM_InformacionDescubrible,
        ErrorFrecuente = m.MOM_ErrorFrecuente,
        Recomendacion = m.MOM_Recomendacion,
        EsCritico = m.MOM_EsCritico,
        OrdenSugerido = m.MOM_OrdenSugerido,
        PermiteOrdenFlexible = m.MOM_PermiteOrdenFlexible,
        RequiereRespuesta = m.MOM_RequiereRespuesta,
        Estatus = m.MOM_Estatus,
        FechaCreacion = m.FechaCreacion,
        CreadoPor = m.CreadoPor,
        FechaActualizacion = m.FechaActualizacion,
        ActualizadoPor = m.ActualizadoPor,
        RowVersion = Convert.ToBase64String(m.RowVersion)
    };
}
