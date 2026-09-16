using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.LabVersions.Services;

public sealed class LabVersionService : ILabVersionService
{
    private readonly SimulationLabsDbContext _db;
    private readonly ILogger<LabVersionService> _logger;

    public LabVersionService(SimulationLabsDbContext db, ILogger<LabVersionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<LabVersionDetailResponse> CreateAsync(
        Guid tenantId,
        Guid userId,
        string createdBy,
        Guid idLab,
        CreateLabVersionRequest request,
        CancellationToken cancellationToken)
    {
        var lab = await _db.Labs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab, cancellationToken);

        if (lab is null)
        {
            throw new LabNotFoundException("Lab no encontrado dentro del tenant.");
        }

        if (string.Equals(lab.LAB_Estatus, LabEstatus.Retired, StringComparison.OrdinalIgnoreCase))
        {
            throw new LabRetiredException("El Lab está retirado y no admite nuevas versiones.");
        }

        var maxVersion = await _db.LabVersions
            .Where(v => v.SEG_IdTenant == tenantId && v.LAB_IdLab == idLab)
            .MaxAsync(v => (int?)v.LAB_NumeroVersion, cancellationToken) ?? 0;

        var nextNumeroVersion = maxVersion + 1;

        var version = new LAB_LabVersion
        {
            LAB_IdVersion = Guid.NewGuid(),
            LAB_IdLab = idLab,
            SEG_IdTenant = tenantId,
            LAB_NumeroVersion = nextNumeroVersion,
            LAB_ObjetivoGeneral = request.ObjetivoGeneral!.Trim(),
            LAB_InstruccionesParticipante = request.InstruccionesParticipante!.Trim(),
            LAB_BriefOculto = string.IsNullOrWhiteSpace(request.BriefOculto) ? null : request.BriefOculto.Trim(),
            LAB_DuracionMinutos = request.DuracionMinutos!.Value,
            LAB_ScoreMinimo = request.ScoreMinimo!.Value,
            LAB_Estatus = LabVersionEstatus.Draft,
            LAB_VigenciaDesde = request.VigenciaDesde,
            LAB_VigenciaHasta = request.VigenciaHasta,
            LAB_HashConfiguracion = null,
            FechaCreacion = DateTimeOffset.UtcNow,
            CreadoPor = createdBy,
        };

        _db.LabVersions.Add(version);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new LabVersionDuplicateException("Conflicto al generar el número de versión (número ya existente).");
        }

