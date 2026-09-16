using HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Services;

public interface IAttemptService
{
    Task<AttemptResponse> StartAsync(Guid tenantId, Guid scenarioId, Guid participantId, string user, StartAttemptRequest request, CancellationToken ct);
    Task<AttemptResponse?> GetByIdAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, CancellationToken ct);
    Task<AttemptListResponse> ListMineAsync(Guid tenantId, Guid participantId, Guid? scenarioId, string? status, string? result, int page, int pageSize, CancellationToken ct);
    Task<AttemptResponse> PauseAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct);
    Task<AttemptResponse> ResumeAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct);
    Task<AttemptResponse> CompleteAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct);
    Task<AttemptResponse> AbandonAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct);
    Task<AttemptResponse> CancelAsync(Guid tenantId, Guid attemptId, Guid participantId, string etag, string user, CancellationToken ct);
    Task DeleteAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canManageAll, CancellationToken ct);
}
