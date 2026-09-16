using System.Net;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.Labs.Services;
using HumanOS.SimulationLabs.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Functions;

public sealed class LabSimulationVoiceSessionResponse
{
    public string ClientSecret { get; set; } = string.Empty;

    public string RealtimeCallsUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Voice { get; set; } = string.Empty;

    public long? ExpiresAtUnixSeconds { get; set; }

    public string? ActorNombre { get; set; }

    public string? ActorRol { get; set; }
}

/// <summary>
/// Studio-only "Probar simulación" preview (2026-09-14): mints an ephemeral Azure OpenAI
/// Realtime session so an ADMIN editing a Lab can experience the AI actor's voice conversation
/// before publishing it. Deliberately READ-ONLY — never creates a LAB_Attempt or any
/// LAB_ConversationTurn row; this is only a preview, not a real recorded attempt.
/// </summary>
public sealed class LabSimulationVoiceSessionFunction
{
    private readonly SimulationLabsDbContext _db;
    private readonly LabRealtimeVoiceSessionService _voiceSessionService;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<LabSimulationVoiceSessionFunction> _logger;

    public LabSimulationVoiceSessionFunction(
        SimulationLabsDbContext db,
        LabRealtimeVoiceSessionService voiceSessionService,
        ICurrentUserContext currentUser,
        ILogger<LabSimulationVoiceSessionFunction> logger)
    {
        _db = db;
        _voiceSessionService = voiceSessionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [Function("Lab_SimulationVoiceSession")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labs/{idLab:guid}/simulate-voice-session")] HttpRequestData request,
        Guid idLab,
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

        try
        {
            var context = await LabSimulationPromptBuilder.LoadAsync(_db, _currentUser.TenantId, idLab, cancellationToken);
            if (context is null || context.Scenario is null || context.Actor is null)
            {
                return await ApiResponses.ProblemAsync(
                    request, HttpStatusCode.UnprocessableEntity,
                    "El Lab no tiene todavía una versión con un escenario y al menos un actor simulado.",
                    correlationId, cancellationToken: cancellationToken);
            }

            var instructions = LabSimulationPromptBuilder.BuildInstructions(context, _currentUser.DisplayName);
            var session = await _voiceSessionService.CreateEphemeralSessionAsync(instructions, context.Actor.ACT_VoiceName, cancellationToken);

            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, new LabSimulationVoiceSessionResponse
            {
                ClientSecret = session.ClientSecret,
                RealtimeCallsUrl = session.RealtimeCallsUrl,
                Model = session.Model,
                Voice = session.Voice,
                ExpiresAtUnixSeconds = session.ExpiresAtUnixSeconds,
                ActorNombre = context.Actor.ACT_Nombre,
                ActorRol = context.Actor.ACT_Rol
            }, correlationId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadGateway, ex.Message, correlationId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error minting Lab simulation voice session for Lab {IdLab}. CorrelationId={CorrelationId}", idLab, correlationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.", correlationId, cancellationToken: cancellationToken);
        }
    }
}
