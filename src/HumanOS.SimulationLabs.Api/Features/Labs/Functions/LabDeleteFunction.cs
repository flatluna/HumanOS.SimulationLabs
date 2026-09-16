using System.Net;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Functions;

/// <summary>Permanently deletes a Lab (and all related rows). Irreversible — for cleaning up
/// test/throwaway Labs, not a substitute for Lab_Inactivate on real ones.</summary>
public sealed class LabDeleteFunction
{
    private readonly ILabService _labService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabDeleteFunction> _logger;

    public LabDeleteFunction(ILabService labService, ICurrentUserContext currentUser, ILogger<LabDeleteFunction> logger)
    {
        _labService = labService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Lab_Delete")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "labs/{idLab}")] HttpRequestData request,
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

        try
        {
            await _labService.DeleteAsync(_currentUser.TenantId, labId, cancellationToken);
        }
        catch (LabNotFoundException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Lab no encontrado.", correlationId, cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Lab_Delete ok. TenantId={TenantId} UserId={UserId} LabId={LabId} CorrelationId={CorrelationId}",
            _currentUser.TenantId, _currentUser.UserId, labId, correlationId);

        var response = request.CreateResponse(HttpStatusCode.NoContent);
        return response;
    }
}