        return ToDetailResponse(version);
    }

    public async Task<LabVersionDetailResponse?> GetByIdAsync(
        Guid tenantId,
        Guid idVersion,
        CancellationToken cancellationToken)
    {
        var version = await _db.LabVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == idVersion, cancellationToken);

        return version is null ? null : ToDetailResponse(version);
    }

    public async Task<PagedResult<LabVersionListItemResponse>> ListByLabAsync(
        Guid tenantId,
        Guid idLab,
        LabVersionListQuery query,
        CancellationToken cancellationToken)
    {
        var labExists = await _db.Labs.AsNoTracking()
            .AnyAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab, cancellationToken);

        if (!labExists)
        {
            throw new LabNotFoundException("Lab no encontrado dentro del tenant.");
        }

        var versions = _db.LabVersions.AsNoTracking()
            .Where(v => v.SEG_IdTenant == tenantId && v.LAB_IdLab == idLab);

        if (!string.IsNullOrWhiteSpace(query.Estatus))
        {
            versions = versions.Where(v => v.LAB_Estatus == query.Estatus);
        }

        var totalItems = await versions.CountAsync(cancellationToken);

        var descending = !string.Equals(query.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase);
        versions = descending
            ? versions.OrderByDescending(v => v.LAB_NumeroVersion)
            : versions.OrderBy(v => v.LAB_NumeroVersion);

        var items = await versions
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(v => new LabVersionListItemResponse
            {
                IdVersion = v.LAB_IdVersion,
                NumeroVersion = v.LAB_NumeroVersion,
                ObjetivoGeneral = v.LAB_ObjetivoGeneral,
                DuracionMinutos = v.LAB_DuracionMinutos,
                ScoreMinimo = v.LAB_ScoreMinimo,
                Estatus = v.LAB_Estatus,
                VigenciaDesde = v.LAB_VigenciaDesde,
                VigenciaHasta = v.LAB_VigenciaHasta,
                FechaCreacion = v.FechaCreacion,
                FechaActualizacion = v.FechaActualizacion,
                RowVersion = Convert.ToBase64String(v.RowVersion),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LabVersionListItemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize),
        };
    }

    public async Task<LabVersionDetailResponse> UpdateAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        UpdateLabVersionRequest request,
        CancellationToken cancellationToken)
    {
        var version = await _db.LabVersions
            .FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == idVersion, cancellationToken);

        if (version is null)
        {
            throw new LabVersionNotFoundException("Versión no encontrada dentro del tenant.");
        }

        ValidateIfMatch(version, ifMatchRowVersion);

        if (!string.Equals(version.LAB_Estatus, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new LabVersionNotEditableException(version.LAB_Estatus);
        }

        if (request.ObjetivoGeneral is not null)
        {
            version.LAB_ObjetivoGeneral = request.ObjetivoGeneral.Trim();
        }

        if (request.InstruccionesParticipante is not null)
        {
            version.LAB_InstruccionesParticipante = request.InstruccionesParticipante.Trim();
        }

        if (request.BriefOculto is not null)
        {
            version.LAB_BriefOculto = string.IsNullOrWhiteSpace(request.BriefOculto) ? null : request.BriefOculto.Trim();
        }

        if (request.DuracionMinutos.HasValue)
        {
            version.LAB_DuracionMinutos = request.DuracionMinutos.Value;
        }

        if (request.ScoreMinimo.HasValue)
        {
            version.LAB_ScoreMinimo = request.ScoreMinimo.Value;
        }

        if (request.VigenciaDesde.HasValue)
        {
            version.LAB_VigenciaDesde = request.VigenciaDesde.Value;
        }

        if (request.VigenciaHasta.HasValue)
        {
            version.LAB_VigenciaHasta = request.VigenciaHasta.Value;
        }

        version.FechaActualizacion = DateTimeOffset.UtcNow;
        version.ActualizadoPor = updatedBy;

        var cleanIfMatch = ifMatchRowVersion.Trim().Trim('"');
        _db.Entry(version).Property(v => v.RowVersion).OriginalValue = Convert.FromBase64String(cleanIfMatch);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LabVersionConcurrencyException();
        }

        return ToDetailResponse(version);
    }

    public async Task<LabVersionDetailResponse> ApproveAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        ApproveLabVersionRequest request,
        CancellationToken cancellationToken)
    {
        var version = await _db.LabVersions
            .FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == idVersion, cancellationToken);

        if (version is null)
        {
            throw new LabVersionNotFoundException("Versión no encontrada dentro del tenant.");
        }

        ValidateIfMatch(version, ifMatchRowVersion);

        if (!string.Equals(version.LAB_Estatus, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidStatusTransitionException(version.LAB_Estatus, LabVersionEstatus.Approved);
        }

        if (string.IsNullOrWhiteSpace(version.LAB_ObjetivoGeneral) ||
            string.IsNullOrWhiteSpace(version.LAB_InstruccionesParticipante))
        {
            throw new InvalidOperationException("La versión debe contener Objetivo General e Instrucciones completas.");
        }

        if (version.LAB_DuracionMinutos <= 0)
        {
            throw new InvalidOperationException("La duración en minutos debe ser mayor que cero.");
        }

        if (version.LAB_ScoreMinimo < 1.00m || version.LAB_ScoreMinimo > 10.00m)
        {
            throw new InvalidOperationException("El score mínimo debe estar entre 1.00 y 10.00.");
        }

        var oldStatus = version.LAB_Estatus;
        version.LAB_Estatus = LabVersionEstatus.Approved;
        version.FechaActualizacion = DateTimeOffset.UtcNow;
        version.ActualizadoPor = updatedBy;

        var cleanIfMatch = ifMatchRowVersion.Trim().Trim('"');
        _db.Entry(version).Property(v => v.RowVersion).OriginalValue = Convert.FromBase64String(cleanIfMatch);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LabVersionConcurrencyException();
        }

        _logger.LogInformation(
            "Audit: LabVersion approved. TenantId={TenantId}, LabId={LabId}, LabVersionId={LabVersionId}, EstadoAnterior={EstadoAnterior}, EstadoNuevo={EstadoNuevo}, UpdatedBy={UpdatedBy}, Motivo={Motivo}",
            tenantId, version.LAB_IdLab, version.LAB_IdVersion, oldStatus, LabVersionEstatus.Approved, updatedBy, request.Motivo);

        return ToDetailResponse(version);
    }

    public async Task<PublishLabVersionResponse> PublishAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        PublishLabVersionRequest request,
        CancellationToken cancellationToken)
    {
        var version = await _db.LabVersions
            .FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == idVersion, cancellationToken);

        if (version is null)
        {
            throw new LabVersionNotFoundException("Versión no encontrada dentro del tenant.");
        }

        ValidateIfMatch(version, ifMatchRowVersion);

        // Si ya está PUBLISHED, devolver estado actual de forma idempotente
        if (string.Equals(version.LAB_Estatus, LabVersionEstatus.Published, StringComparison.OrdinalIgnoreCase))
        {
            return new PublishLabVersionResponse
            {
                IdVersion = version.LAB_IdVersion,
                NumeroVersion = version.LAB_NumeroVersion,
                Estatus = version.LAB_Estatus,
                VigenciaDesde = version.LAB_VigenciaDesde,
                VigenciaHasta = version.LAB_VigenciaHasta,
                HashConfiguracion = version.LAB_HashConfiguracion,
                RowVersion = Convert.ToBase64String(version.RowVersion),
            };
        }

        if (!string.Equals(version.LAB_Estatus, LabVersionEstatus.Approved, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidStatusTransitionException(version.LAB_Estatus, LabVersionEstatus.Published);
        }

        string hash;
        try
        {
            hash = ComputeConfigurationHash(version);
        }
        catch (Exception ex)
        {
            throw new ConfigurationHashException("Error al calcular el hash determinístico de configuración.", ex);
        }

        var oldStatus = version.LAB_Estatus;
        version.LAB_HashConfiguracion = hash;
        version.LAB_Estatus = LabVersionEstatus.Published;
        version.LAB_VigenciaDesde = request.VigenciaDesde;
        version.LAB_VigenciaHasta = request.VigenciaHasta;
        version.FechaActualizacion = DateTimeOffset.UtcNow;
        version.ActualizadoPor = updatedBy;

        var cleanIfMatch = ifMatchRowVersion.Trim().Trim('"');
        _db.Entry(version).Property(v => v.RowVersion).OriginalValue = Convert.FromBase64String(cleanIfMatch);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LabVersionConcurrencyException();
        }

        _logger.LogInformation(
            "Audit: LabVersion published. TenantId={TenantId}, LabId={LabId}, LabVersionId={LabVersionId}, EstadoAnterior={EstadoAnterior}, EstadoNuevo={EstadoNuevo}, UpdatedBy={UpdatedBy}, HashConfiguracion={Hash}, Motivo={Motivo}",
            tenantId, version.LAB_IdLab, version.LAB_IdVersion, oldStatus, LabVersionEstatus.Published, updatedBy, hash, request.Motivo);

        return new PublishLabVersionResponse
        {
            IdVersion = version.LAB_IdVersion,
            NumeroVersion = version.LAB_NumeroVersion,
            Estatus = version.LAB_Estatus,
            VigenciaDesde = version.LAB_VigenciaDesde,
            VigenciaHasta = version.LAB_VigenciaHasta,
            HashConfiguracion = version.LAB_HashConfiguracion,
            RowVersion = Convert.ToBase64String(version.RowVersion),
        };
    }

    public async Task<RetireLabVersionResponse> RetireAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        RetireLabVersionRequest request,
        CancellationToken cancellationToken)
    {
        var version = await _db.LabVersions
            .FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == idVersion, cancellationToken);

        if (version is null)
        {
            throw new LabVersionNotFoundException("Versión no encontrada dentro del tenant.");
        }

        ValidateIfMatch(version, ifMatchRowVersion);

        // Si ya está RETIRED, devolver respuesta idempotente
        if (string.Equals(version.LAB_Estatus, LabVersionEstatus.Retired, StringComparison.OrdinalIgnoreCase))
        {
            return new RetireLabVersionResponse
            {
                IdVersion = version.LAB_IdVersion,
                NumeroVersion = version.LAB_NumeroVersion,
                Estatus = version.LAB_Estatus,
                VigenciaHasta = version.LAB_VigenciaHasta,
                RowVersion = Convert.ToBase64String(version.RowVersion),
            };
        }

        var oldStatus = version.LAB_Estatus;
        if (!string.Equals(oldStatus, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(oldStatus, LabVersionEstatus.Approved, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(oldStatus, LabVersionEstatus.Published, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidStatusTransitionException(oldStatus, LabVersionEstatus.Retired);
        }

        version.LAB_Estatus = LabVersionEstatus.Retired;
        if (request.VigenciaHasta.HasValue)
        {
            version.LAB_VigenciaHasta = request.VigenciaHasta.Value;
        }
        else if (version.LAB_VigenciaHasta is null)
        {
            version.LAB_VigenciaHasta = DateTime.UtcNow;
        }

        version.FechaActualizacion = DateTimeOffset.UtcNow;
        version.ActualizadoPor = updatedBy;

        var cleanIfMatch = ifMatchRowVersion.Trim().Trim('"');
        _db.Entry(version).Property(v => v.RowVersion).OriginalValue = Convert.FromBase64String(cleanIfMatch);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new LabVersionConcurrencyException();
        }

        _logger.LogInformation(
            "Audit: LabVersion retired. TenantId={TenantId}, LabId={LabId}, LabVersionId={LabVersionId}, EstadoAnterior={EstadoAnterior}, EstadoNuevo={EstadoNuevo}, UpdatedBy={UpdatedBy}, Motivo={Motivo}",
            tenantId, version.LAB_IdLab, version.LAB_IdVersion, oldStatus, LabVersionEstatus.Retired, updatedBy, request.Motivo);

        return new RetireLabVersionResponse
        {
            IdVersion = version.LAB_IdVersion,
            NumeroVersion = version.LAB_NumeroVersion,
            Estatus = version.LAB_Estatus,
            VigenciaHasta = version.LAB_VigenciaHasta,
            RowVersion = Convert.ToBase64String(version.RowVersion),
        };
    }

    /// <summary>
    /// Genera un hash SHA-256 reproducible de la configuración actual de la versión con serialización determinística.
    /// Fase actual: incluye únicamente los campos de LAB_LabVersion.
    /// Fase posterior: se extenderá para incluir etapas, objetivos, momentos esperados, rúbrica, criterios, escenarios y actores.
    /// </summary>
    public static string ComputeConfigurationHash(LAB_LabVersion version)
    {
        var canonical = string.Create(CultureInfo.InvariantCulture,
            $"briefOculto:{version.LAB_BriefOculto?.Trim() ?? string.Empty}\n" +
            $"duracionMinutos:{version.LAB_DuracionMinutos}\n" +
            $"instruccionesParticipante:{version.LAB_InstruccionesParticipante.Trim()}\n" +
            $"numeroVersion:{version.LAB_NumeroVersion}\n" +
            $"objetivoGeneral:{version.LAB_ObjetivoGeneral.Trim()}\n" +
            $"scoreMinimo:{version.LAB_ScoreMinimo:F2}");

        var bytes = Encoding.UTF8.GetBytes(canonical);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static void ValidateIfMatch(LAB_LabVersion version, string ifMatchRowVersion)
    {
        if (string.IsNullOrWhiteSpace(ifMatchRowVersion))
        {
            throw new LabVersionPreconditionFailedException("ETAG_REQUIRED", "El header If-Match es requerido.");
        }

        var cleanIfMatch = ifMatchRowVersion.Trim().Trim('"');
        var currentRowVersion = Convert.ToBase64String(version.RowVersion);

        if (!string.Equals(currentRowVersion, cleanIfMatch, StringComparison.Ordinal))
        {
            throw new LabVersionPreconditionFailedException("ETAG_MISMATCH", "El header If-Match no coincide con el estado actual de la versión.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627;
        }

        return ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("UQ_LAB_LabVersion", StringComparison.OrdinalIgnoreCase);
    }

    private static LabVersionDetailResponse ToDetailResponse(LAB_LabVersion v) => new()
    {
        IdVersion = v.LAB_IdVersion,
        IdLab = v.LAB_IdLab,
        NumeroVersion = v.LAB_NumeroVersion,
        ObjetivoGeneral = v.LAB_ObjetivoGeneral,
        InstruccionesParticipante = v.LAB_InstruccionesParticipante,
        BriefOculto = v.LAB_BriefOculto,
        DuracionMinutos = v.LAB_DuracionMinutos,
        ScoreMinimo = v.LAB_ScoreMinimo,
        Estatus = v.LAB_Estatus,
        VigenciaDesde = v.LAB_VigenciaDesde,
        VigenciaHasta = v.LAB_VigenciaHasta,
        HashConfiguracion = v.LAB_HashConfiguracion,
        FechaCreacion = v.FechaCreacion,
        CreadoPor = v.CreadoPor,
        FechaActualizacion = v.FechaActualizacion,
        ActualizadoPor = v.ActualizadoPor,
        RowVersion = Convert.ToBase64String(v.RowVersion),
    };
}
