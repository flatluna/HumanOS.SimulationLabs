using System.Diagnostics;
using System.Net;
using System.Web;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Contracts;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Services;
using HumanOS.SimulationLabs.Api.Features.LabVersions.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.LabVersions.Functions;

public sealed class LabVersionListByLabFunction
{
    private const string FunctionName = "LabVersion_ListByLab";
    private readonly ILabVersionService _versionService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabVersionListByLabFunction> _logger;

    public LabVersionListByLabFunction(
        ILabVersionService versionService,
        ICurrentUserContext currentUser,
        ILogger<LabVersionListByLabFunction> logger)
    {
        _versionService = versionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function(FunctionName)]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labs/{idLab}/versions")] HttpRequestData request,
        string idLab,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            LogAudit(null, "UNAUTHENTICATED", "FAILED", sw.ElapsedMilliseconds);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, errorCode: "UNAUTHENTICATED", cancellationToken: cancellationToken);
        }

        if (!_currentUser.HasPermission(LabVersionPolicies.Read))
        {
            LogAudit(null, "FORBIDDEN", "FAILED", sw.ElapsedMilliseconds);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Forbidden, "Usuario sin permiso para listar versiones de Lab.", correlationId, errorCode: "FORBIDDEN", cancellationToken: cancellationToken);
        }

        if (!Guid.TryParse(idLab, out var labId))
        {
            LogAudit(null, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "idLab debe ser un GUID válido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        var q = HttpUtility.ParseQueryString(request.Url.Query);
        var query = new LabVersionListQuery
        {
            Estatus = q["estatus"],
            Page = int.TryParse(q["page"], out var page) ? page : 1,
            PageSize = int.TryParse(q["pageSize"], out var pageSize) ? pageSize : 20,
            SortDirection = q["sortDirection"] ?? "DESC",
        };

        var errors = LabVersionValidators.ValidateListQuery(query);
        if (errors.Count > 0)
        {
            LogAudit(labId, "REQUEST_INVALID", "FAILED", sw.ElapsedMilliseconds);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Filtros o paginación inválidos.", correlationId, errors: errors, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        try
        {
            var result = await _versionService.ListByLabAsync(_currentUser.TenantId, labId, query, cancellationToken);
            LogAudit(labId, null, "SUCCESS", sw.ElapsedMilliseconds);
            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, correlationId, cancellationToken);
        }
        catch (LabNotFoundException)
        {
            LogAudit(labId, "LAB_NOT_FOUND", "FAILED", sw.ElapsedMilliseconds);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Lab no encontrado dentro del tenant.", correlationId, errorCode: "LAB_NOT_FOUND", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing LabVersions for Lab {LabId}. CorrelationId={CorrelationId}", labId, correlationId);
            LogAudit(labId, "INTERNAL_ERROR", "FAILED", sw.ElapsedMilliseconds);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, errorCode: "INTERNAL_ERROR", cancellationToken: cancellationToken);
        }
    }

    private void LogAudit(
        Guid? labId,
        string? errorCode,
        string resultado,
        long duracionMs)
    {
        _logger.LogInformation(
            "StructuredLog: Function={Function}, TenantId={TenantId}, UserId={UserId}, LabId={LabId}, LabVersionId={LabVersionId}, CorrelationId={CorrelationId}, EstadoAnterior={EstadoAnterior}, EstadoNuevo={EstadoNuevo}, Resultado={Resultado}, Duracion={Duracion}ms, ErrorCode={ErrorCode}",
            FunctionName,
            _currentUser.TenantId,
            _currentUser.UserId,
            labId,
            null,
            _currentUser.CorrelationId,
            "N/A",
            "N/A",
            resultado,
            duracionMs,
            errorCode ?? "NONE");
    }
}
