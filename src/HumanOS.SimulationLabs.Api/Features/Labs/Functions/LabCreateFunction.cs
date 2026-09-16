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

public sealed class LabCreateFunction
{
    private readonly ILabService _labService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<LabCreateFunction> _logger;

    public LabCreateFunction(ILabService labService, ICurrentUserContext currentUser, IIdempotencyService idempotencyService, ILogger<LabCreateFunction> logger)
    {
        _labService = labService;
        _currentUser = currentUser;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    [Function("Lab_Create")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labs")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, cancellationToken: cancellationToken);
        }

        CreateLabRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateLabRequest>(request.Body, ApiResponses.JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud contiene JSON inválido.", correlationId, cancellationToken: cancellationToken);
        }

        if (body is null)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud es requerido.", correlationId, cancellationToken: cancellationToken);
        }

        var errors = LabValidators.ValidateCreate(body);
        if (errors.Count > 0)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Solicitud inválida.", correlationId, errors: errors, cancellationToken: cancellationToken);
        }

        var idempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.FirstOrDefault() : null;

        if (!string.IsNullOrWhiteSpace(idempotencyKey) &&
            _idempotencyService.TryGetResult(_currentUser.TenantId, idempotencyKey, out var cached) &&
            cached is LabResponse cachedLab)
        {
            return await BuildCreatedResponse(request, cachedLab, correlationId, cancellationToken);
        }

        try
        {
            var created = await _labService.CreateAsync(_currentUser.TenantId, _currentUser.UserId, _currentUser.DisplayName, body, cancellationToken);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                _idempotencyService.StoreResult(_currentUser.TenantId, idempotencyKey, created);
            }

            return await BuildCreatedResponse(request, created, correlationId, cancellationToken);
        }
        catch (LabDuplicateCodeException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Conflict, "Ya existe un Lab con ese código en el tenant.", correlationId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating Lab. CorrelationId={CorrelationId}", correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }

    private static async Task<HttpResponseData> BuildCreatedResponse(HttpRequestData request, LabResponse lab, string correlationId, CancellationToken cancellationToken)
    {
        var response = await ApiResponses.JsonAsync(request, HttpStatusCode.Created, lab, correlationId, cancellationToken);
        response.Headers.Add("Location", $"/api/labs/{lab.IdLab}");
        response.Headers.Add("ETag", lab.RowVersion);
        return response;
    }
}
