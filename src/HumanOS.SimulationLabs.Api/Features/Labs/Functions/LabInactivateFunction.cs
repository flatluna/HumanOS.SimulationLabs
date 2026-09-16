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

public sealed class LabInactivateFunction
{
    private readonly ILabService _labService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabInactivateFunction> _logger;

    public LabInactivateFunction(ILabService labService, ICurrentUserContext currentUser, ILogger<LabInactivateFunction> logger)
    {
        _labService = labService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Lab_Inactivate")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labs/{idLab}/inactivate")] HttpRequestData request,
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

        InactivateLabRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<InactivateLabRequest>(request.Body, ApiResponses.JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud contiene JSON inválido.", correlationId, cancellationToken: cancellationToken);
        }

        if (body is null)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud es requerido.", correlationId, cancellationToken: cancellationToken);
        }

        var errors = LabValidators.ValidateInactivate(body);
        if (errors.Count > 0)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Solicitud inválida.", correlationId, errors: errors, cancellationToken: cancellationToken);
        }

        try
        {
            var result = await _labService.InactivateAsync(_currentUser.TenantId, labId, ifMatch.Trim('"'), _currentUser.DisplayName, body, cancellationToken);
            var response = await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, correlationId, cancellationToken);
            response.Headers.Add("ETag", result.RowVersion);
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
        catch (LabConcurrencyException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Conflict, "Conflicto de concurrencia al inactivar el Lab.", correlationId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error inactivating Lab {IdLab}. CorrelationId={CorrelationId}", labId, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }
}
