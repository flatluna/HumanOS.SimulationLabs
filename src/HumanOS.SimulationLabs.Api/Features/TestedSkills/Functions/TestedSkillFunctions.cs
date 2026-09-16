using System.Net;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Objectives;
using HumanOS.SimulationLabs.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.TestedSkills.Functions;

/// <summary>Read-only projection of LAB_TestedSkill for Studio's Lab detail/edit view — shows the
/// AI Lab Builder's own RelevanceToRole/DemonstrationStandard/FeedbackGuidance per skill, which
/// previously only existed as an unreadable JSON blob in LAB_LabVersion/LAB_Scenario.BriefOculto.</summary>
public sealed class TestedSkillListItem
{
    public Guid IdSkill { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string RelevanceToRole { get; set; } = string.Empty;
    public string DemonstrationStandard { get; set; } = string.Empty;
    public string? FeedbackGuidance { get; set; }
    public Guid? CriterionId { get; set; }
}

/// <summary>GET /lab-versions/{idVersion}/tested-skills — reuses Objectives.Read since this is
/// the same "view a Lab's content" permission Studio's Lab detail page already requires.</summary>
public sealed class TestedSkillListByVersionFunction
{
    private readonly SimulationLabsDbContext _db;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<TestedSkillListByVersionFunction> _logger;

    public TestedSkillListByVersionFunction(SimulationLabsDbContext db, ICurrentUserContext user, ILogger<TestedSkillListByVersionFunction> logger)
    {
        _db = db;
        _user = user;
        _logger = logger;
    }

    [Function("TestedSkill_ListByVersion")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lab-versions/{idVersion}/tested-skills")] HttpRequestData r,
        string idVersion,
        CancellationToken ct)
    {
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(r, HttpStatusCode.Unauthorized, "No autenticado.", _user.CorrelationId, errorCode: "UNAUTHENTICATED", cancellationToken: ct);
        }
        if (!_user.HasPermission(ObjectivePolicies.Read))
        {
            return await ApiResponses.ProblemAsync(r, HttpStatusCode.Forbidden, "Usuario sin permiso.", _user.CorrelationId, errorCode: "FORBIDDEN", cancellationToken: ct);
        }
        if (!Guid.TryParse(idVersion, out var versionId))
        {
            return await ApiResponses.ProblemAsync(r, HttpStatusCode.BadRequest, "idVersion debe ser un GUID válido.", _user.CorrelationId, errorCode: "REQUEST_INVALID", cancellationToken: ct);
        }

        try
        {
            var items = await _db.TestedSkills.AsNoTracking()
                .Where(s => s.SEG_IdTenant == _user.TenantId && s.LAB_IdVersion == versionId)
                .OrderBy(s => s.FechaCreacion)
                .Select(s => new TestedSkillListItem
                {
                    IdSkill = s.SKL_IdSkill,
                    Name = s.SKL_Nombre,
                    Type = s.SKL_Tipo,
                    RelevanceToRole = s.SKL_RelevanceToRole,
                    DemonstrationStandard = s.SKL_DemonstrationStandard,
                    FeedbackGuidance = s.SKL_FeedbackGuidance,
                    CriterionId = s.RUB_IdCriterion,
                })
                .ToListAsync(ct);

            return await ApiResponses.JsonAsync(r, HttpStatusCode.OK, new { Items = items }, _user.CorrelationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TestedSkill_ListByVersion failed. CorrelationId={CorrelationId}", _user.CorrelationId);
            return await ApiResponses.ProblemAsync(r, HttpStatusCode.InternalServerError, "Error inesperado.", _user.CorrelationId, errorCode: "INTERNAL_ERROR", cancellationToken: ct);
        }
    }
}
