using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>
/// Validates the Microsoft Entra ID bearer token on incoming HTTP triggers and stores the
/// resulting <see cref="ClaimsPrincipal"/> on <see cref="FunctionContext.Items"/> so downstream
/// code (see <see cref="CurrentUserContext"/>) never has to trust caller-supplied identity data.
/// Functions still run at <c>AuthorizationLevel.Anonymous</c>; this middleware is the real gate.
/// </summary>
public sealed class EntraIdAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    public const string PrincipalItemKey = "ClaimsPrincipal";

    private readonly ILogger<EntraIdAuthenticationMiddleware> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration>? _configManager;
    private readonly string? _audience;
    private readonly string? _issuer;

    public EntraIdAuthenticationMiddleware(IConfiguration configuration, ILogger<EntraIdAuthenticationMiddleware> logger)
    {
        _logger = logger;

        var instance = configuration["EntraId:Instance"];
        var tenantId = configuration["EntraId:TenantId"];
        _audience = configuration["EntraId:Audience"];

        if (string.IsNullOrWhiteSpace(instance) || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(_audience))
        {
            // Not configured (e.g. local dev without Entra ID set up yet); requests will be treated as unauthenticated.
            _configManager = null;
            _issuer = null;
            return;
        }

        _issuer = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
        var metadataAddress = $"{instance.TrimEnd('/')}/{tenantId}/v2.0/.well-known/openid-configuration";
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever());
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();

        if (requestData is not null && _configManager is not null)
        {
            var token = ExtractBearerToken(requestData);
            if (token is not null)
            {
                var principal = await TryValidateAsync(token);
                if (principal is not null)
                {
                    context.Items[PrincipalItemKey] = principal;
                }
            }
        }

        await next(context);
    }

    private static string? ExtractBearerToken(Microsoft.Azure.Functions.Worker.Http.HttpRequestData requestData)
    {
        if (!requestData.Headers.TryGetValues("Authorization", out var values))
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

    private async Task<ClaimsPrincipal?> TryValidateAsync(string token)
    {
        try
        {
            var openIdConfig = await _configManager!.GetConfigurationAsync(CancellationToken.None);
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = _issuer,
                ValidateIssuer = true,
                ValidAudience = _audience,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateIssuerSigningKey = true,
            };

            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bearer token validation failed.");
            return null;
        }
    }
}
