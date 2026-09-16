namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>
/// Minimal idempotency protection for POST endpoints that accept an Idempotency-Key header.
/// NOTE: this in-memory implementation only protects a single Functions instance/process and
/// does not persist across restarts or scale-out instances. LAB_Lab has no idempotency-key
/// column today, so as a floor, LabService also relies on the unique (SEG_IdTenant, LAB_Codigo)
/// constraint to reject duplicate creates. A durable implementation (e.g. a dedicated table or
/// Durable Entities) should replace this once the schema can be extended.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>Returns the previously stored result for (tenant, key) if this key was already processed.</summary>
    bool TryGetResult(Guid tenantId, string idempotencyKey, out object? result);

    void StoreResult(Guid tenantId, string idempotencyKey, object? result);
}

public sealed class InMemoryIdempotencyService : IIdempotencyService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, object?> _store = new();

    public bool TryGetResult(Guid tenantId, string idempotencyKey, out object? result)
        => _store.TryGetValue(Key(tenantId, idempotencyKey), out result);

    public void StoreResult(Guid tenantId, string idempotencyKey, object? result)
        => _store[Key(tenantId, idempotencyKey)] = result;

    private static string Key(Guid tenantId, string idempotencyKey) => $"{tenantId:N}:{idempotencyKey}";
}
