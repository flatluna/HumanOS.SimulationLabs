using HumanOS.SimulationLabs.Api.Features.SimulatedActors.Contracts;

namespace HumanOS.SimulationLabs.Api.Features.SimulatedActors.Services;

public interface ISimulatedActorService
{
    Task<SimulatedActorResponse> CreateAsync(Guid tenantId, Guid scenarioId, string user, CreateSimulatedActorRequest request, CancellationToken ct);
    Task<SimulatedActorResponse?> GetByIdAsync(Guid tenantId, Guid actorId, CancellationToken ct);
    Task<SimulatedActorListResponse> ListAsync(Guid tenantId, Guid scenarioId, string? tipo, string? estiloComunicacion, string? nivelConocimiento, bool? principal, string? estatus, int page, int pageSize, CancellationToken ct);
    Task<SimulatedActorResponse> UpdateAsync(Guid tenantId, Guid actorId, string etag, string user, UpdateSimulatedActorRequest request, CancellationToken ct);
    Task<ReorderSimulatedActorsResponse> ReorderAsync(Guid tenantId, Guid scenarioId, string user, ReorderSimulatedActorsRequest request, CancellationToken ct);
    Task<SimulatedActorResponse> ActivateAsync(Guid tenantId, Guid actorId, string etag, string user, CancellationToken ct);
    Task<SimulatedActorResponse> InactivateAsync(Guid tenantId, Guid actorId, string etag, string user, CancellationToken ct);
}
