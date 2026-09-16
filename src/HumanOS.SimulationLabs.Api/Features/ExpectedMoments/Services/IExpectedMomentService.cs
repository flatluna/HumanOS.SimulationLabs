using HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Services;

public interface IExpectedMomentService
{
    Task<ExpectedMomentResponse> CreateAsync(Guid tenantId, Guid stageId, string user, CreateExpectedMomentRequest request, CancellationToken ct);
    Task<ExpectedMomentResponse?> GetByIdAsync(Guid tenantId, Guid momentId, CancellationToken ct);
    Task<ExpectedMomentListResponse> ListByVersionAsync(Guid tenantId, Guid versionId, Guid? stageId, Guid? objectiveId, string? tipo, bool? critico, bool? requiereRespuesta, string? estatus, int page, int pageSize, CancellationToken ct);
    Task<ExpectedMomentListResponse> ListByStageAsync(Guid tenantId, Guid stageId, Guid? objectiveId, string? tipo, bool? critico, string? estatus, int page, int pageSize, CancellationToken ct);
    Task<ExpectedMomentResponse> UpdateAsync(Guid tenantId, Guid momentId, string etag, string user, UpdateExpectedMomentRequest request, CancellationToken ct);
    Task<ReorderExpectedMomentsResponse> ReorderAsync(Guid tenantId, Guid stageId, string user, ReorderExpectedMomentsRequest request, CancellationToken ct);
    Task<ExpectedMomentResponse> SetStatusAsync(Guid tenantId, Guid momentId, string etag, string user, string targetStatus, CancellationToken ct);
}
