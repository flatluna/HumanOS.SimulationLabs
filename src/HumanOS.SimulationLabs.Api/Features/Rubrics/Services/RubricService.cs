using HumanOS.SimulationLabs.Api.Features.Rubrics.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Rubrics.Services;

public sealed class RubricService : IRubricService
{
    private readonly SimulationLabsDbContext _db;

    public RubricService(SimulationLabsDbContext db) => _db = db;

    public async Task<RubricResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateRubricRequest request, CancellationToken ct)
    {
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct)
            ?? throw new RubricVersionNotFoundException();
        EnsureVersionDraft(version.LAB_Estatus);

        if (await _db.Rubrics.AnyAsync(r => r.SEG_IdTenant == tenantId && r.LAB_IdVersion == versionId, ct))
        {
            throw new RubricAlreadyExistsException();
        }

        var code = request.Codigo!.Trim().ToUpperInvariant();
        if (await _db.Rubrics.AnyAsync(r => r.SEG_IdTenant == tenantId && r.RUB_Codigo == code, ct))
        {
            throw new RubricCodeDuplicateException();
        }

        var rubric = new LAB_Rubric
        {
            RUB_IdRubric = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = versionId,
            RUB_Codigo = code,
            RUB_Nombre = request.Nombre!.Trim(),
            RUB_Descripcion = request.Descripcion!.Trim(),
            RUB_TipoEvaluacion = request.TipoEvaluacion!.Trim().ToUpperInvariant(),
            RUB_EscalaMinima = 1.00m,
            RUB_EscalaMaxima = 10.00m,
            RUB_ScoreMinimoAprobacion = request.ScoreMinimoAprobacion!.Value,
            RUB_RequiereEvidencia = request.RequiereEvidencia!.Value,
            RUB_PermiteFallaCritica = request.PermiteFallaCritica!.Value,
            RUB_MetodoCalculo = request.MetodoCalculo!.Trim().ToUpperInvariant(),
            RUB_InstruccionesEvaluador = request.InstruccionesEvaluador!.Trim(),
            RUB_Estatus = RubricEstatus.Draft,
            RUB_VigenciaDesde = request.VigenciaDesde,
            RUB_VigenciaHasta = request.VigenciaHasta,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = user
        };

        _db.Rubrics.Add(rubric);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UQ_LAB_Rubric_SEG_IdTenant_LAB_IdVersion"))
        {
            throw new RubricAlreadyExistsException();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "UQ_LAB_Rubric_SEG_IdTenant_RUB_Codigo"))
        {
            throw new RubricCodeDuplicateException();
        }

        return ToResponse(rubric);
    }

    public async Task<RubricResponse?> GetByIdAsync(Guid tenantId, Guid rubricId, CancellationToken ct) =>
        (await _db.Rubrics.AsNoTracking().FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct)) is { } rubric
            ? ToResponse(rubric)
            : null;

    public async Task<RubricResponse?> GetByVersionAsync(Guid tenantId, Guid versionId, CancellationToken ct)
    {
        if (!await _db.LabVersions.AsNoTracking().AnyAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct))
        {
            throw new RubricVersionNotFoundException();
        }

        var rubric = await _db.Rubrics.AsNoTracking().FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.LAB_IdVersion == versionId, ct);
        return rubric is null ? null : ToResponse(rubric);
    }

    public async Task<RubricResponse> UpdateAsync(Guid tenantId, Guid rubricId, string etag, string user, UpdateRubricRequest request, CancellationToken ct)
    {
        var rubric = await _db.Rubrics.Include(r => r.LabVersion).FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct)
            ?? throw new RubricNotFoundException();
        EnsureVersionDraft(rubric.LabVersion?.LAB_Estatus);
        EnsureRubricDraft(rubric.RUB_Estatus);
        EnsureEtag(rubric.RowVersion, etag);

        if (request.Nombre is not null) rubric.RUB_Nombre = request.Nombre.Trim();
        if (request.Descripcion is not null) rubric.RUB_Descripcion = request.Descripcion.Trim();
        if (request.TipoEvaluacion is not null) rubric.RUB_TipoEvaluacion = request.TipoEvaluacion.Trim().ToUpperInvariant();
        if (request.ScoreMinimoAprobacion.HasValue) rubric.RUB_ScoreMinimoAprobacion = request.ScoreMinimoAprobacion.Value;
        if (request.RequiereEvidencia.HasValue) rubric.RUB_RequiereEvidencia = request.RequiereEvidencia.Value;
        if (request.PermiteFallaCritica.HasValue) rubric.RUB_PermiteFallaCritica = request.PermiteFallaCritica.Value;
        if (request.MetodoCalculo is not null) rubric.RUB_MetodoCalculo = request.MetodoCalculo.Trim().ToUpperInvariant();
        if (request.InstruccionesEvaluador is not null) rubric.RUB_InstruccionesEvaluador = request.InstruccionesEvaluador.Trim();
        if (request.VigenciaDesde.HasValue) rubric.RUB_VigenciaDesde = request.VigenciaDesde;
        if (request.VigenciaHasta.HasValue) rubric.RUB_VigenciaHasta = request.VigenciaHasta;

        Touch(rubric, user);
        await SaveAsync(ct);
        return ToResponse(rubric);
    }

    public async Task<RubricResponse> ApproveAsync(Guid tenantId, Guid rubricId, string etag, string user, RubricActionRequest request, CancellationToken ct)
    {
        var rubric = await _db.Rubrics.Include(r => r.LabVersion).FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct)
            ?? throw new RubricNotFoundException();
        EnsureVersionDraft(rubric.LabVersion?.LAB_Estatus);
        EnsureEtag(rubric.RowVersion, etag);
        EnsureTransition(rubric.RUB_Estatus, RubricEstatus.Approved);

        if (string.IsNullOrWhiteSpace(rubric.RUB_Nombre) || string.IsNullOrWhiteSpace(rubric.RUB_Descripcion) ||
            !RubricTipoEvaluacion.Allowed.Contains(rubric.RUB_TipoEvaluacion) ||
            rubric.RUB_ScoreMinimoAprobacion < 1.00m || rubric.RUB_ScoreMinimoAprobacion > 10.00m ||
            !RubricMetodoCalculo.Allowed.Contains(rubric.RUB_MetodoCalculo) ||
            string.IsNullOrWhiteSpace(rubric.RUB_InstruccionesEvaluador))
        {
            throw new RubricNotEditableException();
        }

        rubric.RUB_Estatus = RubricEstatus.Approved;
        Touch(rubric, user);
        await SaveAsync(ct);
        return ToResponse(rubric);
    }

    public async Task<RubricResponse> PublishAsync(Guid tenantId, Guid rubricId, string etag, string user, PublishRubricRequest request, CancellationToken ct)
    {
        var rubric = await _db.Rubrics.Include(r => r.LabVersion).FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct)
            ?? throw new RubricNotFoundException();
        EnsureEtag(rubric.RowVersion, etag);

        if (rubric.RUB_Estatus == RubricEstatus.Published)
        {
            return ToResponse(rubric);
        }

        var versionStatus = rubric.LabVersion?.LAB_Estatus;
        if (!string.Equals(versionStatus, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(versionStatus, LabVersionEstatus.Approved, StringComparison.OrdinalIgnoreCase))
        {
            throw new RubricVersionNotEditableException();
        }

        EnsureTransition(rubric.RUB_Estatus, RubricEstatus.Published);

        rubric.RUB_Estatus = RubricEstatus.Published;
        rubric.RUB_VigenciaDesde = request.VigenciaDesde;
        rubric.RUB_VigenciaHasta = request.VigenciaHasta;
        Touch(rubric, user);
        await SaveAsync(ct);
        return ToResponse(rubric);
    }

    public async Task<RubricResponse> RetireAsync(Guid tenantId, Guid rubricId, string etag, string user, RetireRubricRequest request, CancellationToken ct)
    {
        var rubric = await _db.Rubrics.FirstOrDefaultAsync(r => r.SEG_IdTenant == tenantId && r.RUB_IdRubric == rubricId, ct)
            ?? throw new RubricNotFoundException();
        EnsureEtag(rubric.RowVersion, etag);

        if (rubric.RUB_Estatus == RubricEstatus.Retired)
        {
            return ToResponse(rubric);
        }

        EnsureTransition(rubric.RUB_Estatus, RubricEstatus.Retired);

        rubric.RUB_Estatus = RubricEstatus.Retired;
        if (request.VigenciaHasta.HasValue) rubric.RUB_VigenciaHasta = request.VigenciaHasta;
        Touch(rubric, user);
        await SaveAsync(ct);
        return ToResponse(rubric);
    }

    private static void EnsureTransition(string current, string target)
    {
        var allowed = current switch
        {
            RubricEstatus.Draft => target is RubricEstatus.Approved or RubricEstatus.Retired,
            RubricEstatus.Approved => target is RubricEstatus.Published or RubricEstatus.Retired,
            RubricEstatus.Published => target is RubricEstatus.Retired,
            _ => false
        };

        if (!allowed)
        {
            throw new RubricPreconditionException("INVALID_STATUS_TRANSITION");
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
            throw new RubricConcurrencyException();
        }
    }

    private static void EnsureVersionDraft(string? status)
    {
        if (!string.Equals(status, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new RubricVersionNotEditableException();
        }
    }

    private static void EnsureRubricDraft(string status)
    {
        if (!string.Equals(status, RubricEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new RubricNotEditableException();
        }
    }

    private static void EnsureEtag(byte[] current, string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            throw new RubricPreconditionException("ETAG_REQUIRED");
        }

        if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal))
        {
            throw new RubricPreconditionException("ETAG_MISMATCH");
        }
    }

    private static void Touch(LAB_Rubric r, string user)
    {
        r.FechaActualizacion = DateTimeOffset.UtcNow;
        r.ActualizadoPor = user;
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string indexName) =>
        ex.InnerException?.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase) == true;

    private static RubricResponse ToResponse(LAB_Rubric r) => new()
    {
        IdRubric = r.RUB_IdRubric,
        IdVersion = r.LAB_IdVersion,
        Codigo = r.RUB_Codigo,
        Nombre = r.RUB_Nombre,
        Descripcion = r.RUB_Descripcion,
        TipoEvaluacion = r.RUB_TipoEvaluacion,
        EscalaMinima = r.RUB_EscalaMinima,
        EscalaMaxima = r.RUB_EscalaMaxima,
        ScoreMinimoAprobacion = r.RUB_ScoreMinimoAprobacion,
        RequiereEvidencia = r.RUB_RequiereEvidencia,
        PermiteFallaCritica = r.RUB_PermiteFallaCritica,
        MetodoCalculo = r.RUB_MetodoCalculo,
        InstruccionesEvaluador = r.RUB_InstruccionesEvaluador,
        Estatus = r.RUB_Estatus,
        VigenciaDesde = r.RUB_VigenciaDesde,
        VigenciaHasta = r.RUB_VigenciaHasta,
        FechaCreacion = r.FechaCreacion,
        CreadoPor = r.CreadoPor,
        FechaActualizacion = r.FechaActualizacion,
        ActualizadoPor = r.ActualizadoPor,
        RowVersion = Convert.ToBase64String(r.RowVersion)
    };
}
