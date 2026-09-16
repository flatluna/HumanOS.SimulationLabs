namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Services;

using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;

public interface ILabDraftSaveService
{
    Task<SaveLabDraftResponse> SaveDraftAsync(
        Guid tenantId,
        Guid userId,
        string createdBy,
        SaveLabDraftRequest request,
        CancellationToken cancellationToken);
}
