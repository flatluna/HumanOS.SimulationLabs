using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>
/// Accepts the plain X-Azure-OID/X-Azure-TID/X-Azure-Email headers used by the main
/// HumanOS backend and its own SPAs (human-os-employee, human-os-web) — same
/// trust model already used throughout HumanOS/backend/HumanOS's own Functions
/// (per-function header parsing, no bearer JWT). Runs only as a last-resort
/// fallback: if Entra ID or the Studio launch JWT already resolved a principal,
/// this middleware does nothing. Grants no elevated permission — enough for
/// read-only endpoints that only check IsAuthenticated (e.g. Lab_List), not for
/// policy-gated admin operations.
/// </summary>
public sealed class HeaderIdentityAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.Items.ContainsKey(EntraIdAuthenticationMiddleware.PrincipalItemKey))
        {
            await next(context);
            return;
        }

        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData is not null)
        {
            var oid = FirstHeaderValue(requestData, "X-Azure-OID");
            var tid = FirstHeaderValue(requestData, "X-Azure-TID");
            var email = FirstHeaderValue(requestData, "X-Azure-Email");

            if (!string.IsNullOrWhiteSpace(oid) && Guid.TryParse(tid, out _))
            {
                var claims = new List<Claim>
                {
                    new(CurrentUserContext.TenantClaimType, tid!),
                    new(ClaimTypes.NameIdentifier, oid),
                    new("name", email ?? oid),
                };
                // Minimal "participant" permission set for employees running Labs from the
                // Engram portal — read catalog data + manage only their own attempts/turns,
                // never Studio's authoring/admin policies (Create/Update/Approve/Publish/...).
                foreach (var permission in new[]
                {
                    "Labs.Read", "LabVersions.Read", "Scenarios.Read", "SimulatedActors.Read",
                    "Attempts.Start", "Attempts.ReadOwn", "Attempts.ManageOwn",
                    "ConversationTurns.CreateInternal", "ConversationTurns.ReadOwn",
                })
                {
                    claims.Add(new Claim(ClaimTypes.Role, permission));
                }
                var identity = new ClaimsIdentity(claims, "HeaderIdentity");
                context.Items[EntraIdAuthenticationMiddleware.PrincipalItemKey] = new ClaimsPrincipal(identity);
            }
        }

        await next(context);
    }

    private static string? FirstHeaderValue(Microsoft.Azure.Functions.Worker.Http.HttpRequestData requestData, string name) =>
        requestData.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
}
