namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>Identity of the authenticated caller for the current Function invocation.</summary>
public interface ICurrentUserContext
{
    Guid TenantId { get; }

    Guid UserId { get; }

    string DisplayName { get; }

    string CorrelationId { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool HasPermission(string permission);
}
