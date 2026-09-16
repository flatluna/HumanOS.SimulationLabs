using System.Net;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Functions;

public sealed class LabGetFunction
{
    private readonly ILabService _labService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabGetFunction> _logger;

    public LabGetFunction(ILabService labService, ICurrentUserContext currentUser, ILogger<LabGetFunction> logger)
    {
        _labService = labService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Lab_Get")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labs/{idLab:guid}")] HttpRequestData request,
        Guid idLab,
        CancellationToken cancellationToken)
    {
        var correlationId = _currentUser.CorrelationId;
        if (!_currentUser.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, cancellationToken: cancellationToken);
        }

        try
        {
            var lab = await _labService.GetByIdAsync(_currentUser.TenantId, idLab, cancellationToken);
            return lab is null
                ? await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Lab no encontrado.", correlationId, cancellationToken: cancellationToken)
                : await ApiResponses.JsonAsync(request, HttpStatusCode.OK, lab, correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting Lab {IdLab}. CorrelationId={CorrelationId}", idLab, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }
}