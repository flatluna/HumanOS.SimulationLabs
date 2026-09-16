using System.Net;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Functions;

/// <summary>Optional body for Attempt_VoiceSession — the employee's own real job role/résumé,
/// fetched by the frontend from the main HumanOS backend, so the live agent knows who it's
/// really talking to. All fields optional; omit the body entirely for the same behavior as
/// before this existed.</summary>
public sealed class AttemptVoiceSessionRequest
{
    public string? EmployeeName { get; set; }
    public string? CompanyName { get; set; }
    public string? JobRoleTitle { get; set; }
    public string? JobRoleSummary { get; set; }
    public List<string> RequiredTechnicalSkills { get; set; } = [];
    public List<string> RequiredSoftSkills { get; set; } = [];
    public string? ResumeSummary { get; set; }
}

public sealed class AttemptVoiceSessionResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string RealtimeCallsUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Voice { get; set; } = string.Empty;
    public long? ExpiresAtUnixSeconds { get; set; }
    public string? ActorNombre { get; set; }
    public string? ActorRol { get; set; }
    public Guid? ActorId { get; set; }
}

/// <summary>
/// Mints an ephemeral Azure OpenAI Realtime session for a REAL, participant-owned Attempt (not
/// the Studio "Probar simulación" preview — see <see cref="Labs.Functions.LabSimulationVoiceSessionFunction"/>).
/// The caller (employee) is expected to persist every turn itself via ConversationTurn_Create as
/// the realtime data channel emits transcript events — this endpoint only negotiates the voice
/// session, it never writes conversation turns.
/// </summary>
public sealed class AttemptVoiceSessionFunction
{
    private readonly SimulationLabsDbContext _db;
    private readonly LabRealtimeVoiceSessionService _voiceSessionService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<AttemptVoiceSessionFunction> _logger;

    public AttemptVoiceSessionFunction(
        SimulationLabsDbContext db,
        LabRealtimeVoiceSessionService voiceSessionService,
        ICurrentUserContext currentUser,
        ILogger<AttemptVoiceSessionFunction> logger)
    {
        _db = db;
        _voiceSessionService = voiceSessionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Attempt_VoiceSession")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt:guid}/voice-session")] HttpRequestData request,
        Guid idAttempt,
        CancellationToken cancellationToken)
    {
        var correlationId = _currentUser.CorrelationId;

        if (!_currentUser.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", correlationId, cancellationToken: cancellationToken);
        }

        if (!_voiceSessionService.IsConfigured)
        {
            return await ApiResponses.ProblemAsync(
                request, HttpStatusCode.ServiceUnavailable,
                "La simulación por voz no está configurada (faltan AzureOpenAIRealtimeDeploymentName/AzureOpenAIRealtimeApiKey).",
                correlationId, cancellationToken: cancellationToken);
        }

        var attempt = await _db.Attempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.SEG_IdTenant == _currentUser.TenantId && a.ATT_IdAttempt == idAttempt, cancellationToken);
        if (attempt is null)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Intento no encontrado.", correlationId, errorCode: "ATTEMPT_NOT_FOUND", cancellationToken: cancellationToken);
        }
        if (attempt.USR_IdParticipant != _currentUser.UserId)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Forbidden, "Este intento pertenece a otro participante.", correlationId, errorCode: "FORBIDDEN", cancellationToken: cancellationToken);
        }
        if (attempt.ATT_Estatus is not (AttemptEstatus.InProgress or AttemptEstatus.Paused))
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.UnprocessableEntity, "El intento no admite una sesión de voz en su estatus actual.", correlationId, errorCode: "ATTEMPT_NOT_EDITABLE", cancellationToken: cancellationToken);
        }

        try
        {
            AttemptVoiceSessionRequest? profileRequest = null;
            try
            {
                profileRequest = await JsonSerializer.DeserializeAsync<AttemptVoiceSessionRequest>(request.Body, cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                // Body omitted or empty — voice session still works, just without live profile grounding.
            }

            var context = await LabSimulationPromptBuilder.LoadForAttemptAsync(_db, _currentUser.TenantId, attempt.LAB_IdVersion, attempt.SCN_IdScenario, cancellationToken);
            if (context is null || context.Scenario is null || context.Actor is null)
            {
                return await ApiResponses.ProblemAsync(
                    request, HttpStatusCode.UnprocessableEntity,
                    "El Lab no tiene un escenario y al menos un actor simulado configurados.",
                    correlationId, cancellationToken: cancellationToken);
            }

            var employeeProfile = profileRequest is null ? null : new LabSimulationPromptBuilder.EmployeeProfileContext
            {
                EmployeeName = profileRequest.EmployeeName,
                CompanyName = profileRequest.CompanyName,
                JobRoleTitle = profileRequest.JobRoleTitle,
                JobRoleSummary = profileRequest.JobRoleSummary,
                RequiredTechnicalSkills = profileRequest.RequiredTechnicalSkills,
                RequiredSoftSkills = profileRequest.RequiredSoftSkills,
                ResumeSummary = profileRequest.ResumeSummary,
            };

            var instructions = LabSimulationPromptBuilder.BuildInstructions(context, _currentUser.DisplayName, isRealAttempt: true, employeeProfile: employeeProfile);
            var session = await _voiceSessionService.CreateEphemeralSessionAsync(instructions, context.Actor.ACT_VoiceName, cancellationToken);

            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, new AttemptVoiceSessionResponse
            {
                ClientSecret = session.ClientSecret,
                RealtimeCallsUrl = session.RealtimeCallsUrl,
                Model = session.Model,
                Voice = session.Voice,
                ExpiresAtUnixSeconds = session.ExpiresAtUnixSeconds,
                ActorNombre = context.Actor.ACT_Nombre,
                ActorRol = context.Actor.ACT_Rol,
                ActorId = context.Actor.ACT_IdActor
            }, correlationId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadGateway, ex.Message, correlationId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error minting Attempt voice session. AttemptId={AttemptId} CorrelationId={CorrelationId}", idAttempt, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }
}
