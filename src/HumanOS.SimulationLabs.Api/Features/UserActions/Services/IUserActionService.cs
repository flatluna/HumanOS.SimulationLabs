using HumanOS.SimulationLabs.Api.Features.UserActions.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.UserActions.Services;

public interface IUserActionService
{
    Task<UserActionResponse> CreateAsync(Guid tenantId, Guid attemptId, string user, CreateUserActionRequest request, CancellationToken ct);
    Task<UserActionResponse?> GetByIdAsync(Guid tenantId, Guid userActionId, Guid participantId, bool canReadAll, CancellationToken ct);
    Task<UserActionListResponse> ListByAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, string? tipo, string? resultado, string? severidad, bool? critica, Guid? expectedMomentId, int page, int pageSize, CancellationToken ct);
    Task<UserActionResponse> FinalizeAsync(Guid tenantId, Guid userActionId, string etag, string user, CancellationToken ct);
    Task<UserActionResponse> ReverseAsync(Guid tenantId, Guid userActionId, string etag, string user, CancellationToken ct);
    Task<UserActionResponse> ExcludeAsync(Guid tenantId, Guid userActionId, string etag, string user, CancellationToken ct);
}
