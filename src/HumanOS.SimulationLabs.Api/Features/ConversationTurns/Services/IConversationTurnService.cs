using HumanOS.SimulationLabs.Api.Features.ConversationTurns.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.ConversationTurns.Services;

public interface IConversationTurnService
{
    Task<ConversationTurnResponse> CreateAsync(Guid tenantId, Guid attemptId, string user, CreateConversationTurnRequest request, CancellationToken ct);
    Task<ConversationTurnResponse?> GetByIdAsync(Guid tenantId, Guid turnId, Guid participantId, bool canReadAll, CancellationToken ct);
    Task<ConversationTurnListResponse> ListByAttemptAsync(Guid tenantId, Guid attemptId, Guid participantId, bool canReadAll, int page, int pageSize, CancellationToken ct);
    Task<ConversationTurnResponse> FinalizeAsync(Guid tenantId, Guid turnId, string etag, string user, CancellationToken ct);
    Task<ConversationTurnResponse> CorrectAsync(Guid tenantId, Guid turnId, string etag, string user, CorrectConversationTurnRequest request, CancellationToken ct);
    Task<ConversationTurnResponse> ExcludeAsync(Guid tenantId, Guid turnId, string etag, string user, CancellationToken ct);
}
