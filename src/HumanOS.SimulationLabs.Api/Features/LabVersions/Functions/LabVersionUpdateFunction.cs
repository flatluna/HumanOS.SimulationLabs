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

public sealed class LabVersionUpdateFunction
{
    private const string FunctionName = "LabVersion_Update";
    private readonly ILabVersionService _versionService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabVersionUpdateFunction> _logger;

    public LabVersionUpdateFunction(
        ILabVersionService versionService,
        ICurrentUserContext currentUser,
        ILogger<LabVersionUpdateFunction> logger)
    {
        _versionService = versionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "lab-versions/{idVersion}")] HttpRequestData request,
        string idVersion,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            LogAudit(null, "UNAUTHENTICATED", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, errorCode: "UNAUTHENTICATED", cancellationToken: cancellationToken);
        }

        if (!_currentUser.HasPermission(LabVersionPolicies.Update))
        {
            LogAudit(null, "FORBIDDEN", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Forbidden, "Usuario sin permiso para actualizar la versión de Lab.", correlationId, errorCode: "FORBIDDEN", cancellationToken: cancellationToken);
        }

        if (!Guid.TryParse(idVersion, out var versionId))
        {
            LogAudit(null, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        var ifMatch = request.Headers.TryGetValues("If-Match", out var ifMatchValues) ? ifMatchValues.FirstOrDefault() : null;
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            LogAudit(versionId, "ETAG_REQUIRED", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.PreconditionFailed, "El header If-Match es requerido.", correlationId, errorCode: "ETAG_REQUIRED", cancellationToken: cancellationToken);
        }

        UpdateLabVersionRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<UpdateLabVersionRequest>(request.Body, ApiResponses.JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            LogAudit(versionId, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud contiene JSON inválido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        if (body is null)
        {
            LogAudit(versionId, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo de la solicitud es requerido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        var errors = LabVersionValidators.ValidateUpdate(body);
        if (errors.Count > 0)
        {
            LogAudit(versionId, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Solicitud inválida.", correlationId, errors: errors, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        try
        {
            var updated = await _versionService.UpdateAsync(
                _currentUser.TenantId,
                versionId,
                ifMatch.Trim('"'),
                _currentUser.DisplayName,
                body,
                cancellationToken);

            LogAudit(versionId, null, "SUCCESS", sw.ElapsedMilliseconds, "DRAFT", updated.Estatus);
            var response = await ApiResponses.JsonAsync(request, HttpStatusCode.OK, updated, correlationId, cancellationToken);
            response.Headers.Add("ETag", updated.RowVersion);
            return response;
        }
        catch (LabVersionNotFoundException)
        {
            LogAudit(versionId, "LAB_VERSION_NOT_FOUND", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", correlationId, errorCode: "LAB_VERSION_NOT_FOUND", cancellationToken: cancellationToken);
        }
        catch (LabVersionPreconditionFailedException ex)
        {
            LogAudit(versionId, ex.ReasonCode, "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.PreconditionFailed, ex.Message, correlationId, errorCode: ex.ReasonCode, cancellationToken: cancellationToken);
        }
        catch (LabVersionNotEditableException ex)
        {
            LogAudit(versionId, "LAB_VERSION_NOT_EDITABLE", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.UnprocessableEntity, ex.Message, correlationId, errorCode: "LAB_VERSION_NOT_EDITABLE", cancellationToken: cancellationToken);
        }
        catch (LabVersionConcurrencyException)
        {
            LogAudit(versionId, "CONCURRENCY_CONFLICT", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Conflict, "Conflicto de concurrencia al actualizar la versión.", correlationId, errorCode: "CONCURRENCY_CONFLICT", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating LabVersion {IdVersion}. CorrelationId={CorrelationId}", versionId, correlationId);
            LogAudit(versionId, "INTERNAL_ERROR", "FAILED", sw.ElapsedMilliseconds, null, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, errors: [$"{ex.GetType().Name}: {ex.Message}"], errorCode: "INTERNAL_ERROR", cancellationToken: cancellationToken);
        }
    }

    private void LogAudit(
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
            null,
            labVersionId,
            _currentUser.CorrelationId,
            estadoAnterior ?? "N/A",
            estadoNuevo ?? "N/A",
            resultado,
            duracionMs,
            errorCode ?? "NONE");
    }
}
