using HumanOS.SimulationLabs.Api.Features.RubricCriteria.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.RubricCriteria.Services;

public sealed class RubricCriterionService : IRubricCriterionService
{
    private readonly SimulationLabsDbContext _db;

    public RubricCriterionService(SimulationLabsDbContext db) => _db = db;

    public async Task<RubricCriterionResponse> CreateAsync(Guid tenantId, Guid rubricId, string user, CreateRubricCriterionRequest request, CancellationToken ct)
    {
        var rubric = await _db.Rubrics.AsNoTracking().FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct)
            ?? throw new RubricCriterionRubricNotFoundException();

        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == rubric.LAB_IdVersion, ct)
            ?? throw new RubricCriterionVersionNotFoundException();

        EnsureVersionDraft(version.LAB_Estatus);
        EnsureRubricDraft(rubric.RUB_Estatus);

        if (request.ObjectiveId.HasValue)
        {
            var objExists = await _db.Objectives.AsNoTracking()
                .AnyAsync(o => o.SEG_IdTenant == tenantId && o.LAB_IdVersion == rubric.LAB_IdVersion && o.OBJ_IdObjective == request.ObjectiveId.Value, ct);
            if (!objExists)
            {
                throw new RubricCriterionObjectiveNotFoundException();
            }
        }

        if (request.ExpectedMomentId.HasValue)
        {
            var mom = await _db.ExpectedMoments.AsNoTracking()
                .FirstOrDefaultAsync(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == rubric.LAB_IdVersion && m.MOM_IdExpectedMoment == request.ExpectedMomentId.Value, ct)
                ?? throw new RubricCriterionExpectedMomentNotFoundException();

            if (request.ObjectiveId.HasValue && mom.OBJ_IdObjective.HasValue && mom.OBJ_IdObjective.Value != request.ObjectiveId.Value)
            {
                throw new RubricCriterionObjectiveMomentMismatchException();
            }
        }

        var code = request.Codigo!.Trim().ToUpperInvariant();
        if (await _db.RubricCriteria.AnyAsync(c => c.SEG_IdTenant == tenantId && c.RUB_IdRubric == rubricId && c.CRT_Codigo == code, ct))
        {
            throw new RubricCriterionCodeDuplicateException();
        }

        var order = (await _db.RubricCriteria
            .Where(c => c.SEG_IdTenant == tenantId && c.RUB_IdRubric == rubricId)
            .MaxAsync(c => (int?)c.CRT_Orden, ct) ?? 0) + 1;

        var criterion = new LAB_RubricCriterion
        {
            CRT_IdCriterion = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = rubric.LAB_IdVersion,
            RUB_IdRubric = rubricId,
            OBJ_IdObjective = request.ObjectiveId,
            MOM_IdExpectedMoment = request.ExpectedMomentId,
            CRT_Codigo = code,
            CRT_Nombre = request.Nombre!.Trim(),
            CRT_Descripcion = request.Descripcion!.Trim(),
            CRT_TipoEvidencia = request.TipoEvidencia!.Trim().ToUpperInvariant(),
            CRT_Peso = request.Peso!.Value,
            CRT_ScoreMinimoEsperado = request.ScoreMinimoEsperado ?? 7.00m,
            CRT_EsCritico = request.EsCritico!.Value,
            CRT_IndicadoresPositivos = request.IndicadoresPositivos!.Trim(),
            CRT_IndicadoresNegativos = request.IndicadoresNegativos?.Trim(),
            CRT_ErrorCritico = request.ErrorCritico?.Trim(),
            CRT_RecomendacionBase = request.RecomendacionBase?.Trim(),
            CRT_Orden = order,
            CRT_Estatus = CriterionEstatus.Draft,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = user
        };

        _db.RubricCriteria.Add(criterion);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UQ_LAB_RubricCriterion_Tenant_Rubric_Codigo"))
        {
            throw new RubricCriterionCodeDuplicateException();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UQ_LAB_RubricCriterion_Tenant_Rubric_Orden"))
        {
            throw new RubricCriterionOrderDuplicateException();
        }

        return ToResponse(criterion);
    }

    public async Task<RubricCriterionResponse?> GetByIdAsync(Guid tenantId, Guid criterionId, CancellationToken ct)
    {
        var criterion = await _db.RubricCriteria.AsNoTracking()
            .FirstOrDefaultAsync(c => c.SEG_IdTenant == tenantId && c.CRT_IdCriterion == criterionId, ct);

        return criterion is null ? null : ToResponse(criterion);
    }

    public async Task<RubricCriterionListResponse> ListByRubricAsync(
        Guid tenantId,
        Guid rubricId,
        Guid? objectiveId,
        Guid? expectedMomentId,
        string? tipoEvidencia,
        bool? critico,
        string? estatus,
        int page,
        int pageSize,
        string? sortDirection,
        CancellationToken ct)
    {
        var rubricExists = await _db.Rubrics.AsNoTracking()
            .AnyAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct);
        if (!rubricExists)
        {
            throw new RubricCriterionRubricNotFoundException();
        }

        var query = _db.RubricCriteria.AsNoTracking()
            .Where(c => c.SEG_IdTenant == tenantId && c.RUB_IdRubric == rubricId);

        if (objectiveId.HasValue) query = query.Where(c => c.OBJ_IdObjective == objectiveId.Value);
        if (expectedMomentId.HasValue) query = query.Where(c => c.MOM_IdExpectedMoment == expectedMomentId.Value);
        if (!string.IsNullOrWhiteSpace(tipoEvidencia)) query = query.Where(c => c.CRT_TipoEvidencia == tipoEvidencia.Trim().ToUpperInvariant());
        if (critico.HasValue) query = query.Where(c => c.CRT_EsCritico == critico.Value);
        if (!string.IsNullOrWhiteSpace(estatus)) query = query.Where(c => c.CRT_Estatus == estatus.Trim().ToUpperInvariant());

        var total = await query.CountAsync(ct);

        var isDesc = string.Equals(sortDirection, "DESC", StringComparison.OrdinalIgnoreCase);
        query = isDesc ? query.OrderByDescending(c => c.CRT_Orden) : query.OrderBy(c => c.CRT_Orden);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new RubricCriterionListItemResponse
            {
                IdCriterion = c.CRT_IdCriterion,
                ObjectiveId = c.OBJ_IdObjective,
                ExpectedMomentId = c.MOM_IdExpectedMoment,
                Codigo = c.CRT_Codigo,
                Nombre = c.CRT_Nombre,
                TipoEvidencia = c.CRT_TipoEvidencia,
                Peso = c.CRT_Peso,
                ScoreMinimoEsperado = c.CRT_ScoreMinimoEsperado,
                EsCritico = c.CRT_EsCritico,
                Orden = c.CRT_Orden,
                Estatus = c.CRT_Estatus,
                RowVersion = Convert.ToBase64String(c.RowVersion)
            })
            .ToListAsync(ct);

        return new RubricCriterionListResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<RubricCriterionResponse> UpdateAsync(Guid tenantId, Guid criterionId, string etag, string user, UpdateRubricCriterionRequest request, CancellationToken ct)
    {
        var criterion = await _db.RubricCriteria
            .Include(c => c.Rubric)
            .ThenInclude(r => r.LabVersion)
            .FirstOrDefaultAsync(c => c.SEG_IdTenant == tenantId && c.CRT_IdCriterion == criterionId, ct)
            ?? throw new RubricCriterionNotFoundException();

        EnsureVersionDraft(criterion.Rubric?.LabVersion?.LAB_Estatus);
        EnsureRubricDraft(criterion.Rubric?.RUB_Estatus);
        EnsureCriterionDraft(criterion.CRT_Estatus);
        EnsureEtag(criterion.RowVersion, etag);

        var targetObjectiveId = request.ObjectiveId.IsSpecified ? request.ObjectiveId.Value : criterion.OBJ_IdObjective;
        var targetMomentId = request.ExpectedMomentId.IsSpecified ? request.ExpectedMomentId.Value : criterion.MOM_IdExpectedMoment;

        if (request.ObjectiveId.IsSpecified && request.ObjectiveId.Value.HasValue)
        {
            var objExists = await _db.Objectives.AsNoTracking()
                .AnyAsync(o => o.SEG_IdTenant == tenantId && o.LAB_IdVersion == criterion.LAB_IdVersion && o.OBJ_IdObjective == request.ObjectiveId.Value.Value, ct);
            if (!objExists)
            {
                throw new RubricCriterionObjectiveNotFoundException();
            }
        }

        if (request.ExpectedMomentId.IsSpecified && request.ExpectedMomentId.Value.HasValue)
        {
            var momExists = await _db.ExpectedMoments.AsNoTracking()
                .AnyAsync(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == criterion.LAB_IdVersion && m.MOM_IdExpectedMoment == request.ExpectedMomentId.Value.Value, ct);
            if (!momExists)
            {
                throw new RubricCriterionExpectedMomentNotFoundException();
            }
        }

        if (targetObjectiveId.HasValue && targetMomentId.HasValue)
        {
            var mom = await _db.ExpectedMoments.AsNoTracking()
                .FirstOrDefaultAsync(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == criterion.LAB_IdVersion && m.MOM_IdExpectedMoment == targetMomentId.Value, ct);
            if (mom is not null && mom.OBJ_IdObjective.HasValue && mom.OBJ_IdObjective.Value != targetObjectiveId.Value)
            {
                throw new RubricCriterionObjectiveMomentMismatchException();
            }
        }

        if (request.ObjectiveId.IsSpecified) criterion.OBJ_IdObjective = request.ObjectiveId.Value;
        if (request.ExpectedMomentId.IsSpecified) criterion.MOM_IdExpectedMoment = request.ExpectedMomentId.Value;
        if (request.Nombre is not null) criterion.CRT_Nombre = request.Nombre.Trim();
        if (request.Descripcion is not null) criterion.CRT_Descripcion = request.Descripcion.Trim();
        if (request.TipoEvidencia is not null) criterion.CRT_TipoEvidencia = request.TipoEvidencia.Trim().ToUpperInvariant();
        if (request.Peso.HasValue) criterion.CRT_Peso = request.Peso.Value;
        if (request.ScoreMinimoEsperado.HasValue) criterion.CRT_ScoreMinimoEsperado = request.ScoreMinimoEsperado.Value;
        if (request.EsCritico.HasValue) criterion.CRT_EsCritico = request.EsCritico.Value;
        if (request.IndicadoresPositivos is not null) criterion.CRT_IndicadoresPositivos = request.IndicadoresPositivos.Trim();
        if (request.IndicadoresNegativos.IsSpecified) criterion.CRT_IndicadoresNegativos = request.IndicadoresNegativos.Value?.Trim();
        if (request.ErrorCritico.IsSpecified) criterion.CRT_ErrorCritico = request.ErrorCritico.Value?.Trim();
        if (request.RecomendacionBase.IsSpecified) criterion.CRT_RecomendacionBase = request.RecomendacionBase.Value?.Trim();

        Touch(criterion, user);
        await SaveAsync(ct);
        return ToResponse(criterion);
    }

    public async Task<ReorderRubricCriteriaResponse> ReorderAsync(Guid tenantId, Guid rubricId, string user, ReorderRubricCriteriaRequest request, CancellationToken ct)
    {
        var rubric = await _db.Rubrics.Include(r => r.LabVersion).FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct)
            ?? throw new RubricCriterionRubricNotFoundException();

        EnsureVersionDraft(rubric.LabVersion?.LAB_Estatus);
        EnsureRubricDraft(rubric.RUB_Estatus);

        var input = request.Criteria!;
        if (input.Select(x => x.IdCriterion).Distinct().Count() != input.Count ||
            input.Select(x => x.Orden).Distinct().Count() != input.Count ||
            input.Any(x => x.Orden <= 0))
        {
            throw new RubricCriterionOrderDuplicateException();
        }

        var ids = input.Select(x => x.IdCriterion).ToArray();
        var criteria = await _db.RubricCriteria
            .Where(c => c.SEG_IdTenant == tenantId && c.RUB_IdRubric == rubricId && ids.Contains(c.CRT_IdCriterion))
            .ToListAsync(ct);

        if (criteria.Count != input.Count)
        {
            throw new RubricCriterionNotFoundException();
        }

        foreach (var item in input)
        {
            EnsureEtag(criteria.Single(c => c.CRT_IdCriterion == item.IdCriterion).RowVersion, item.RowVersion);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var offset = Math.Max(criteria.Max(c => c.CRT_Orden), input.Max(x => x.Orden)) + criteria.Count + 1;
            foreach (var c in criteria)
            {
                c.CRT_Orden += offset;
                Touch(c, user);
            }
            await _db.SaveChangesAsync(ct);

            foreach (var item in input)
            {
                var c = criteria.Single(x => x.CRT_IdCriterion == item.IdCriterion);
                c.CRT_Orden = item.Orden;
                Touch(c, user);
            }
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(ct);
            throw new RubricCriterionConcurrencyException();
        }
        catch (DbUpdateException ex) when (IsUnique(ex))
        {
            await tx.RollbackAsync(ct);
            throw new RubricCriterionOrderDuplicateException();
        }

        return new ReorderRubricCriteriaResponse
        {
            Items = criteria.OrderBy(c => c.CRT_Orden).Select(c => new ReorderRubricCriterionResponseItem
            {
                IdCriterion = c.CRT_IdCriterion,
                Orden = c.CRT_Orden,
                RowVersion = Convert.ToBase64String(c.RowVersion)
            }).ToList()
        };
    }

    public async Task<RubricCriterionResponse> ActivateAsync(Guid tenantId, Guid criterionId, string etag, string user, RubricCriterionActionRequest request, CancellationToken ct)
    {
        var criterion = await _db.RubricCriteria
            .Include(c => c.Rubric)
            .ThenInclude(r => r.LabVersion)
            .FirstOrDefaultAsync(c => c.SEG_IdTenant == tenantId && c.CRT_IdCriterion == criterionId, ct)
            ?? throw new RubricCriterionNotFoundException();

        EnsureVersionDraft(criterion.Rubric?.LabVersion?.LAB_Estatus);
        EnsureRubricDraft(criterion.Rubric?.RUB_Estatus);
        EnsureEtag(criterion.RowVersion, etag);

        if (criterion.CRT_Estatus == CriterionEstatus.Active)
        {
            return ToResponse(criterion);
        }

        if (criterion.CRT_Estatus != CriterionEstatus.Draft && criterion.CRT_Estatus != CriterionEstatus.Inactive)
        {
            throw new RubricCriterionPreconditionException("INVALID_STATUS_TRANSITION");
        }

        criterion.CRT_Estatus = CriterionEstatus.Active;
        Touch(criterion, user);
        await SaveAsync(ct);
        return ToResponse(criterion);
    }

    public async Task<RubricCriterionResponse> InactivateAsync(Guid tenantId, Guid criterionId, string etag, string user, RubricCriterionActionRequest request, CancellationToken ct)
    {
        var criterion = await _db.RubricCriteria
            .Include(c => c.Rubric)
            .ThenInclude(r => r.LabVersion)
            .FirstOrDefaultAsync(c => c.SEG_IdTenant == tenantId && c.CRT_IdCriterion == criterionId, ct)
            ?? throw new RubricCriterionNotFoundException();

        EnsureVersionDraft(criterion.Rubric?.LabVersion?.LAB_Estatus);
        EnsureRubricDraft(criterion.Rubric?.RUB_Estatus);
        EnsureEtag(criterion.RowVersion, etag);

        if (criterion.CRT_Estatus == CriterionEstatus.Inactive)
        {
            return ToResponse(criterion);
        }

        if (criterion.CRT_Estatus != CriterionEstatus.Draft && criterion.CRT_Estatus != CriterionEstatus.Active)
        {
            throw new RubricCriterionPreconditionException("INVALID_STATUS_TRANSITION");
        }

        criterion.CRT_Estatus = CriterionEstatus.Inactive;
        Touch(criterion, user);
        await SaveAsync(ct);
        return ToResponse(criterion);
    }

    private static void EnsureVersionDraft(string? status)
    {
        if (!string.Equals(status, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new RubricCriterionVersionNotEditableException();
        }
    }

    private static void EnsureRubricDraft(string? status)
    {
        if (!string.Equals(status, RubricEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new RubricCriterionRubricNotEditableException();
        }
    }

    private static void EnsureCriterionDraft(string? status)
    {
        if (!string.Equals(status, CriterionEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new RubricCriterionNotEditableException();
        }
    }

    private static void EnsureEtag(byte[] rowVersion, string? ifMatch)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            throw new RubricCriterionPreconditionException("ETAG_REQUIRED");
        }

        var clean = ifMatch.Trim().Trim('"');
        if (!string.Equals(Convert.ToBase64String(rowVersion), clean, StringComparison.Ordinal))
        {
            throw new RubricCriterionPreconditionException("ETAG_MISMATCH");
        }
    }

    private static void Touch(LAB_RubricCriterion c, string user)
    {
        c.FechaActualizacion = DateTimeOffset.UtcNow;
        c.ActualizadoPor = user;
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RubricCriterionConcurrencyException();
        }
    }

    private static bool IsUnique(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("UQ_LAB_RubricCriterion", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsUniqueViolation(DbUpdateException ex, string indexName) =>
        ex.InnerException?.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase) == true ||
        ex.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);

    private static RubricCriterionResponse ToResponse(LAB_RubricCriterion c) => new()
    {
        IdCriterion = c.CRT_IdCriterion,
        IdRubric = c.RUB_IdRubric,
        IdVersion = c.LAB_IdVersion,
        ObjectiveId = c.OBJ_IdObjective,
        ExpectedMomentId = c.MOM_IdExpectedMoment,
        Codigo = c.CRT_Codigo,
        Nombre = c.CRT_Nombre,
        Descripcion = c.CRT_Descripcion,
        TipoEvidencia = c.CRT_TipoEvidencia,
        Peso = c.CRT_Peso,
        ScoreMinimoEsperado = c.CRT_ScoreMinimoEsperado,
        EsCritico = c.CRT_EsCritico,
        IndicadoresPositivos = c.CRT_IndicadoresPositivos,
        IndicadoresNegativos = c.CRT_IndicadoresNegativos,
        ErrorCritico = c.CRT_ErrorCritico,
        RecomendacionBase = c.CRT_RecomendacionBase,
        Orden = c.CRT_Orden,
        Estatus = c.CRT_Estatus,
        FechaCreacion = c.FechaCreacion,
        CreadoPor = c.CreadoPor,
        FechaActualizacion = c.FechaActualizacion,
        ActualizadoPor = c.ActualizadoPor,
        RowVersion = Convert.ToBase64String(c.RowVersion)
    };
}
