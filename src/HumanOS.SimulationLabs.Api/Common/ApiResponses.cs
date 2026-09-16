using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace HumanOS.SimulationLabs.Api.Common;

public sealed class ProblemDetailsPayload
{
    public string Type { get; set; } = "about:blank";

    public string Title { get; set; } = string.Empty;

    public int Status { get; set; }

    public string? Detail { get; set; }

    public string? ErrorCode { get; set; }

    public string? CorrelationId { get; set; }

    public IReadOnlyList<string>? Errors { get; set; }
}

/// <summary>Builds ProblemDetails-shaped HTTP responses and stamps the correlation id header consistently.</summary>
public static class ApiResponses
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<HttpResponseData> JsonAsync<T>(
        HttpRequestData request, HttpStatusCode statusCode, T body, string correlationId, CancellationToken cancellationToken = default)
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(body, JsonOptions), cancellationToken);
        return response;
    }

    public static async Task<HttpResponseData> ProblemAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string title,
        string correlationId,
        string? detail = null,
        IReadOnlyList<string>? errors = null,
        string? errorCode = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new ProblemDetailsPayload
        {
            Type = !string.IsNullOrWhiteSpace(errorCode) ? $"urn:error:{errorCode}" : "about:blank",
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            ErrorCode = errorCode,
            CorrelationId = correlationId,
            Errors = errors,
        };

        var response = request.CreateResponse(statusCode);
        response.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);
        response.Headers.Add("Content-Type", "application/problem+json");
        await response.WriteStringAsync(JsonSerializer.Serialize(payload, JsonOptions), cancellationToken);
        return response;
    }
}
