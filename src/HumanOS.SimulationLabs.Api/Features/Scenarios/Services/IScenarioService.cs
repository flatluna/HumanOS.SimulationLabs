using HumanOS.SimulationLabs.Api.Features.Scenarios.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.Scenarios.Services;

public interface IScenarioService
{
    Task<ScenarioResponse> CreateAsync(Guid tenantId, Guid versionId, string user, CreateScenarioRequest request, CancellationToken ct);
    Task<ScenarioResponse?> GetByIdAsync(Guid tenantId, Guid scenarioId, CancellationToken ct);
    Task<ScenarioListResponse> ListAsync(Guid tenantId, Guid versionId, string? tipo, string? dificultad, string? estatus, string? search, int page, int pageSize, CancellationToken ct);
    Task<ScenarioResponse> UpdateAsync(Guid tenantId, Guid scenarioId, string etag, string user, UpdateScenarioRequest request, CancellationToken ct);
    Task<ScenarioResponse> ApproveAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct);
    Task<ScenarioResponse> PublishAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct);
    Task<ScenarioResponse> RetireAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct);
    /// <summary>APPROVED or PUBLISHED -> DRAFT, so Studio admins can edit content again (e.g. job
    /// description fields) without losing the Scenario/re-creating the Lab. See ScenarioService.</summary>
    Task<ScenarioResponse> RevertToDraftAsync(Guid tenantId, Guid scenarioId, string etag, string user, CancellationToken ct);
}
