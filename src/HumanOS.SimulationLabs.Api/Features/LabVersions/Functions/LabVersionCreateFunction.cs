using System.Diagnostics;
using System.Net;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Contracts;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Services;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.LabVersions.Functions;

public sealed class LabVersionCreateFunction
{
    private const string FunctionName = "LabVersion_Create";
    private readonly ILabVersionService _versionService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<LabVersionCreateFunction> _logger;

    public LabVersionCreateFunction(
        ILabVersionService versionService,
        ICurrentUserContext currentUser,
        IIdempotencyService idempotencyService,
        ILogger<LabVersionCreateFunction> logger)
    {
        _versionService = versionService;
        _currentUser = currentUser;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labs/{idLab}/versions")] HttpRequestData request,
        string idLab,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            LogAudit(null, null, "UNAUTHENTICATED", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, errorCode: "UNAUTHENTICATED", cancellationToken: cancellationToken);
        }

        if (!_currentUser.HasPermission(LabVersionPolicies.Create))
        {
            LogAudit(null, null, "FORBIDDEN", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Forbidden, "Usuario sin permiso para crear versiones de Lab.", correlationId, errorCode: "FORBIDDEN", cancellationToken: cancellationToken);
        }

        if (!Guid.TryParse(idLab, out var labId))
        {
            LogAudit(null, null, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "idLab debe ser un GUID válido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        CreateLabVersionRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<CreateLabVersionRequest>(request.Body, ApiResponses.JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            LogAudit(labId, null, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud contiene JSON inválido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        if (body is null)
        {
            LogAudit(labId, null, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud es requerido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        var errors = LabVersionValidators.ValidateCreate(body);
        if (errors.Count > 0)
        {
            LogAudit(labId, null, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Solicitud inválida.", correlationId, errors: errors, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        var idempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.FirstOrDefault() : null;

        if (!string.IsNullOrWhiteSpace(idempotencyKey) &&
            _idempotencyService.TryGetResult(_currentUser.TenantId, idempotencyKey, out var cached) &&
            cached is LabVersionDetailResponse cachedVersion)
        {
            LogAudit(labId, cachedVersion.IdVersion, null, "SUCCESS_IDEMPOTENT", sw.ElapsedMilliseconds, null, cachedVersion.Estatus);
            return await BuildCreatedResponse(request, cachedVersion, correlationId, cancellationToken);
        }

        try
        {
            var created = await _versionService.CreateAsync(_currentUser.TenantId, _currentUser.UserId, _currentUser.DisplayName, labId, body, cancellationToken);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                _idempotencyService.StoreResult(_currentUser.TenantId, idempotencyKey, created);
            }

            LogAudit(labId, created.IdVersion, null, "SUCCESS", sw.ElapsedMilliseconds, null, created.Estatus);
            return await BuildCreatedResponse(request, created, correlationId, cancellationToken);
        }
        catch (LabNotFoundException)
        {
            LogAudit(labId, null, "LAB_NOT_FOUND", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Lab no encontrado dentro del tenant.", correlationId, errorCode: "LAB_NOT_FOUND", cancellationToken: cancellationToken);
        }
        catch (LabRetiredException ex)
        {
            LogAudit(labId, null, "LAB_RETIRED", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.UnprocessableEntity, ex.Message, correlationId, errorCode: "LAB_RETIRED", cancellationToken: cancellationToken);
        }
        catch (LabVersionDuplicateException)
        {
            LogAudit(labId, null, "LAB_VERSION_DUPLICATE", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Conflict, "Conflicto al generar el número de versión (número ya existente).", correlationId, errorCode: "LAB_VERSION_DUPLICATE", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating LabVersion for Lab {LabId}. CorrelationId={CorrelationId}", labId, correlationId);
            LogAudit(labId, null, "INTERNAL_ERROR", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, errorCode: "INTERNAL_ERROR", cancellationToken: cancellationToken);
        }
    }

    private static async Task<HttpResponseData> BuildCreatedResponse(
        HttpRequestData request,
        LabVersionDetailResponse version,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var response = await ApiResponses.JsonAsync(request, HttpStatusCode.Created, version, correlationId, cancellationToken);
        response.Headers.Add("Location", $"/api/lab-versions/{version.IdVersion}");
        response.Headers.Add("ETag", version.RowVersion);
        return response;
    }

    private void LogAudit(
        Guid? labId,
        Guid? labVersionId,
        string? errorCode,
        string resultado,
        long duracionMs,
        string? estadoAnterior,
        string? estadoNuevo)
    {
        _logger.LogInformation(
            "StructuredLog: Function={Function}, TenantId={TenantId}, UserId={UserId}, LabId={LabId}, LabVersionId={LabVersionId}, CorrelationId={CorrelationId}, EstadoAnterior={EstadoAnterior}, EstadoNuevo={EstadoNuevo}, Resultado={Resultado}, Duracion={Duracion}ms, ErrorCode={ErrorCode}",
            FunctionName,
            _currentUser.TenantId,
            _currentUser.UserId,
            labId,
            labVersionId,
            _currentUser.CorrelationId,
            estadoAnterior ?? "N/A",
            estadoNuevo ?? "N/A",
            resultado,
            duracionMs,
            errorCode ?? "NONE");
    }
}
