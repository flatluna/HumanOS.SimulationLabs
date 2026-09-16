using System.Net;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;
using HumanOS.SimulationLabs.Api.Features.Attempts.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Functions;

public sealed class AttemptEvaluateFunction
{
    private readonly AttemptEvaluationService _service;
    private readonly AttemptEvaluationAgent _agent;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<AttemptEvaluateFunction> _logger;

    public AttemptEvaluateFunction(
        AttemptEvaluationService service, AttemptEvaluationAgent agent, ICurrentUserContext user, ILogger<AttemptEvaluateFunction> logger)
    {
        _service = service;
        _agent = agent;
        _user = user;
        _logger = logger;
    }

    [Function("Attempt_Evaluate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt:guid}/evaluate")] HttpRequestData request,
        Guid idAttempt,
        CancellationToken ct)
    {
        var correlationId = _user.CorrelationId;
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, cancellationToken: ct);
        }

        if (!_agent.IsConfigured)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.ServiceUnavailable, "El agente de evaluación no está configurado.", correlationId, cancellationToken: ct);
        }

        EvaluateAttemptRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<EvaluateAttemptRequest>(request.Body, ApiResponses.JsonOptions, ct) ?? new EvaluateAttemptRequest();
        }
        catch (JsonException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo no es un JSON válido.", correlationId, cancellationToken: ct);
        }

        try
        {
            var result = await _service.EvaluateAsync(_user.TenantId, idAttempt, _user.UserId, body, ct);
            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, correlationId, ct);
        }
        catch (AttemptNotFoundForEvaluationException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Intento no encontrado.", correlationId, errorCode: "ATTEMPT_NOT_FOUND", cancellationToken: ct);
        }
        catch (AttemptNotCompletedException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.UnprocessableEntity, "El intento debe estar completado antes de evaluarlo.", correlationId, errorCode: "ATTEMPT_NOT_COMPLETED", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Attempt_Evaluate failed. AttemptId={AttemptId} CorrelationId={CorrelationId}", idAttempt, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado evaluando el intento.", correlationId, cancellationToken: ct);
        }
    }
}

public sealed class AttemptEvaluationGetByAttemptFunction
{
    private readonly AttemptEvaluationService _service;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<AttemptEvaluationGetByAttemptFunction> _logger;

    public AttemptEvaluationGetByAttemptFunction(AttemptEvaluationService service, ICurrentUserContext user, ILogger<AttemptEvaluationGetByAttemptFunction> logger)
    {
        _service = service;
        _user = user;
        _logger = logger;
    }

    [Function("AttemptEvaluation_GetByAttempt")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "attempts/{idAttempt:guid}/evaluation")] HttpRequestData request,
        Guid idAttempt,
        CancellationToken ct)
    {
        var correlationId = _user.CorrelationId;
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, cancellationToken: ct);
        }

        try
        {
            var result = await _service.GetByAttemptAsync(_user.TenantId, idAttempt, _user.UserId, ct);
            return result is null
                ? await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Evaluación no encontrada.", correlationId, errorCode: "EVALUATION_NOT_FOUND", cancellationToken: ct)
                : await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, correlationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AttemptEvaluation_GetByAttempt failed. AttemptId={AttemptId} CorrelationId={CorrelationId}", idAttempt, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: ct);
        }
    }
}
