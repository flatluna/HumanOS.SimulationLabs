using System.Net;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Functions;

public sealed class LabBuilderSaveDraftFunction
{
    private readonly ILabDraftSaveService _saveService;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<LabBuilderSaveDraftFunction> _logger;

    public LabBuilderSaveDraftFunction(
        ILabDraftSaveService saveService,
        ICurrentUserContext user,
        ILogger<LabBuilderSaveDraftFunction> logger)
    {
        _saveService = saveService;
        _user = user;
        _logger = logger;
    }

    [Function("LabBuilder_SaveDraft")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai-lab-builder/save-draft")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", _user.CorrelationId, errorCode: "UNAUTHENTICATED", cancellationToken: cancellationToken);
        }

        if (!_user.HasPermission(AiLabBuilderPolicies.Generate))
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Forbidden, "Usuario sin permiso.", _user.CorrelationId, errorCode: "FORBIDDEN", cancellationToken: cancellationToken);
        }

        SaveLabDraftRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<SaveLabDraftRequest>(request.Body, ApiResponses.JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo no es un JSON válido.", _user.CorrelationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        if (body?.Draft is null)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El borrador (Draft) es requerido.", _user.CorrelationId, errorCode: "DRAFT_REQUIRED", cancellationToken: cancellationToken);
        }

        try
        {
            var saved = await _saveService.SaveDraftAsync(
                _user.TenantId,
                _user.UserId,
                _user.DisplayName,
                body,
                cancellationToken);

            var response = request.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add(CorrelationIdMiddleware.HeaderName, _user.CorrelationId);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(saved, ApiResponses.JsonOptions), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando Lab atómicamente. CorrelationId={CorrelationId}", _user.CorrelationId);
            return await ApiResponses.ProblemAsync(
                request,
                HttpStatusCode.InternalServerError,
                "Error al guardar el Lab en la base de datos.",
                _user.CorrelationId,
                detail: GetDatabaseErrorDetail(ex),
                errorCode: "SAVE_FAILED",
                cancellationToken: cancellationToken);
        }
    }

    private static string GetDatabaseErrorDetail(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null) current = current.InnerException;
        return current.Message;
    }
}
