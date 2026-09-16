using System.Net;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Labs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Enrollments;

public sealed class EnrollmentEnrollFunction
{
    private readonly EnrollmentService _service;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<EnrollmentEnrollFunction> _logger;

    public EnrollmentEnrollFunction(EnrollmentService service, ICurrentUserContext user, ILogger<EnrollmentEnrollFunction> logger)
    {
        _service = service;
        _user = user;
        _logger = logger;
    }

    [Function("Enrollment_Enroll")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labs/{idLab:guid}/enroll")] HttpRequestData request,
        Guid idLab,
        CancellationToken ct)
    {
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", _user.CorrelationId, cancellationToken: ct);
        }

        try
        {
            var result = await _service.EnrollAsync(_user.TenantId, idLab, _user.UserId, _user.DisplayName, ct);
            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, _user.CorrelationId, ct);
        }
        catch (LabNotFoundException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Lab no encontrado.", _user.CorrelationId, errorCode: "LAB_NOT_FOUND", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enrollment_Enroll failed. TenantId={TenantId} LabId={LabId} CorrelationId={CorrelationId}", _user.TenantId, idLab, _user.CorrelationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", _user.CorrelationId, cancellationToken: ct);
        }
    }
}

public sealed class EnrollmentUnenrollFunction
{
    private readonly EnrollmentService _service;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<EnrollmentUnenrollFunction> _logger;

    public EnrollmentUnenrollFunction(EnrollmentService service, ICurrentUserContext user, ILogger<EnrollmentUnenrollFunction> logger)
    {
        _service = service;
        _user = user;
        _logger = logger;
    }

    [Function("Enrollment_Unenroll")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "labs/{idLab:guid}/enroll")] HttpRequestData request,
        Guid idLab,
        CancellationToken ct)
    {
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", _user.CorrelationId, cancellationToken: ct);
        }

        try
        {
            var result = await _service.UnenrollAsync(_user.TenantId, idLab, _user.UserId, _user.DisplayName, ct);
            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, _user.CorrelationId, ct);
        }
        catch (EnrollmentNotFoundException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Inscripción no encontrada.", _user.CorrelationId, errorCode: "ENROLLMENT_NOT_FOUND", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enrollment_Unenroll failed. TenantId={TenantId} LabId={LabId} CorrelationId={CorrelationId}", _user.TenantId, idLab, _user.CorrelationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", _user.CorrelationId, cancellationToken: ct);
        }
    }
}

public sealed class EnrollmentListMineFunction
{
    private readonly EnrollmentService _service;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<EnrollmentListMineFunction> _logger;

    public EnrollmentListMineFunction(EnrollmentService service, ICurrentUserContext user, ILogger<EnrollmentListMineFunction> logger)
    {
        _service = service;
        _user = user;
        _logger = logger;
    }

    [Function("Enrollment_ListMine")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/lab-enrollments")] HttpRequestData request,
        CancellationToken ct)
    {
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", _user.CorrelationId, cancellationToken: ct);
        }

        try
        {
            var result = await _service.ListMineAsync(_user.TenantId, _user.UserId, ct);
            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, _user.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enrollment_ListMine failed. TenantId={TenantId} CorrelationId={CorrelationId}", _user.TenantId, _user.CorrelationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", _user.CorrelationId, cancellationToken: ct);
        }
    }
}
