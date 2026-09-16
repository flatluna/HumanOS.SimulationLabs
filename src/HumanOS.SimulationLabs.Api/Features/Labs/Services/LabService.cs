using HumanOS.SimulationLabs.Api.Features.Labs.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Services;

public sealed class LabService : ILabService
{
    private readonly SimulationLabsDbContext _db;
    private readonly ILogger<LabService> _logger;

    public LabService(SimulationLabsDbContext db, ILogger<LabService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<LabResponse> CreateAsync(Guid tenantId, Guid userId, string createdBy, CreateLabRequest request, CancellationToken cancellationToken)
    {
        var codigo = request.Codigo!.Trim().ToUpperInvariant();
        var nombre = request.Nombre!.Trim();
        var dominio = request.Dominio!.Trim();

        var exists = await _db.Labs
            .AsNoTracking()
            .AnyAsync(l => l.SEG_IdTenant == tenantId && l.LAB_Codigo == codigo, cancellationToken);

        if (exists)
        {
            throw new LabDuplicateCodeException();
        }

        var lab = new LAB_Lab
        {
            LAB_IdLab = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_Codigo = codigo,
            LAB_Nombre = nombre,
            LAB_Descripcion = request.Descripcion!.Trim(),
            LAB_Tipo = request.Tipo!,
            LAB_Dominio = dominio,
            LAB_Estatus = LabEstatus.Draft,
            LAB_OwnerId = request.OwnerId!.Value,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = createdBy,
        };

        _db.Labs.Add(lab);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new LabDuplicateCodeException();
        }

        return ToResponse(lab);
    }

    public async Task<LabResponse?> GetByIdAsync(Guid tenantId, Guid idLab, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab, cancellationToken);

