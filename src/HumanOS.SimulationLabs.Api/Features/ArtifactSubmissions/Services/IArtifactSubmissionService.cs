using HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Services;

public interface IArtifactSubmissionService
{
    Task<ArtifactSubmissionResponse> CreateAsync(Guid tenantId, Guid attemptId, string user, CreateArtifactSubmissionRequest request, CancellationToken ct);
    Task<ArtifactSubmissionResponse?> GetByIdAsync(Guid tenantId, Guid submissionId, Guid participantId, bool canReadAll, CancellationToken ct);
    Task<ArtifactSubmissionListResponse> ListByAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, Guid? stageId, Guid? objectiveId, string? tipo, string? formato, string? estatus, bool? final, int page, int pageSize, CancellationToken ct);
    Task<ArtifactSubmissionResponse> UpdateAsync(Guid tenantId, Guid submissionId, string etag, string user, UpdateArtifactSubmissionRequest request, CancellationToken ct);
    Task<ArtifactSubmissionResponse> SubmitAsync(Guid tenantId, Guid submissionId, string etag, string user, CancellationToken ct);
    Task<ArtifactSubmissionResponse> FinalizeAsync(Guid tenantId, Guid submissionId, string etag, string user, CancellationToken ct);
    Task<ArtifactSubmissionResponse> ExcludeAsync(Guid tenantId, Guid submissionId, string etag, string user, CancellationToken ct);
}
