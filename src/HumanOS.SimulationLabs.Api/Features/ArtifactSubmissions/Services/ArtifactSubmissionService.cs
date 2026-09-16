using HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Services;

public sealed class ArtifactSubmissionService : IArtifactSubmissionService
{
    private readonly SimulationLabsDbContext _db;
    public ArtifactSubmissionService(SimulationLabsDbContext db) => _db = db;

    public async Task<ArtifactSubmissionResponse> CreateAsync(Guid tenantId, Guid attemptId, string user, CreateArtifactSubmissionRequest request, CancellationToken ct)
    {
        var attempt = await _db.Attempts.AsNoTracking().FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct) ?? throw new ArtifactSubmissionAttemptNotFoundException();
        if (attempt.ATT_Estatus is not (AttemptEstatus.InProgress or AttemptEstatus.Paused)) throw new ArtifactSubmissionAttemptNotEditableException();

        if (request.StageId is { } stageId && !await _db.Stages.AnyAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId && s.LAB_IdVersion == attempt.LAB_IdVersion, ct)) throw new ArtifactSubmissionStageMismatchException();
        if (request.ObjectiveId is { } objectiveId && !await _db.Objectives.AnyAsync(o => o.SEG_IdTenant == tenantId && o.OBJ_IdObjective == objectiveId && o.LAB_IdVersion == attempt.LAB_IdVersion, ct)) throw new ArtifactSubmissionObjectiveMismatchException();

        var code = request.CodigoArtefacto!.Trim().ToUpperInvariant();
        var previous = await _db.ArtifactSubmissions.Where(s => s.SEG_IdTenant == tenantId && s.ATT_IdAttempt == attemptId && s.SUB_CodigoArtefacto == code).OrderByDescending(s => s.SUB_Version).ToListAsync(ct);
        var nextVersion = (previous.FirstOrDefault()?.SUB_Version ?? 0) + 1;
        var priorFinal = previous.FirstOrDefault(s => s.SUB_Estatus == ArtifactSubmissionEstatus.Final);
        if (priorFinal is not null) throw new ArtifactSubmissionVersionDuplicateException();

        var now = DateTimeOffset.UtcNow;
        var submission = new LAB_ArtifactSubmission
        {
            SUB_IdSubmission = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            LAB_IdVersion = attempt.LAB_IdVersion,
            ATT_IdAttempt = attemptId,
            STG_IdStage = request.StageId,
            OBJ_IdObjective = request.ObjectiveId,
            SUB_CodigoArtefacto = code,
            SUB_Nombre = request.Nombre!.Trim(),
            SUB_Tipo = request.Tipo!.Trim().ToUpperInvariant(),
            SUB_Formato = request.Formato!.Trim().ToUpperInvariant(),
            SUB_Version = nextVersion,
            SUB_ContenidoTexto = Clean(request.ContenidoTexto),
            SUB_ContenidoJson = Clean(request.ContenidoJson),
            SUB_BlobPath = Clean(request.BlobPath),
            SUB_NombreArchivo = Clean(request.NombreArchivo),
            SUB_MimeType = Clean(request.MimeType),
            SUB_Estatus = ArtifactSubmissionEstatus.Draft,
            SUB_FechaInicio = now,
            SUB_EsEntregaFinal = false,
            SUB_RequiereEvaluacion = request.RequiereEvaluacion ?? true,
            SUB_FueGeneradoConAsistencia = request.FueGeneradoConAsistencia ?? false,
            SUB_TipoAsistencia = Clean(request.TipoAsistencia)?.ToUpperInvariant(),
            FechaCreacion = now,
            CreadoPor = user,
        };
        _db.ArtifactSubmissions.Add(submission);
        try { await _db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (IsUnique(ex)) { throw new ArtifactSubmissionVersionDuplicateException(); }
        return ToResponse(submission);
    }

    public async Task<ArtifactSubmissionResponse?> GetByIdAsync(Guid tenantId, Guid submissionId, Guid participantId, bool canReadAll, CancellationToken ct)
    {
        var submission = await _db.ArtifactSubmissions.AsNoTracking().FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SUB_IdSubmission == submissionId, ct);
        if (submission is null) return null;
        if (!canReadAll && !await OwnsAttemptAsync(tenantId, submission.ATT_IdAttempt, participantId, ct)) return null;
        return ToResponse(submission);
    }

    public async Task<ArtifactSubmissionListResponse> ListByAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, Guid? stageId, Guid? objectiveId, string? tipo, string? formato, string? estatus, bool? final, int page, int pageSize, CancellationToken ct)
    {
        if (!await _db.Attempts.AsNoTracking().AnyAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId, ct)) throw new ArtifactSubmissionAttemptNotFoundException();
        if (!canReadAll && !await OwnsAttemptAsync(tenantId, attemptId, participantId, ct)) throw new ArtifactSubmissionAttemptNotFoundException();
        var query = _db.ArtifactSubmissions.AsNoTracking().Where(s => s.SEG_IdTenant == tenantId && s.ATT_IdAttempt == attemptId);
        if (stageId.HasValue) query = query.Where(s => s.STG_IdStage == stageId.Value);
        if (objectiveId.HasValue) query = query.Where(s => s.OBJ_IdObjective == objectiveId.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(s => s.SUB_Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(formato)) query = query.Where(s => s.SUB_Formato == formato);
        if (!string.IsNullOrWhiteSpace(estatus)) query = query.Where(s => s.SUB_Estatus == estatus);
        if (final.HasValue) query = query.Where(s => s.SUB_EsEntregaFinal == final.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.SUB_CodigoArtefacto).ThenByDescending(s => s.SUB_Version).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new ArtifactSubmissionListItemResponse { IdSubmission = s.SUB_IdSubmission, CodigoArtefacto = s.SUB_CodigoArtefacto, Nombre = s.SUB_Nombre, Tipo = s.SUB_Tipo, Formato = s.SUB_Formato, Version = s.SUB_Version, Estatus = s.SUB_Estatus, EsEntregaFinal = s.SUB_EsEntregaFinal, StageId = s.STG_IdStage, ObjectiveId = s.OBJ_IdObjective, RowVersion = Convert.ToBase64String(s.RowVersion) })
            .ToListAsync(ct);
        return new ArtifactSubmissionListResponse { Items = items, Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public async Task<ArtifactSubmissionResponse> UpdateAsync(Guid tenantId, Guid submissionId, string etag, string user, UpdateArtifactSubmissionRequest request, CancellationToken ct)
    {
        var submission = await _db.ArtifactSubmissions.Include(s => s.Attempt).FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SUB_IdSubmission == submissionId, ct) ?? throw new ArtifactSubmissionNotFoundException();
        EnsureEtag(submission.RowVersion, etag);
        if (submission.Attempt?.ATT_Estatus is not (AttemptEstatus.InProgress or AttemptEstatus.Paused)) throw new ArtifactSubmissionAttemptNotEditableException();
        if (submission.SUB_Estatus == ArtifactSubmissionEstatus.Final) throw new ArtifactSubmissionNotEditableException();

        if (request.Nombre is not null) submission.SUB_Nombre = request.Nombre.Trim();
        if (request.ContenidoTexto is not null) submission.SUB_ContenidoTexto = Clean(request.ContenidoTexto);
        if (request.ContenidoJson is not null) submission.SUB_ContenidoJson = Clean(request.ContenidoJson);
        if (request.BlobPath is not null) submission.SUB_BlobPath = Clean(request.BlobPath);
        if (request.NombreArchivo is not null) submission.SUB_NombreArchivo = Clean(request.NombreArchivo);
        if (request.MimeType is not null) submission.SUB_MimeType = Clean(request.MimeType);
        if (request.RequiereEvaluacion.HasValue) submission.SUB_RequiereEvaluacion = request.RequiereEvaluacion.Value;
        if (request.FueGeneradoConAsistencia.HasValue) submission.SUB_FueGeneradoConAsistencia = request.FueGeneradoConAsistencia.Value;
        if (request.TipoAsistencia is not null) submission.SUB_TipoAsistencia = Clean(request.TipoAsistencia)?.ToUpperInvariant();

        Touch(submission, user);
        await SaveAsync(ct);
        return ToResponse(submission);
    }

    public async Task<ArtifactSubmissionResponse> SubmitAsync(Guid tenantId, Guid submissionId, string etag, string user, CancellationToken ct)
    {
        var submission = await _db.ArtifactSubmissions.FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SUB_IdSubmission == submissionId, ct) ?? throw new ArtifactSubmissionNotFoundException();
        EnsureEtag(submission.RowVersion, etag);
        if (submission.SUB_Estatus == ArtifactSubmissionEstatus.Final) throw new ArtifactSubmissionNotEditableException();
        submission.SUB_Estatus = ArtifactSubmissionEstatus.Submitted;
        submission.SUB_FechaEnvio = DateTimeOffset.UtcNow;
        Touch(submission, user);
        await SaveAsync(ct);
        return ToResponse(submission);
    }

    public async Task<ArtifactSubmissionResponse> FinalizeAsync(Guid tenantId, Guid submissionId, string etag, string user, CancellationToken ct)
    {
        var submission = await _db.ArtifactSubmissions.FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SUB_IdSubmission == submissionId, ct) ?? throw new ArtifactSubmissionNotFoundException();
        EnsureEtag(submission.RowVersion, etag);
        if (submission.SUB_Estatus == ArtifactSubmissionEstatus.Final) throw new ArtifactSubmissionNotEditableException();
        submission.SUB_Estatus = ArtifactSubmissionEstatus.Final;
        submission.SUB_EsEntregaFinal = true;
        Touch(submission, user);
        await SaveAsync(ct);
        return ToResponse(submission);
    }

    public async Task<ArtifactSubmissionResponse> ExcludeAsync(Guid tenantId, Guid submissionId, string etag, string user, CancellationToken ct)
    {
        var submission = await _db.ArtifactSubmissions.FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SUB_IdSubmission == submissionId, ct) ?? throw new ArtifactSubmissionNotFoundException();
        EnsureEtag(submission.RowVersion, etag);
        submission.SUB_Estatus = ArtifactSubmissionEstatus.Excluded;
        Touch(submission, user);
        await SaveAsync(ct);
        return ToResponse(submission);
    }

    private async Task<bool> OwnsAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, CancellationToken ct) =>
        await _db.Attempts.AsNoTracking().AnyAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == attemptId && a.USR_IdParticipant == participantId, ct);

    private async Task SaveAsync(CancellationToken ct) { try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ArtifactSubmissionConcurrencyException(); } }
    private static void EnsureEtag(byte[] current, string? expected) { if (string.IsNullOrWhiteSpace(expected)) throw new ArtifactSubmissionPreconditionException("ETAG_REQUIRED"); if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal)) throw new ArtifactSubmissionPreconditionException("ETAG_MISMATCH"); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Touch(LAB_ArtifactSubmission s, string user) { s.FechaActualizacion = DateTimeOffset.UtcNow; s.ActualizadoPor = user; }
    private static bool IsUnique(DbUpdateException ex) => ex.InnerException?.Message.Contains("UQ_LAB_ArtifactSubmission", StringComparison.OrdinalIgnoreCase) == true;

    private static ArtifactSubmissionResponse ToResponse(LAB_ArtifactSubmission s) => new()
    {
        IdSubmission = s.SUB_IdSubmission,
        IdVersion = s.LAB_IdVersion,
        IdAttempt = s.ATT_IdAttempt,
        StageId = s.STG_IdStage,
        ObjectiveId = s.OBJ_IdObjective,
        CodigoArtefacto = s.SUB_CodigoArtefacto,
        Nombre = s.SUB_Nombre,
        Tipo = s.SUB_Tipo,
        Formato = s.SUB_Formato,
        Version = s.SUB_Version,
        ContenidoTexto = s.SUB_ContenidoTexto,
        ContenidoJson = s.SUB_ContenidoJson,
        BlobPath = s.SUB_BlobPath,
        NombreArchivo = s.SUB_NombreArchivo,
        MimeType = s.SUB_MimeType,
        HashSHA256 = s.SUB_HashSHA256,
        Estatus = s.SUB_Estatus,
        FechaInicio = s.SUB_FechaInicio,
        FechaEnvio = s.SUB_FechaEnvio,
        EsEntregaFinal = s.SUB_EsEntregaFinal,
        RequiereEvaluacion = s.SUB_RequiereEvaluacion,
        FueGeneradoConAsistencia = s.SUB_FueGeneradoConAsistencia,
        TipoAsistencia = s.SUB_TipoAsistencia,
        FechaCreacion = s.FechaCreacion,
        CreadoPor = s.CreadoPor,
        FechaActualizacion = s.FechaActualizacion,
        ActualizadoPor = s.ActualizadoPor,
        RowVersion = Convert.ToBase64String(s.RowVersion),
    };
}
