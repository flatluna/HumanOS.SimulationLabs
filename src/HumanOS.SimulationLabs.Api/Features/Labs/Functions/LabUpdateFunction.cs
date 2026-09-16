using System.Net;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Labs.Contracts;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using HumanOS.SimulationLabs.Api.Features.Labs.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Functions;

public sealed class LabUpdateFunction
{
    private readonly ILabService _labService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabUpdateFunction> _logger;

    public LabUpdateFunction(ILabService labService, ICurrentUserContext currentUser, ILogger<LabUpdateFunction> logger)
    {
        _labService = labService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Lab_Update")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "labs/{idLab}")] HttpRequestData request,
        string idLab,
        CancellationToken cancellationToken)
    {
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, cancellationToken: cancellationToken);
        }

        if (!Guid.TryParse(idLab, out var labId))
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "idLab debe ser un GUID válido.", correlationId, cancellationToken: cancellationToken);
        }

        var ifMatch = request.Headers.TryGetValues("If-Match", out var ifMatchValues) ? ifMatchValues.FirstOrDefault() : null;
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", correlationId, cancellationToken: cancellationToken);
        }

        UpdateLabRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdateLabRequest>(request.Body, ApiResponses.JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud contiene JSON inválido.", correlationId, cancellationToken: cancellationToken);
        }

        if (body is null)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud es requerido.", correlationId, cancellationToken: cancellationToken);
        }

        var errors = LabValidators.ValidateUpdate(body);
        if (errors.Count > 0)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Solicitud inválida.", correlationId, errors: errors, cancellationToken: cancellationToken);
        }

        try
        {
            var updated = await _labService.UpdateAsync(_currentUser.TenantId, labId, ifMatch.Trim('"'), _currentUser.DisplayName, body, cancellationToken);
            var response = await ApiResponses.JsonAsync(request, HttpStatusCode.OK, updated, correlationId, cancellationToken);
            response.Headers.Add("ETag", updated.RowVersion);
            return response;
        }
        catch (LabNotFoundException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Lab no encontrado.", correlationId, cancellationToken: cancellationToken);
        }
        catch (LabPreconditionFailedException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.PreconditionFailed, "El header If-Match no coincide con el estado actual del Lab.", correlationId, cancellationToken: cancellationToken);
        }
        catch (LabNotEditableException ex)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.UnprocessableEntity, ex.Message, correlationId, cancellationToken: cancellationToken);
        }
        catch (LabConcurrencyException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Conflict, "Conflicto de concurrencia al actualizar el Lab.", correlationId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating Lab {IdLab}. CorrelationId={CorrelationId}", labId, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }
}
