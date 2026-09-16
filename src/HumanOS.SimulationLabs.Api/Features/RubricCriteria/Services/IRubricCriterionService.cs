using HumanOS.SimulationLabs.Api.Features.RubricCriteria.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.RubricCriteria.Services;

public interface IRubricCriterionService
{
    Task<RubricCriterionResponse> CreateAsync(Guid tenantId, Guid rubricId, string user, CreateRubricCriterionRequest request, CancellationToken ct);
    Task<RubricCriterionResponse?> GetByIdAsync(Guid tenantId, Guid criterionId, CancellationToken ct);
    Task<RubricCriterionListResponse> ListByRubricAsync(Guid tenantId, Guid rubricId, Guid? objectiveId, Guid? expectedMomentId, string? tipoEvidencia, bool? critico, string? estatus, int page, int pageSize, string? sortDirection, CancellationToken ct);
    Task<RubricCriterionResponse> UpdateAsync(Guid tenantId, Guid criterionId, string etag, string user, UpdateRubricCriterionRequest request, CancellationToken ct);
    Task<ReorderRubricCriteriaResponse> ReorderAsync(Guid tenantId, Guid rubricId, string user, ReorderRubricCriteriaRequest request, CancellationToken ct);
    Task<RubricCriterionResponse> ActivateAsync(Guid tenantId, Guid criterionId, string etag, string user, RubricCriterionActionRequest request, CancellationToken ct);
    Task<RubricCriterionResponse> InactivateAsync(Guid tenantId, Guid criterionId, string etag, string user, RubricCriterionActionRequest request, CancellationToken ct);
}
