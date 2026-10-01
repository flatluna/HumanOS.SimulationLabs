using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Services;

/// <summary>
/// Mints short-lived ephemeral client secrets for the Azure OpenAI GPT Realtime API, used ONLY
/// for the Studio "Probar simulación" flow (an admin previewing a Lab actor's voice before
/// publishing it — never a real participant attempt). Ported from HumanOS/backend/HumanOS's
/// RealtimeVoiceSessionService — same GA REST endpoint, same never-expose-the-real-key model:
/// the browser only ever receives the ephemeral secret returned here and negotiates WebRTC
/// directly against Azure.
/// </summary>
public sealed class LabRealtimeVoiceSessionService
{
    private readonly HttpClient _httpClient;
    private readonly string? _endpoint;
    private readonly string? _deploymentName;
    private readonly string? _apiKey;
    private readonly string _defaultVoice;

    public LabRealtimeVoiceSessionService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(LabRealtimeVoiceSessionService));
        // Same dual-resource setup as the main HumanOS backend (gpt-realtime-mini is pinned on
        // its own resource) — falls back to the plain chat settings if unset, so a single-resource
        // dev environment still works.
        _endpoint = configuration["AzureOpenAIRealtimeEndpoint"] ?? configuration["AzureOpenAIEndpoint"];
        _deploymentName = configuration["AzureOpenAIRealtimeDeploymentName"];
        _apiKey = configuration["AzureOpenAIRealtimeApiKey"] ?? configuration["AzureOpenAIApiKey"];
        // "echo" (2026-09-14) — both "cedar" and "marin" returned 400 OpperationNotSupported once
        // combined with this session's full config (transcription + turn_detection), even though a bare
        // client_secrets call with just the voice succeeded in isolation. Reverted to "echo", the only
        // voice confirmed fully working end-to-end with this deployment's real session shape.
        _defaultVoice = configuration["AzureOpenAIRealtimeVoice"] is { Length: > 0 } configuredVoice ? configuredVoice : "echo";
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_endpoint) && !string.IsNullOrWhiteSpace(_deploymentName) && !string.IsNullOrWhiteSpace(_apiKey);

    public sealed class EphemeralSession
    {
        public string ClientSecret { get; set; } = string.Empty;

        public string RealtimeCallsUrl { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string Voice { get; set; } = string.Empty;

        public long? ExpiresAtUnixSeconds { get; set; }
    }

    /// <param name="voiceOverride">The Lab actor's own ACT_VoiceName, when set — otherwise falls
    /// back to the configured default (a male-sounding voice, "echo").</param>
    public async Task<EphemeralSession> CreateEphemeralSessionAsync(
        string instructions, string? voiceOverride, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "LabRealtimeVoiceSessionService is not configured. Set 'AzureOpenAIRealtimeEndpoint', " +
                "'AzureOpenAIRealtimeDeploymentName' and 'AzureOpenAIRealtimeApiKey' application settings.");
        }

        var voice = string.IsNullOrWhiteSpace(voiceOverride) ? _defaultVoice : voiceOverride;
        var baseUri = _endpoint!.TrimEnd('/');
        var requestUri = $"{baseUri}/openai/v1/realtime/client_secrets";

        var sessionConfig = new
        {
            session = new
            {
                type = "realtime",
                model = _deploymentName,
                instructions,
                audio = new
                {
                    output = new { voice },
                    input = new
                    {
                        transcription = new { model = "whisper-1" },
                        turn_detection = new
                        {
                            type = "server_vad",
                            // Now matches VoiceTutorAgent.tsx/RealtimeVoiceSessionService.cs's tuned value
                            // (2026-08-27) exactly. The 0.85 tried here (2026-09-22) turned out too strict
                            // to ever register a genuine student interruption as "speech started", so
                            // barge-in silently never fired — the ACTUAL stuttering/restart bug (fixed
                            // separately) was a stray response.cancel from committee hand-off logic that
                            // ran even for single-actor Labs, not VAD sensitivity.
                            threshold = 0.65,
                            prefix_padding_ms = 300,
                            silence_duration_ms = 650,
                            create_response = true,
                            interrupt_response = true
                        },
                        noise_reduction = new { type = "near_field" }
                    }
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(sessionConfig), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("api-key", _apiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Azure OpenAI Realtime client_secrets request failed ({(int)response.StatusCode}): {body}");
        }

        using var parsedResponse = JsonDocument.Parse(body);
        var root = parsedResponse.RootElement;

        if (!root.TryGetProperty("value", out var valueElement) || valueElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Azure OpenAI Realtime client_secrets response did not contain a 'value' field: {body}");
        }

        long? expiresAt = root.TryGetProperty("expires_at", out var expiresElement) && expiresElement.ValueKind == JsonValueKind.Number
            ? expiresElement.GetInt64()
            : null;

        return new EphemeralSession
        {
            ClientSecret = valueElement.GetString()!,
            RealtimeCallsUrl = $"{baseUri}/openai/v1/realtime/calls",
            Model = _deploymentName!,
            Voice = voice,
            ExpiresAtUnixSeconds = expiresAt
        };
    }
}
