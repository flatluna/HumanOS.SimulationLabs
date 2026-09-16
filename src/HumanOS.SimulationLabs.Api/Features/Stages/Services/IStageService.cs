using HumanOS.SimulationLabs.Api.Features.Stages.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.Stages.Services;

public interface IStageService
{
    Task<StageResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateStageRequest request, CancellationToken ct);
    Task<StageResponse?> GetByIdAsync(Guid tenantId, Guid stageId, CancellationToken ct);
    Task<StageListResponse> ListAsync(Guid tenantId, Guid versionId, string? status, string? interactionType, bool? required, int page, int pageSize, bool descending, CancellationToken ct);
    Task<StageResponse> UpdateAsync(Guid tenantId, Guid stageId, string etag, string user, UpdateStageRequest request, CancellationToken ct);
    Task<ReorderStagesResponse> ReorderAsync(Guid tenantId, Guid versionId, string user, ReorderStagesRequest request, CancellationToken ct);
    Task<StageResponse> SetStatusAsync(Guid tenantId, Guid stageId, string etag, string user, string targetStatus, CancellationToken ct);
}
