using HumanOS.SimulationLabs.Api.Features.Labs.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Services;

public interface ILabService
{
    Task<LabResponse> CreateAsync(Guid tenantId, Guid userId, string createdBy, CreateLabRequest request, CancellationToken cancellationToken);

    Task<LabResponse?> GetByIdAsync(Guid tenantId, Guid idLab, CancellationToken cancellationToken);

    Task<PagedResult<LabListItemResponse>> ListAsync(Guid tenantId, LabListQuery query, CancellationToken cancellationToken);

    Task<LabResponse> UpdateAsync(Guid tenantId, Guid idLab, string ifMatchRowVersion, string updatedBy, UpdateLabRequest request, CancellationToken cancellationToken);

    Task<InactivateLabResponse> InactivateAsync(Guid tenantId, Guid idLab, string ifMatchRowVersion, string updatedBy, InactivateLabRequest request, CancellationToken cancellationToken);

    /// <summary>Permanently deletes a Lab and ALL its related rows (versions, scenarios, actors,
    /// stages, objectives, expected moments, rubric/criteria, and any attempts/turns/actions/
    /// artifacts recorded against it) in a single atomic transaction. Irreversible.</summary>
    Task DeleteAsync(Guid tenantId, Guid idLab, CancellationToken cancellationToken);
}
