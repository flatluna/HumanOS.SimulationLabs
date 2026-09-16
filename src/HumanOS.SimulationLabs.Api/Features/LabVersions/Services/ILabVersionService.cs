using HumanOS.SimulationLabs.Api.Features.LabVersions.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.LabVersions.Services;

public interface ILabVersionService
{
    Task<LabVersionDetailResponse> CreateAsync(
        Guid tenantId,
        Guid userId,
        string createdBy,
        Guid idLab,
        CreateLabVersionRequest request,
        CancellationToken cancellationToken);

    Task<LabVersionDetailResponse?> GetByIdAsync(
        Guid tenantId,
        Guid idVersion,
        CancellationToken cancellationToken);

    Task<PagedResult<LabVersionListItemResponse>> ListByLabAsync(
        Guid tenantId,
        Guid idLab,
        LabVersionListQuery query,
        CancellationToken cancellationToken);

    Task<LabVersionDetailResponse> UpdateAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        UpdateLabVersionRequest request,
        CancellationToken cancellationToken);

    Task<LabVersionDetailResponse> ApproveAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        ApproveLabVersionRequest request,
        CancellationToken cancellationToken);

    Task<PublishLabVersionResponse> PublishAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        PublishLabVersionRequest request,
        CancellationToken cancellationToken);

    Task<RetireLabVersionResponse> RetireAsync(
        Guid tenantId,
        Guid idVersion,
        string ifMatchRowVersion,
        string updatedBy,
        RetireLabVersionRequest request,
        CancellationToken cancellationToken);
}
