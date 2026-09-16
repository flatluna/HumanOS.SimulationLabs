using HumanOS.SimulationLabs.Api.Features.Objectives.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Objectives.Services;

public sealed class ObjectiveService : IObjectiveService
{
    private readonly SimulationLabsDbContext _db;

    public ObjectiveService(SimulationLabsDbContext db) => _db = db;

    public async Task<ObjectiveResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateObjectiveRequest request, CancellationToken ct)
    {
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct)
            ?? throw new ObjectiveVersionNotFoundException();
        EnsureDraft(version.LAB_Estatus);

        if (request.StageId.HasValue)
        {
            var stageExists = await _db.Stages.AsNoTracking().AnyAsync(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == versionId && s.STG_IdStage == request.StageId.Value, ct);
            if (!stageExists)
            {
                throw new ObjectiveStageNotFoundException();
            }
        }

        var code = request.Codigo!.Trim().ToUpperInvariant();
        if (await _db.Objectives.AnyAsync(o => o.SEG_IdTenant == tenantId && o.LAB_IdVersion == versionId && o.OBJ_Codigo == code, ct))
        {
            throw new ObjectiveCodeDuplicateException();
        }

        int order;
        if (request.StageId.HasValue)
        {
            order = (await _db.Objectives
                .Where(o => o.SEG_IdTenant == tenantId && o.LAB_IdVersion == versionId && o.STG_IdStage == request.StageId.Value)
                .MaxAsync(o => (int?)o.OBJ_Orden, ct) ?? 0) + 1;
        }
        else
        {
            order = (await _db.Objectives
                .Where(o => o.SEG_IdTenant == tenantId && o.LAB_IdVersion == versionId && o.STG_IdStage == null)
                .MaxAsync(o => (int?)o.OBJ_Orden, ct) ?? 0) + 1;
        }

        var objective = new LAB_Objective
        {
            OBJ_IdObjective = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = versionId,
            STG_IdStage = request.StageId,
            OBJ_Codigo = code,
            OBJ_Descripcion = request.Descripcion!.Trim(),
            OBJ_TipoEvidencia = request.TipoEvidencia!.Trim().ToUpperInvariant(),
            OBJ_EsCritico = request.EsCritico!.Value,
            OBJ_Peso = request.Peso!.Value,
            OBJ_CondicionExito = request.CondicionExito!.Trim(),
            OBJ_Orden = order,
            OBJ_Estatus = ObjectiveEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = user
        };

        _db.Objectives.Add(objective);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUnique(ex))
        {
            throw new ObjectiveCodeDuplicateException();
        }

        return ToResponse(objective);
    }

    public async Task<ObjectiveResponse?> GetByIdAsync(Guid tenantId, Guid objectiveId, CancellationToken ct) =>
        (await _db.Objectives.AsNoTracking().FirstOrDefaultAsync(o => o.SEG_IdTenant == tenantId && o.OBJ_IdObjective == objectiveId, ct)) is { } obj
            ? ToResponse(obj)
            : null;

    public async Task<ObjectiveListResponse> ListByVersionAsync(Guid tenantId, Guid versionId, Guid? stageId, bool? onlyGeneral, string? status, string? evidenceType, bool? isCritical, int page, int pageSize, bool descending, CancellationToken ct)
    {
        if (!await _db.LabVersions.AsNoTracking().AnyAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct))
        {
            throw new ObjectiveVersionNotFoundException();
        }

        var query = _db.Objectives.AsNoTracking().Where(o => o.SEG_IdTenant == tenantId && o.LAB_IdVersion == versionId);
        if (stageId.HasValue)
        {
            query = query.Where(o => o.STG_IdStage == stageId.Value);
        }
        else if (onlyGeneral == true)
        {
            query = query.Where(o => o.STG_IdStage == null);
        }

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.OBJ_Estatus == status);
        if (!string.IsNullOrWhiteSpace(evidenceType)) query = query.Where(o => o.OBJ_TipoEvidencia == evidenceType);
        if (isCritical.HasValue) query = query.Where(o => o.OBJ_EsCritico == isCritical.Value);

        var total = await query.CountAsync(ct);
        query = descending
            ? query.OrderByDescending(o => o.STG_IdStage).ThenByDescending(o => o.OBJ_Orden)
            : query.OrderBy(o => o.STG_IdStage).ThenBy(o => o.OBJ_Orden);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new ObjectiveListItemResponse
            {
                IdObjective = o.OBJ_IdObjective,
                IdVersion = o.LAB_IdVersion,
                IdStage = o.STG_IdStage,
                Codigo = o.OBJ_Codigo,
                Descripcion = o.OBJ_Descripcion,
                TipoEvidencia = o.OBJ_TipoEvidencia,
                EsCritico = o.OBJ_EsCritico,
                Peso = o.OBJ_Peso,
                CondicionExito = o.OBJ_CondicionExito,
                Orden = o.OBJ_Orden,
                Estatus = o.OBJ_Estatus,
                RowVersion = Convert.ToBase64String(o.RowVersion)
            }).ToListAsync(ct);

        return new ObjectiveListResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<ObjectiveListResponse> ListByStageAsync(Guid tenantId, Guid stageId, string? status, string? evidenceType, bool? isCritical, int page, int pageSize, bool descending, CancellationToken ct)
    {
        if (!await _db.Stages.AsNoTracking().AnyAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId, ct))
        {
            throw new ObjectiveStageNotFoundException();
        }

        var query = _db.Objectives.AsNoTracking().Where(o => o.SEG_IdTenant == tenantId && o.STG_IdStage == stageId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.OBJ_Estatus == status);
        if (!string.IsNullOrWhiteSpace(evidenceType)) query = query.Where(o => o.OBJ_TipoEvidencia == evidenceType);
        if (isCritical.HasValue) query = query.Where(o => o.OBJ_EsCritico == isCritical.Value);

        var total = await query.CountAsync(ct);
        query = descending ? query.OrderByDescending(o => o.OBJ_Orden) : query.OrderBy(o => o.OBJ_Orden);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new ObjectiveListItemResponse
            {
                IdObjective = o.OBJ_IdObjective,
                IdVersion = o.LAB_IdVersion,
                IdStage = o.STG_IdStage,
                Codigo = o.OBJ_Codigo,
                Descripcion = o.OBJ_Descripcion,
                TipoEvidencia = o.OBJ_TipoEvidencia,
                EsCritico = o.OBJ_EsCritico,
                Peso = o.OBJ_Peso,
                CondicionExito = o.OBJ_CondicionExito,
                Orden = o.OBJ_Orden,
                Estatus = o.OBJ_Estatus,
                RowVersion = Convert.ToBase64String(o.RowVersion)
            }).ToListAsync(ct);

        return new ObjectiveListResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<ObjectiveResponse> UpdateAsync(Guid tenantId, Guid objectiveId, string etag, string user, UpdateObjectiveRequest request, CancellationToken ct)
    {
        var obj = await _db.Objectives.Include(o => o.LabVersion).FirstOrDefaultAsync(o => o.SEG_IdTenant == tenantId && o.OBJ_IdObjective == objectiveId, ct)
            ?? throw new ObjectiveNotFoundException();
        EnsureDraft(obj.LabVersion?.LAB_Estatus);
        EnsureEtag(obj.RowVersion, etag);

        if (request.Descripcion is not null) obj.OBJ_Descripcion = request.Descripcion.Trim();
        if (request.TipoEvidencia is not null) obj.OBJ_TipoEvidencia = request.TipoEvidencia.Trim().ToUpperInvariant();
        if (request.EsCritico.HasValue) obj.OBJ_EsCritico = request.EsCritico.Value;
        if (request.Peso.HasValue) obj.OBJ_Peso = request.Peso.Value;
        if (request.CondicionExito is not null) obj.OBJ_CondicionExito = request.CondicionExito.Trim();

        Touch(obj, user);
        await SaveAsync(ct);
        return ToResponse(obj);
    }

    public async Task<ReorderObjectivesResponse> ReorderAsync(Guid tenantId, Guid versionId, string user, ReorderObjectivesRequest request, CancellationToken ct)
    {
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct)
            ?? throw new ObjectiveVersionNotFoundException();
        EnsureDraft(version.LAB_Estatus);

        if (request.StageId.HasValue)
        {
            var stageExists = await _db.Stages.AsNoTracking().AnyAsync(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == versionId && s.STG_IdStage == request.StageId.Value, ct);
            if (!stageExists)
            {
                throw new ObjectiveStageNotFoundException();
            }
        }

        var input = request.Objectives!;
        if (input.Select(x => x.IdObjective).Distinct().Count() != input.Count ||
            input.Select(x => x.Orden).Distinct().Count() != input.Count ||
            input.Any(x => x.Orden <= 0))
        {
            throw new ObjectiveOrderDuplicateException();
        }

        var ids = input.Select(x => x.IdObjective).ToArray();
        var query = _db.Objectives.Where(o => o.SEG_IdTenant == tenantId && o.LAB_IdVersion == versionId && ids.Contains(o.OBJ_IdObjective));
        if (request.StageId.HasValue)
        {
            query = query.Where(o => o.STG_IdStage == request.StageId.Value);
        }
        else
        {
            query = query.Where(o => o.STG_IdStage == null);
        }

        var objectives = await query.ToListAsync(ct);
        if (objectives.Count != input.Count)
        {
            throw new ObjectiveNotFoundException();
        }

        foreach (var item in input)
        {
            EnsureEtag(objectives.Single(o => o.OBJ_IdObjective == item.IdObjective).RowVersion, item.RowVersion);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var offset = Math.Max(objectives.Max(o => o.OBJ_Orden), input.Max(x => x.Orden)) + objectives.Count + 1;
            foreach (var obj in objectives)
            {
                obj.OBJ_Orden += offset;
                Touch(obj, user);
            }
            await _db.SaveChangesAsync(ct);

            foreach (var item in input)
            {
                var obj = objectives.Single(o => o.OBJ_IdObjective == item.IdObjective);
                obj.OBJ_Orden = item.Orden;
            }
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new ObjectiveConcurrencyException();
        }
        catch (DbUpdateException ex) when (IsUnique(ex))
        {
            await tx.RollbackAsync(ct);
            throw new ObjectiveOrderDuplicateException();
        }

        return new ReorderObjectivesResponse
        {
            Items = objectives.OrderBy(o => o.OBJ_Orden).Select(o => new ReorderObjectiveResponseItem
            {
                IdObjective = o.OBJ_IdObjective,
                Orden = o.OBJ_Orden,
                RowVersion = Convert.ToBase64String(o.RowVersion)
            }).ToList()
        };
    }

    public async Task<ObjectiveResponse> SetStatusAsync(Guid tenantId, Guid objectiveId, string etag, string user, string targetStatus, CancellationToken ct)
    {
        var obj = await _db.Objectives.Include(o => o.LabVersion).FirstOrDefaultAsync(o => o.SEG_IdTenant == tenantId && o.OBJ_IdObjective == objectiveId, ct)
            ?? throw new ObjectiveNotFoundException();
        EnsureDraft(obj.LabVersion?.LAB_Estatus);
        EnsureEtag(obj.RowVersion, etag);

        if (obj.OBJ_Estatus != targetStatus &&
            !((obj.OBJ_Estatus == ObjectiveEstatus.Draft && (targetStatus == ObjectiveEstatus.Active || targetStatus == ObjectiveEstatus.Inactive)) ||
              (obj.OBJ_Estatus == ObjectiveEstatus.Active && targetStatus == ObjectiveEstatus.Inactive) ||
              (obj.OBJ_Estatus == ObjectiveEstatus.Inactive && targetStatus == ObjectiveEstatus.Active)))
        {
            throw new ObjectivePreconditionException("INVALID_STATUS_TRANSITION");
        }

        if (obj.OBJ_Estatus != targetStatus)
        {
            obj.OBJ_Estatus = targetStatus;
            Touch(obj, user);
            await SaveAsync(ct);
        }

        return ToResponse(obj);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ObjectiveConcurrencyException();
        }
    }

    private static void EnsureDraft(string? status)
    {
        if (!string.Equals(status, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new ObjectiveVersionNotEditableException();
        }
    }

    private static void EnsureEtag(byte[] current, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            throw new ObjectivePreconditionException("ETAG_REQUIRED");
        }

        if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal))
        {
            throw new ObjectivePreconditionException("ETAG_MISMATCH");
        }
    }

    private static void Touch(LAB_Objective o, string user)
    {
        o.FechaActualizacion = DateTimeOffset.UtcNow;
        o.ActualizadoPor = user;
    }

    private static bool IsUnique(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("UQ_LAB_Objective", StringComparison.OrdinalIgnoreCase) == true;

    private static ObjectiveResponse ToResponse(LAB_Objective o) => new()
    {
        IdObjective = o.OBJ_IdObjective,
        IdVersion = o.LAB_IdVersion,
        IdStage = o.STG_IdStage,
        Codigo = o.OBJ_Codigo,
        Descripcion = o.OBJ_Descripcion,
        TipoEvidencia = o.OBJ_TipoEvidencia,
        EsCritico = o.OBJ_EsCritico,
        Peso = o.OBJ_Peso,
        CondicionExito = o.OBJ_CondicionExito,
        Orden = o.OBJ_Orden,
        Estatus = o.OBJ_Estatus,
        FechaCreacion = o.FechaCreacion,
        CreadoPor = o.CreadoPor,
        FechaActualizacion = o.FechaActualizacion,
        ActualizadoPor = o.ActualizadoPor,
        RowVersion = Convert.ToBase64String(o.RowVersion)
    };
}
