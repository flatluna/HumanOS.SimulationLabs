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
    public string? ProfessionalObjective { get; set; }
    public string? AcademicProfileSummary { get; set; }
    public string? ThesisSummary { get; set; }
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
    public List<AttemptVoiceSessionActorResponse>? Committee { get; set; }
}

public sealed class AttemptVoiceSessionActorResponse
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
                ProfessionalObjective = profileRequest.ProfessionalObjective,
                AcademicProfileSummary = profileRequest.AcademicProfileSummary,
                ThesisSummary = profileRequest.ThesisSummary,
            };

            // Multi-sinodal committee disabled per explicit product decision (2026-09-17) - the
            // round-robin handoff never sounded reliable in practice. Every Academic Defense
            // attempt now always falls through to the single-actor path below, using just the
            // committee's principal/president actor.
            if (false
                && string.Equals(context.Arquetipo, LabArquetipos.AcademicDefense, StringComparison.OrdinalIgnoreCase)
                && context.CommitteeActors.Count >= 3)
            {
                var committee = new List<AttemptVoiceSessionActorResponse>(3);
                var committeeIndex = 0;
                foreach (var committeeActor in context.CommitteeActors.Take(3))
                {
                    var committeeInstructions = LabSimulationPromptBuilder.BuildInstructions(
                        context,
                        _currentUser.DisplayName,
                        isRealAttempt: true,
                        employeeProfile: employeeProfile,
                        actorOverride: committeeActor,
                        isFirstCommitteeSpeaker: committeeIndex == 0,
                        isIntroductionOnlyTurn: true);
                    committeeIndex++;
                    var committeeSession = await _voiceSessionService.CreateEphemeralSessionAsync(
                        committeeInstructions,
                        committeeActor.ACT_VoiceName,
                        cancellationToken);

                    committee.Add(new AttemptVoiceSessionActorResponse
                    {
                        ClientSecret = committeeSession.ClientSecret,
                        RealtimeCallsUrl = committeeSession.RealtimeCallsUrl,
                        Model = committeeSession.Model,
                        Voice = committeeSession.Voice,
                        ExpiresAtUnixSeconds = committeeSession.ExpiresAtUnixSeconds,
                        ActorNombre = committeeActor.ACT_Nombre,
                        ActorRol = committeeActor.ACT_Rol,
                        ActorId = committeeActor.ACT_IdActor
                    });
                }

                return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, new AttemptVoiceSessionResponse
                {
                    ClientSecret = committee[0].ClientSecret,
                    RealtimeCallsUrl = committee[0].RealtimeCallsUrl,
                    Model = committee[0].Model,
                    ExpiresAtUnixSeconds = committee[0].ExpiresAtUnixSeconds,
                    Committee = committee,
                    ActorNombre = committee[0].ActorNombre,
                    ActorRol = committee[0].ActorRol,
                    ActorId = committee[0].ActorId,
                    Voice = committee[0].Voice
                }, correlationId, cancellationToken);
            }

            var instructions = LabSimulationPromptBuilder.BuildInstructions(context, _currentUser.DisplayName, isRealAttempt: true, employeeProfile: employeeProfile);
            // Single-sinodal mode (2026-09-17): always use the default voice (same as every other
            // course/Lab) instead of this actor's own ACT_VoiceName, per explicit request.
            var session = await _voiceSessionService.CreateEphemeralSessionAsync(instructions, voiceOverride: null, cancellationToken);

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

/// <summary>Body for Attempt_CommitteeNextSpeaker — same optional profile fields as
/// <see cref="AttemptVoiceSessionRequest"/>, plus which committee actor is about to speak next.</summary>
public sealed class AttemptCommitteeNextSpeakerRequest
{
    public Guid ActorId { get; set; }
    /// <summary>True when this actor is the committee president/lead (maps 1:1 to
    /// BuildInstructions' isFirstCommitteeSpeaker) — used both for the president's opening
    /// introduction and later for their real first thesis question.</summary>
    public bool IsFirstCommitteeSpeaker { get; set; }
    /// <summary>True only when THIS specific turn is a bare self-introduction (no question yet) —
    /// maps 1:1 to BuildInstructions' isIntroductionOnlyTurn.</summary>
    public bool IsIntroductionOnlyTurn { get; set; }
    public string? EmployeeName { get; set; }
    public string? CompanyName { get; set; }
    public string? JobRoleTitle { get; set; }
    public string? JobRoleSummary { get; set; }
    public List<string> RequiredTechnicalSkills { get; set; } = [];
    public List<string> RequiredSoftSkills { get; set; } = [];
    public string? ResumeSummary { get; set; }
    public string? ProfessionalObjective { get; set; }
    public string? AcademicProfileSummary { get; set; }
    public string? ThesisSummary { get; set; }
}

