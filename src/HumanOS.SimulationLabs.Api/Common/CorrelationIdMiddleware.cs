using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace HumanOS.SimulationLabs.Api.Common;

/// <summary>
/// Resolves the X-Correlation-ID header (or generates a new one) and stores it on
/// <see cref="FunctionContext.Items"/> so it is available to <see cref="CurrentUserContext"/>
/// and can be echoed back on the response by each Function.
/// </summary>
public sealed class CorrelationIdMiddleware : IFunctionsWorkerMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string CorrelationIdItemKey = "CorrelationId";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        var correlationId = Guid.NewGuid().ToString();

        if (requestData is not null &&
            requestData.Headers.TryGetValues(HeaderName, out var values))
        {
            var provided = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(provided))
            {
                correlationId = provided;
            }
        }

        context.Items[CorrelationIdItemKey] = correlationId;

        await next(context);
    }
}
