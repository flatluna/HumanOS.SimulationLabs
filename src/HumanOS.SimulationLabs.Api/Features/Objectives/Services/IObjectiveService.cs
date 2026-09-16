using HumanOS.SimulationLabs.Api.Features.Objectives.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.Objectives.Services;

public interface IObjectiveService
{
    Task<ObjectiveResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateObjectiveRequest request, CancellationToken ct);
    Task<ObjectiveResponse?> GetByIdAsync(Guid tenantId, Guid objectiveId, CancellationToken ct);
    Task<ObjectiveListResponse> ListByVersionAsync(Guid tenantId, Guid versionId, Guid? stageId, bool? onlyGeneral, string? status, string? evidenceType, bool? isCritical, int page, int pageSize, bool descending, CancellationToken ct);
    Task<ObjectiveListResponse> ListByStageAsync(Guid tenantId, Guid stageId, string? status, string? evidenceType, bool? isCritical, int page, int pageSize, bool descending, CancellationToken ct);
    Task<ObjectiveResponse> UpdateAsync(Guid tenantId, Guid objectiveId, string etag, string user, UpdateObjectiveRequest request, CancellationToken ct);
    Task<ReorderObjectivesResponse> ReorderAsync(Guid tenantId, Guid versionId, string user, ReorderObjectivesRequest request, CancellationToken ct);
    Task<ObjectiveResponse> SetStatusAsync(Guid tenantId, Guid objectiveId, string etag, string user, string targetStatus, CancellationToken ct);
}
