using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;

namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>
/// Reads the current tenant/user from the <see cref="ClaimsPrincipal"/> populated by
/// <see cref="EntraIdAuthenticationMiddleware"/>, and the correlation id from the request headers.
/// Registered per invocation (scoped to <see cref="FunctionContext"/>).
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    public const string TenantClaimType = "SEG_IdTenant";

    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string CorrelationId { get; private set; } = string.Empty;

    public bool IsAuthenticated { get; private set; }

    public IReadOnlyCollection<string> Permissions => _permissions;

    public bool HasPermission(string permission)
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        if (_permissions.Contains("*") ||
            _permissions.Contains("Admin") ||
            _permissions.Contains("SimulationLabs.Admin") ||
            _permissions.Contains("LabVersions.Manage"))
        {
            return true;
        }

        return _permissions.Contains(permission);
    }

    public void Initialize(FunctionContext context)
    {
        CorrelationId = ResolveCorrelationId(context);

        if (!context.Items.TryGetValue(EntraIdAuthenticationMiddleware.PrincipalItemKey, out var principalObj) ||
            principalObj is not ClaimsPrincipal principal ||
            principal.Identity?.IsAuthenticated != true)
        {
            IsAuthenticated = false;
            return;
        }

        var tenantClaim = principal.FindFirst(TenantClaimType)?.Value;
        var userClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("oid")?.Value
            ?? principal.FindFirst("sub")?.Value;

        if (!Guid.TryParse(tenantClaim, out var tenantId) || !Guid.TryParse(userClaim, out var userId))
        {
            IsAuthenticated = false;
            return;
        }

        TenantId = tenantId;
        UserId = userId;
        DisplayName = principal.FindFirst("name")?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("preferred_username")?.Value
            ?? userId.ToString();
        IsAuthenticated = true;

        _permissions.Clear();
        foreach (var claim in principal.FindAll(ClaimTypes.Role))
        {
            _permissions.Add(claim.Value);
        }
        foreach (var claim in principal.FindAll("roles"))
        {
            _permissions.Add(claim.Value);
        }
        foreach (var claim in principal.FindAll("permissions"))
        {
            _permissions.Add(claim.Value);
        }
        foreach (var claim in principal.FindAll("scp"))
        {
            foreach (var scope in claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                _permissions.Add(scope);
            }
        }
    }

    private static string ResolveCorrelationId(FunctionContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var correlationObj) &&
            correlationObj is string correlationId && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid().ToString();
    }
}
