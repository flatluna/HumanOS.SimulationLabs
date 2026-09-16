using System.Net;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Labs.Contracts;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using HumanOS.SimulationLabs.Api.Features.Labs.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Functions;

public sealed class LabListFunction
{
    private readonly ILabService _labService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabListFunction> _logger;

    public LabListFunction(ILabService labService, ICurrentUserContext currentUser, ILogger<LabListFunction> logger)
    {
        _labService = labService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Lab_List")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labs")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, cancellationToken: cancellationToken);
        }

        var q = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
        var query = new LabListQuery
        {
            Search = q["search"],
            Tipo = q["tipo"],
            Dominio = q["dominio"],
            Estatus = q["estatus"],
            Page = int.TryParse(q["page"], out var page) ? page : 1,
            PageSize = int.TryParse(q["pageSize"], out var pageSize) ? pageSize : 20,
            SortBy = q["sortBy"],
            SortDirection = q["sortDirection"],
        };

        var errors = LabValidators.ValidateListQuery(query);
        if (errors.Count > 0)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Filtros o paginación inválidos.", correlationId, errors: errors, cancellationToken: cancellationToken);
        }

        try
        {
            var result = await _labService.ListAsync(_currentUser.TenantId, query, cancellationToken);
            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, result, correlationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing Labs. CorrelationId={CorrelationId}", correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }
}