        return lab is null ? null : ToResponse(lab);
    }

    public async Task<PagedResult<LabListItemResponse>> ListAsync(Guid tenantId, LabListQuery query, CancellationToken cancellationToken)
    {
        var labs = _db.Labs.AsNoTracking().Where(l => l.SEG_IdTenant == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            labs = labs.Where(l =>
                EF.Functions.Like(l.LAB_Codigo, $"%{search}%") ||
                EF.Functions.Like(l.LAB_Nombre, $"%{search}%") ||
                EF.Functions.Like(l.LAB_Descripcion, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Tipo))
        {
            labs = labs.Where(l => l.LAB_Tipo == query.Tipo);
        }

        if (!string.IsNullOrWhiteSpace(query.Dominio))
        {
            labs = labs.Where(l => l.LAB_Dominio == query.Dominio);
        }

        if (!string.IsNullOrWhiteSpace(query.Estatus))
        {
            labs = labs.Where(l => l.LAB_Estatus == query.Estatus);
        }

        var totalItems = await labs.CountAsync(cancellationToken);

        var descending = string.Equals(query.SortDirection, "DESC", StringComparison.OrdinalIgnoreCase);
        labs = (query.SortBy?.ToLowerInvariant()) switch
        {
            "codigo" => descending ? labs.OrderByDescending(l => l.LAB_Codigo) : labs.OrderBy(l => l.LAB_Codigo),
            "nombre" => descending ? labs.OrderByDescending(l => l.LAB_Nombre) : labs.OrderBy(l => l.LAB_Nombre),
            "tipo" => descending ? labs.OrderByDescending(l => l.LAB_Tipo) : labs.OrderBy(l => l.LAB_Tipo),
            "dominio" => descending ? labs.OrderByDescending(l => l.LAB_Dominio) : labs.OrderBy(l => l.LAB_Dominio),
            "estatus" => descending ? labs.OrderByDescending(l => l.LAB_Estatus) : labs.OrderBy(l => l.LAB_Estatus),
            "fechaactualizacion" => descending ? labs.OrderByDescending(l => l.FechaActualizacion) : labs.OrderBy(l => l.FechaActualizacion),
            _ => descending ? labs.OrderByDescending(l => l.FechaCreacion) : labs.OrderBy(l => l.FechaCreacion),
        };

        var items = await labs
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new LabListItemResponse
            {
                IdLab = l.LAB_IdLab,
                Codigo = l.LAB_Codigo,
                Nombre = l.LAB_Nombre,
                Descripcion = l.LAB_Descripcion,
                Tipo = l.LAB_Tipo,
                Dominio = l.LAB_Dominio,
                Estatus = l.LAB_Estatus,
                OwnerId = l.LAB_OwnerId,
                FechaCreacion = l.FechaCreacion,
                FechaActualizacion = l.FechaActualizacion,
                RowVersion = Convert.ToBase64String(l.RowVersion),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LabListItemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize),
        };
    }

    public async Task<LabResponse> UpdateAsync(Guid tenantId, Guid idLab, string ifMatchRowVersion, string updatedBy, UpdateLabRequest request, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.FirstOrDefaultAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab, cancellationToken);
        if (lab is null)
        {
            throw new LabNotFoundException();
        }

        var currentRowVersion = Convert.ToBase64String(lab.RowVersion);
        if (string.IsNullOrWhiteSpace(ifMatchRowVersion) || !string.Equals(currentRowVersion, ifMatchRowVersion, StringComparison.Ordinal))
        {
            throw new LabPreconditionFailedException();
        }

        if (lab.LAB_Estatus != LabEstatus.Draft)
        {
            throw new LabNotEditableException(lab.LAB_Estatus);
        }

        if (request.Nombre is not null)
        {
            lab.LAB_Nombre = request.Nombre.Trim();
        }

        if (request.Descripcion is not null)
        {
            lab.LAB_Descripcion = request.Descripcion.Trim();
        }

        if (request.Tipo is not null)
        {
            lab.LAB_Tipo = request.Tipo;
        }

        if (request.Dominio is not null)
        {
            lab.LAB_Dominio = request.Dominio.Trim();
        }

        if (request.OwnerId is not null)
        {
            lab.LAB_OwnerId = request.OwnerId.Value;
        }

        lab.FechaActualizacion = DateTimeOffset.UtcNow;
        lab.ActualizadoPor = updatedBy;

        _db.Entry(lab).Property(l => l.RowVersion).OriginalValue = Convert.FromBase64String(ifMatchRowVersion);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LabConcurrencyException();
        }

        return ToResponse(lab);
    }

    public async Task<InactivateLabResponse> InactivateAsync(Guid tenantId, Guid idLab, string ifMatchRowVersion, string updatedBy, InactivateLabRequest request, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.FirstOrDefaultAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab, cancellationToken);
        if (lab is null)
        {
            throw new LabNotFoundException();
        }

        var currentRowVersion = Convert.ToBase64String(lab.RowVersion);
        if (string.IsNullOrWhiteSpace(ifMatchRowVersion) || !string.Equals(currentRowVersion, ifMatchRowVersion, StringComparison.Ordinal))
        {
            throw new LabPreconditionFailedException();
        }

        if (lab.LAB_Estatus == LabEstatus.Retired)
        {
            // Idempotent: already retired, return current state without modifying it.
            return new InactivateLabResponse
            {
                IdLab = lab.LAB_IdLab,
                Estatus = lab.LAB_Estatus,
                FechaActualizacion = lab.FechaActualizacion ?? lab.FechaCreacion,
                ActualizadoPor = lab.ActualizadoPor ?? lab.CreadoPor,
                RowVersion = currentRowVersion,
            };
        }

        lab.LAB_Estatus = LabEstatus.Retired;
        lab.FechaActualizacion = DateTimeOffset.UtcNow;
        lab.ActualizadoPor = updatedBy;

        _db.Entry(lab).Property(l => l.RowVersion).OriginalValue = Convert.FromBase64String(ifMatchRowVersion);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LabConcurrencyException();
        }

        // Auditoría: el motivo de inactivación aún no tiene tabla dedicada; se registra solo en el log
        // estructurado hasta que el módulo de auditoría exista.
        _logger.LogInformation(
            "Lab {IdLab} inactivated by {UpdatedBy}. Motivo: {Motivo}", lab.LAB_IdLab, updatedBy, request.Motivo!.Trim());

        return new InactivateLabResponse
        {
            IdLab = lab.LAB_IdLab,
            Estatus = lab.LAB_Estatus,
            FechaActualizacion = lab.FechaActualizacion.Value,
            ActualizadoPor = lab.ActualizadoPor,
            RowVersion = Convert.ToBase64String(lab.RowVersion),
        };
    }

    public async Task DeleteAsync(Guid tenantId, Guid idLab, CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.AsNoTracking().FirstOrDefaultAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab, cancellationToken);
        if (lab is null)
        {
            throw new LabNotFoundException();
        }

        var versionIds = await _db.LabVersions.AsNoTracking()
            .Where(v => v.SEG_IdTenant == tenantId && v.LAB_IdLab == idLab)
            .Select(v => v.LAB_IdVersion)
            .ToListAsync(cancellationToken);

        var attemptIds = await _db.Attempts.AsNoTracking()
            .Where(a => a.SEG_IdTenant == tenantId && versionIds.Contains(a.LAB_IdVersion))
            .Select(a => a.ATT_IdAttempt)
            .ToListAsync(cancellationToken);

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Delete in FK-safe order (all FKs in this schema are Restrict, not Cascade) —
                // deepest children first, up to LAB_Lab itself. LAB_AttemptEvaluation has no DB-level
                // FK but must still be cleaned up (orphans would otherwise reference a deleted Attempt).
                await _db.AttemptEvaluations.Where(e => e.SEG_IdTenant == tenantId && attemptIds.Contains(e.ATT_IdAttempt)).ExecuteDeleteAsync(cancellationToken);
                await _db.ArtifactSubmissions.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.UserActions.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.ConversationTurns.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.Attempts.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                // TestedSkill references LAB_RubricCriterion too — must go before RubricCriteria.
                await _db.TestedSkills.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.RubricCriteria.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.Rubrics.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.ExpectedMoments.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.Objectives.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.SimulatedActors.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.Scenarios.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                await _db.Stages.Where(e => e.SEG_IdTenant == tenantId && versionIds.Contains(e.LAB_IdVersion)).ExecuteDeleteAsync(cancellationToken);
                // Enrollment references LAB_Lab directly (not version-scoped) — delete anytime before Labs.
                await _db.Enrollments.Where(e => e.SEG_IdTenant == tenantId && e.LAB_IdLab == idLab).ExecuteDeleteAsync(cancellationToken);
                await _db.LabVersions.Where(v => v.SEG_IdTenant == tenantId && v.LAB_IdLab == idLab).ExecuteDeleteAsync(cancellationToken);
                await _db.Labs.Where(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab).ExecuteDeleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });

        _logger.LogInformation("Lab {IdLab} permanently deleted ({VersionCount} version(s)).", idLab, versionIds.Count);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;

    private static LabResponse ToResponse(LAB_Lab lab) => new()
    {
        IdLab = lab.LAB_IdLab,
        Codigo = lab.LAB_Codigo,
        Nombre = lab.LAB_Nombre,
        Descripcion = lab.LAB_Descripcion,
        Tipo = lab.LAB_Tipo,
        Arquetipo = lab.LAB_Arquetipo,
        Dominio = lab.LAB_Dominio,
        Estatus = lab.LAB_Estatus,
        OwnerId = lab.LAB_OwnerId,
        FechaCreacion = lab.FechaCreacion,
        CreadoPor = lab.CreadoPor,
        FechaActualizacion = lab.FechaActualizacion,
        ActualizadoPor = lab.ActualizadoPor,
        RowVersion = Convert.ToBase64String(lab.RowVersion),
    };
}
