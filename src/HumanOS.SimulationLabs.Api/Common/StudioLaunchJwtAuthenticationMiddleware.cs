using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>
/// Accepts the Human OS Studio "launch session" JWT (same contract as
/// HumanOsStudioSsoToken in the main HumanOS backend: HMAC-SHA256, issuer
/// "humanos", audience "capability-studio") as an alternative identity for
/// callers coming from Studio, which does not use Entra ID bearer tokens.
/// Runs only as a fallback: if <see cref="EntraIdAuthenticationMiddleware"/>
/// already resolved a principal for this request, this middleware does nothing.
/// studioCreate/studioEdit granted in the token map to the existing
/// "SimulationLabs.Admin" bypass in <see cref="CurrentUserContext.HasPermission"/>.
/// </summary>
public sealed class StudioLaunchJwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private const string Issuer = "humanos";
    private const string Audience = "capability-studio";

    private readonly string? _secret;

    public StudioLaunchJwtAuthenticationMiddleware(IConfiguration configuration)
    {
        _secret = configuration["HumanOSStudioLaunchJwtSecret"];
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.Items.ContainsKey(EntraIdAuthenticationMiddleware.PrincipalItemKey))
        {
            await next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_secret))
        {
            await next(context);
            return;
        }

        var requestData = await context.GetHttpRequestDataAsync();
        var token = ExtractBearerToken(requestData);
        if (token is not null)
        {
            var principal = TryValidate(token);
            if (principal is not null)
            {
                context.Items[EntraIdAuthenticationMiddleware.PrincipalItemKey] = principal;
            }
        }

        await next(context);
    }

    private static string? ExtractBearerToken(Microsoft.Azure.Functions.Worker.Http.HttpRequestData? requestData)
    {
        if (requestData is null || !requestData.Headers.TryGetValues("Authorization", out var values))
        {
            return null;
        }

        var header = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header["Bearer ".Length..].Trim();
    }

    private ClaimsPrincipal? TryValidate(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            // Without this, JwtSecurityTokenHandler remaps short claim types (e.g. "oid") to
            // long Microsoft/XML-SOAP claim URIs, so FindFirst("oid") below would silently
            // return null and authentication would fail even for a validly signed token.
            handler.MapInboundClaims = false;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret!));

            var studioPrincipal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            }, out _);

            var oid = studioPrincipal.FindFirst("oid")?.Value;
            var tenantId = studioPrincipal.FindFirst("tenantId")?.Value;
            var email = studioPrincipal.FindFirst("email")?.Value;
            if (string.IsNullOrWhiteSpace(oid) || !Guid.TryParse(tenantId, out _))
            {
                return null;
            }

            var canCreate = string.Equals(studioPrincipal.FindFirst("studioCreate")?.Value, "True", StringComparison.OrdinalIgnoreCase);
            var canEdit = string.Equals(studioPrincipal.FindFirst("studioEdit")?.Value, "True", StringComparison.OrdinalIgnoreCase);

            var claims = new List<Claim>
            {
                new(CurrentUserContext.TenantClaimType, tenantId!),
                new(ClaimTypes.NameIdentifier, oid),
                new("name", email ?? oid),
            };

            // Studio's launch session has no per-feature permission model — grant full
            // Simulation Labs access (same as the existing "SimulationLabs.Admin" bypass)
            // when the user is allowed to create/edit content in Studio.
            if (canCreate || canEdit)
            {
                claims.Add(new Claim(ClaimTypes.Role, "SimulationLabs.Admin"));
            }

            var identity = new ClaimsIdentity(claims, "StudioLaunch");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}
