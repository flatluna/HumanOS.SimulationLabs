using HumanOS.SimulationLabs.Api.Features.Stages.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Stages.Services;

public sealed class StageService : IStageService
{
    private readonly SimulationLabsDbContext _db;
    public StageService(SimulationLabsDbContext db) => _db = db;

    public async Task<StageResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateStageRequest request, CancellationToken ct)
    {
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct) ?? throw new StageVersionNotFoundException();
        EnsureDraft(version.LAB_Estatus);
        var code = request.Codigo!.Trim().ToUpperInvariant();
        if (await _db.Stages.AnyAsync(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == versionId && s.STG_Codigo == code, ct)) throw new StageCodeDuplicateException();
        var order = (await _db.Stages.Where(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == versionId).MaxAsync(s => (int?)s.STG_Orden, ct) ?? 0) + 1;
        var stage = new LAB_Stage { STG_IdStage = Guid.NewGuid(), SEG_IdTenant = tenantId, LAB_IdVersion = versionId, STG_Codigo = code, STG_Nombre = request.Nombre!.Trim(), STG_Descripcion = request.Descripcion!.Trim(), STG_Orden = order, STG_TipoInteraccion = request.TipoInteraccion!.Trim().ToUpperInvariant(), STG_EsObligatorio = request.EsObligatorio!.Value, STG_CondicionCompletitud = Clean(request.CondicionCompletitud), STG_TiempoSugeridoMinutos = request.TiempoSugeridoMinutos, STG_PermiteOrdenFlexible = request.PermiteOrdenFlexible!.Value, STG_Estatus = StageEstatus.Draft, FechaCreacion = DateTimeOffset.UtcNow, CreadoPor = user };
        _db.Stages.Add(stage);
        try { await _db.SaveChangesAsync(ct); } catch (DbUpdateException ex) when (IsUnique(ex)) { throw new StageCodeDuplicateException(); }
        return ToResponse(stage);
    }

    public async Task<StageResponse?> GetByIdAsync(Guid tenantId, Guid stageId, CancellationToken ct) => (await _db.Stages.AsNoTracking().FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId, ct)) is { } stage ? ToResponse(stage) : null;

    public async Task<StageListResponse> ListAsync(Guid tenantId, Guid versionId, string? status, string? interactionType, bool? required, int page, int pageSize, bool descending, CancellationToken ct)
    {
        if (!await _db.LabVersions.AsNoTracking().AnyAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct)) throw new StageVersionNotFoundException();
        var query = _db.Stages.AsNoTracking().Where(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == versionId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.STG_Estatus == status);
        if (!string.IsNullOrWhiteSpace(interactionType)) query = query.Where(s => s.STG_TipoInteraccion == interactionType);
        if (required.HasValue) query = query.Where(s => s.STG_EsObligatorio == required.Value);
        var total = await query.CountAsync(ct);
        query = descending ? query.OrderByDescending(s => s.STG_Orden) : query.OrderBy(s => s.STG_Orden);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(s => new StageListItemResponse { IdStage = s.STG_IdStage, Codigo = s.STG_Codigo, Nombre = s.STG_Nombre, Descripcion = s.STG_Descripcion, Orden = s.STG_Orden, TipoInteraccion = s.STG_TipoInteraccion, EsObligatorio = s.STG_EsObligatorio, TiempoSugeridoMinutos = s.STG_TiempoSugeridoMinutos, PermiteOrdenFlexible = s.STG_PermiteOrdenFlexible, Estatus = s.STG_Estatus, RowVersion = Convert.ToBase64String(s.RowVersion) }).ToListAsync(ct);
        return new StageListResponse { Items = items, Page = page, PageSize = pageSize, TotalItems = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) };
    }

    public async Task<StageResponse> UpdateAsync(Guid tenantId, Guid stageId, string etag, string user, UpdateStageRequest request, CancellationToken ct)
    {
        var stage = await _db.Stages.Include(s => s.LabVersion).FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId, ct) ?? throw new StageNotFoundException();
        EnsureDraft(stage.LabVersion?.LAB_Estatus); EnsureEtag(stage.RowVersion, etag);
        if (request.Nombre is not null) stage.STG_Nombre = request.Nombre.Trim();
        if (request.Descripcion is not null) stage.STG_Descripcion = request.Descripcion.Trim();
        if (request.TipoInteraccion is not null) stage.STG_TipoInteraccion = request.TipoInteraccion.Trim().ToUpperInvariant();
        if (request.EsObligatorio.HasValue) stage.STG_EsObligatorio = request.EsObligatorio.Value;
        if (request.CondicionCompletitud is not null) stage.STG_CondicionCompletitud = Clean(request.CondicionCompletitud);
        if (request.TiempoSugeridoMinutos is not null) stage.STG_TiempoSugeridoMinutos = request.TiempoSugeridoMinutos;
        if (request.PermiteOrdenFlexible.HasValue) stage.STG_PermiteOrdenFlexible = request.PermiteOrdenFlexible.Value;
        Touch(stage, user);
        await SaveAsync(ct); return ToResponse(stage);
    }

    public async Task<ReorderStagesResponse> ReorderAsync(Guid tenantId, Guid versionId, string user, ReorderStagesRequest request, CancellationToken ct)
    {
        var version = await _db.LabVersions.AsNoTracking().FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == versionId, ct) ?? throw new StageVersionNotFoundException();
        EnsureDraft(version.LAB_Estatus);
        var input = request.Stages!;
        if (input.Select(x => x.IdStage).Distinct().Count() != input.Count || input.Select(x => x.Orden).Distinct().Count() != input.Count || input.Any(x => x.Orden <= 0)) throw new StageOrderDuplicateException();
        var ids = input.Select(x => x.IdStage).ToArray();
        var stages = await _db.Stages.Where(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == versionId && ids.Contains(s.STG_IdStage)).ToListAsync(ct);
        if (stages.Count != input.Count) throw new StageNotFoundException();
        foreach (var item in input) EnsureEtag(stages.Single(s => s.STG_IdStage == item.IdStage).RowVersion, item.RowVersion);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var offset = Math.Max(stages.Max(s => s.STG_Orden), input.Max(x => x.Orden)) + stages.Count + 1;
            foreach (var stage in stages) { stage.STG_Orden += offset; Touch(stage, user); }
            await _db.SaveChangesAsync(ct);
            foreach (var item in input) { var stage = stages.Single(s => s.STG_IdStage == item.IdStage); stage.STG_Orden = item.Orden; }
            await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { await tx.RollbackAsync(ct); throw new StageConcurrencyException(); }
        catch (DbUpdateException ex) when (IsUnique(ex)) { await tx.RollbackAsync(ct); throw new StageOrderDuplicateException(); }
        return new ReorderStagesResponse { Items = stages.OrderBy(s => s.STG_Orden).Select(s => new ReorderStageResponseItem { IdStage = s.STG_IdStage, Orden = s.STG_Orden, RowVersion = Convert.ToBase64String(s.RowVersion) }).ToList() };
    }

    public async Task<StageResponse> SetStatusAsync(Guid tenantId, Guid stageId, string etag, string user, string targetStatus, CancellationToken ct)
    {
        var stage = await _db.Stages.Include(s => s.LabVersion).FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.STG_IdStage == stageId, ct) ?? throw new StageNotFoundException();
        EnsureDraft(stage.LabVersion?.LAB_Estatus); EnsureEtag(stage.RowVersion, etag);
        if (stage.STG_Estatus != targetStatus && !((stage.STG_Estatus == StageEstatus.Draft && (targetStatus == StageEstatus.Active || targetStatus == StageEstatus.Inactive)) || (stage.STG_Estatus == StageEstatus.Active && targetStatus == StageEstatus.Inactive) || (stage.STG_Estatus == StageEstatus.Inactive && targetStatus == StageEstatus.Active))) throw new StagePreconditionException("INVALID_STATUS_TRANSITION");
        if (stage.STG_Estatus != targetStatus) { stage.STG_Estatus = targetStatus; Touch(stage, user); await SaveAsync(ct); }
        return ToResponse(stage);
    }

    private async Task SaveAsync(CancellationToken ct) { try { await _db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new StageConcurrencyException(); } }
    private static void EnsureDraft(string? status) { if (!string.Equals(status, LabVersionEstatus.Draft, StringComparison.OrdinalIgnoreCase)) throw new StageVersionNotEditableException(); }
    private static void EnsureEtag(byte[] current, string? expected) { if (string.IsNullOrWhiteSpace(expected)) throw new StagePreconditionException("ETAG_REQUIRED"); if (!string.Equals(Convert.ToBase64String(current), expected.Trim('"'), StringComparison.Ordinal)) throw new StagePreconditionException("ETAG_MISMATCH"); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Touch(LAB_Stage s, string user) { s.FechaActualizacion = DateTimeOffset.UtcNow; s.ActualizadoPor = user; }
    private static bool IsUnique(DbUpdateException ex) => ex.InnerException?.Message.Contains("UQ_LAB_Stage", StringComparison.OrdinalIgnoreCase) == true;
    private static StageResponse ToResponse(LAB_Stage s) => new() { IdStage = s.STG_IdStage, IdVersion = s.LAB_IdVersion, Codigo = s.STG_Codigo, Nombre = s.STG_Nombre, Descripcion = s.STG_Descripcion, Orden = s.STG_Orden, TipoInteraccion = s.STG_TipoInteraccion, EsObligatorio = s.STG_EsObligatorio, CondicionCompletitud = s.STG_CondicionCompletitud, TiempoSugeridoMinutos = s.STG_TiempoSugeridoMinutos, PermiteOrdenFlexible = s.STG_PermiteOrdenFlexible, Estatus = s.STG_Estatus, FechaCreacion = s.FechaCreacion, CreadoPor = s.CreadoPor, FechaActualizacion = s.FechaActualizacion, ActualizadoPor = s.ActualizadoPor, RowVersion = Convert.ToBase64String(s.RowVersion) };
}
