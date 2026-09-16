using HumanOS.SimulationLabs.Api.Features.Rubrics.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.Rubrics.Services;

public interface IRubricService
{
    Task<RubricResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateRubricRequest request, CancellationToken ct);
    Task<RubricResponse?> GetByIdAsync(Guid tenantId, Guid rubricId, CancellationToken ct);
    Task<RubricResponse?> GetByVersionAsync(Guid tenantId, Guid versionId, CancellationToken ct);
    Task<RubricResponse> UpdateAsync(Guid tenantId, Guid rubricId, string etag, string user, UpdateRubricRequest request, CancellationToken ct);
    Task<RubricResponse> ApproveAsync(Guid tenantId, Guid rubricId, string etag, string user, RubricActionRequest request, CancellationToken ct);
    Task<RubricResponse> PublishAsync(Guid tenantId, Guid rubricId, string etag, string user, PublishRubricRequest request, CancellationToken ct);
    Task<RubricResponse> RetireAsync(Guid tenantId, Guid rubricId, string etag, string user, RetireRubricRequest request, CancellationToken ct);
}
