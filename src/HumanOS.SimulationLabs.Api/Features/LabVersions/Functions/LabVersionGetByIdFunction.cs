using System.Diagnostics;
using System.Net;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Contracts;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.LabVersions.Functions;

public sealed class LabVersionGetByIdFunction
{
    private const string FunctionName = "LabVersion_GetById";
    private readonly ILabVersionService _versionService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabVersionGetByIdFunction> _logger;

    public LabVersionGetByIdFunction(
        ILabVersionService versionService,
        ICurrentUserContext currentUser,
        ILogger<LabVersionGetByIdFunction> logger)
    {
        _versionService = versionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lab-versions/{idVersion}")] HttpRequestData request,
        string idVersion,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            LogAudit(null, "UNAUTHENTICATED", "FAILED", sw.ElapsedMilliseconds, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, errorCode: "UNAUTHENTICATED", cancellationToken: cancellationToken);
        }

        // Endpoint administrativo que devuelve LAB_BriefOculto; requiere permiso LabVersions.Manage (o Read + Manage)
        if (!_currentUser.HasPermission(LabVersionPolicies.Manage) && !_currentUser.HasPermission(LabVersionPolicies.Read))
        {
            LogAudit(null, "FORBIDDEN", "FAILED", sw.ElapsedMilliseconds, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Forbidden, "Usuario sin permiso para consultar el detalle de la versión (requiere LabVersions.Manage).", correlationId, errorCode: "FORBIDDEN", cancellationToken: cancellationToken);
        }

        if (!Guid.TryParse(idVersion, out var versionId))
        {
            LogAudit(null, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        try
        {
            var version = await _versionService.GetByIdAsync(_currentUser.TenantId, versionId, cancellationToken);
            if (version is null)
            {
                LogAudit(versionId, "LAB_VERSION_NOT_FOUND", "FAILED", sw.ElapsedMilliseconds, null);
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Versión de Lab no encontrada.", correlationId, errorCode: "LAB_VERSION_NOT_FOUND", cancellationToken: cancellationToken);
            }

            LogAudit(versionId, null, "SUCCESS", sw.ElapsedMilliseconds, version.Estatus);
            var response = await ApiResponses.JsonAsync(request, HttpStatusCode.OK, version, correlationId, cancellationToken);
            // ETag values must be quoted per HTTP spec — a bare base64 RowVersion is rejected by
            // HttpHeaders.Add with a FormatException ("The format of value '...' is invalid.").
            response.Headers.Add("ETag", $"\"{version.RowVersion}\"");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving LabVersion {IdVersion}. CorrelationId={CorrelationId}", versionId, correlationId);
            LogAudit(versionId, "INTERNAL_ERROR", "FAILED", sw.ElapsedMilliseconds, null);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, errorCode: "INTERNAL_ERROR", cancellationToken: cancellationToken);
        }
    }

    private void LogAudit(
        Guid? labVersionId,
        string? errorCode,
        string resultado,
        long duracionMs,
        string? estadoActual)
    {
        _logger.LogInformation(
            "StructuredLog: Function={Function}, TenantId={TenantId}, UserId={UserId}, LabId={LabId}, LabVersionId={LabVersionId}, CorrelationId={CorrelationId}, EstadoAnterior={EstadoAnterior}, EstadoNuevo={EstadoNuevo}, Resultado={Resultado}, Duracion={Duracion}ms, ErrorCode={ErrorCode}",
            FunctionName,
            _currentUser.TenantId,
            _currentUser.UserId,
            null,
            labVersionId,
            _currentUser.CorrelationId,
            estadoActual ?? "N/A",
            estadoActual ?? "N/A",
            resultado,
            duracionMs,
            errorCode ?? "NONE");
    }
}