/// <summary>
/// Mints a FRESH ephemeral Realtime session for the NEXT sinodal in an Academic Defense committee
/// round-robin, right before the frontend connects to them — unlike Attempt_VoiceSession (which
/// mints all three committee sessions up front, before any conversation happens), this endpoint is
/// called at handoff time, so it can read the real transcript so far and ground the new sinodal's
/// instructions in what was actually asked/answered. Without this, every committee member starts
/// from a blank Realtime session with no memory of the conversation and re-introduces the
/// committee / repeats questions every time the round-robin switches speakers.
/// </summary>
public sealed class AttemptCommitteeNextSpeakerFunction
{
    private readonly SimulationLabsDbContext _db;
    private readonly LabRealtimeVoiceSessionService _voiceSessionService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<AttemptCommitteeNextSpeakerFunction> _logger;

    public AttemptCommitteeNextSpeakerFunction(
        SimulationLabsDbContext db,
        LabRealtimeVoiceSessionService voiceSessionService,
        ICurrentUserContext currentUser,
        ILogger<AttemptCommitteeNextSpeakerFunction> logger)
    {
        _db = db;
        _voiceSessionService = voiceSessionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Attempt_CommitteeNextSpeaker")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "attempts/{idAttempt:guid}/committee-voice-session")] HttpRequestData request,
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
                "La simulación por voz no está configurada.", correlationId, cancellationToken: cancellationToken);
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
            AttemptCommitteeNextSpeakerRequest? profileRequest;
            try
            {
                profileRequest = await JsonSerializer.DeserializeAsync<AttemptCommitteeNextSpeakerRequest>(request.Body, cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Cuerpo inválido: se requiere ActorId.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
            }
            if (profileRequest is null || profileRequest.ActorId == Guid.Empty)
            {
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "ActorId es requerido.", correlationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
            }

            var context = await LabSimulationPromptBuilder.LoadForAttemptAsync(_db, _currentUser.TenantId, attempt.LAB_IdVersion, attempt.SCN_IdScenario, cancellationToken);
            if (context is null || !string.Equals(context.Arquetipo, LabArquetipos.AcademicDefense, StringComparison.OrdinalIgnoreCase))
            {
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.UnprocessableEntity, "Este endpoint solo aplica a defensas de tesis.", correlationId, cancellationToken: cancellationToken);
            }
            var actor = context.CommitteeActors.FirstOrDefault(a => a.ACT_IdActor == profileRequest.ActorId);
            if (actor is null)
            {
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotFound, "Sinodal no encontrado en el comité de este escenario.", correlationId, cancellationToken: cancellationToken);
            }

            var employeeProfile = new LabSimulationPromptBuilder.EmployeeProfileContext
            {
                EmployeeName = profileRequest.EmployeeName,
                CompanyName = profileRequest.CompanyName,
                JobRoleTitle = profileRequest.JobRoleTitle,
                JobRoleSummary = profileRequest.JobRoleSummary,
                RequiredTechnicalSkills = profileRequest.RequiredTechnicalSkills,
                RequiredSoftSkills = profileRequest.RequiredSoftSkills,
                ResumeSummary = profileRequest.ResumeSummary,
                ProfessionalObjective = profileRequest.ProfessionalObjective,
                AcademicProfileSummary = profileRequest.AcademicProfileSummary,
                ThesisSummary = profileRequest.ThesisSummary,
            };

            var priorTurns = await _db.ConversationTurns.AsNoTracking()
                .Where(t => t.SEG_IdTenant == _currentUser.TenantId && t.ATT_IdAttempt == idAttempt)
                .OrderBy(t => t.TRN_NumeroTurno)
                .ToListAsync(cancellationToken);
            var transcriptLines = priorTurns.Select(t =>
            {
                if (t.TRN_SpeakerType == "PARTICIPANT") return $"Sofía: {t.TRN_Texto}";
                var speakerActor = context.CommitteeActors.FirstOrDefault(a => a.ACT_IdActor == t.ACT_IdActor);
                return $"{speakerActor?.ACT_Nombre ?? "Sinodal"}: {t.TRN_Texto}";
            });
            var transcript = string.Join("\n", transcriptLines);

            var instructions = LabSimulationPromptBuilder.BuildInstructions(
                context,
                _currentUser.DisplayName,
                isRealAttempt: true,
                employeeProfile: employeeProfile,
                actorOverride: actor,
                isFirstCommitteeSpeaker: profileRequest.IsFirstCommitteeSpeaker,
                priorConversationTranscript: transcript,
                isIntroductionOnlyTurn: profileRequest.IsIntroductionOnlyTurn);

            var session = await _voiceSessionService.CreateEphemeralSessionAsync(instructions, actor.ACT_VoiceName, cancellationToken);

            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, new AttemptVoiceSessionActorResponse
            {
                ClientSecret = session.ClientSecret,
                RealtimeCallsUrl = session.RealtimeCallsUrl,
                Model = session.Model,
                Voice = session.Voice,
                ExpiresAtUnixSeconds = session.ExpiresAtUnixSeconds,
                ActorNombre = actor.ACT_Nombre,
                ActorRol = actor.ACT_Rol,
                ActorId = actor.ACT_IdActor
            }, correlationId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadGateway, ex.Message, correlationId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error minting committee next-speaker voice session. AttemptId={AttemptId} CorrelationId={CorrelationId}", idAttempt, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }
}
