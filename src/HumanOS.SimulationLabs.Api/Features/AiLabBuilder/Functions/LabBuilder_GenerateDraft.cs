using System.Net;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Common;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Validators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Functions;

/// <summary>
/// LabBuilder_GenerateDraft — invokes the AI Lab Builder agent and returns a Lab draft
/// for human review. Does not write to Azure SQL and does not approve/publish anything.
/// </summary>
public sealed class LabBuilderGenerateDraftFunction
{
    private readonly LabBuilderAgent _agent;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<LabBuilderGenerateDraftFunction> _logger;
    private readonly IConfiguration _configuration;

    public LabBuilderGenerateDraftFunction(LabBuilderAgent agent, ICurrentUserContext user, ILogger<LabBuilderGenerateDraftFunction> logger, IConfiguration configuration)
    {
        _agent = agent;
        _user = user;
        _logger = logger;
        _configuration = configuration;
    }

    [Function("LabBuilder_GenerateDraft")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ai-lab-builder/generate-draft")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        if (!_user.IsAuthenticated)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Unauthorized, "No autenticado.", _user.CorrelationId, errorCode: "UNAUTHENTICATED", cancellationToken: cancellationToken);
        }

        if (!_user.HasPermission(AiLabBuilderPolicies.Generate))
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.Forbidden, "Usuario sin permiso.", _user.CorrelationId, errorCode: "FORBIDDEN", cancellationToken: cancellationToken);
        }

        if (!_agent.IsConfigured)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.NotImplemented, "El agente AI Lab Builder no está configurado.", _user.CorrelationId, errorCode: "AGENT_NOT_CONFIGURED", cancellationToken: cancellationToken);
        }

        GenerateLabDraftRequest? body;
        try
        {
            body = await JsonSerializer.DeserializeAsync<GenerateLabDraftRequest>(request.Body, ApiResponses.JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo no es un JSON válido.", _user.CorrelationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        if (body is null)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "El cuerpo es requerido.", _user.CorrelationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        // If a process PDF was uploaded, extract its text server-side and use that as
        // ProcessContent, instead of requiring the caller to paste text by hand.
        if (!string.IsNullOrWhiteSpace(body.ProcessFileName) && !string.IsNullOrWhiteSpace(body.ProcessContentBase64))
        {
            byte[] pdfBytes;
            try
            {
                pdfBytes = Convert.FromBase64String(body.ProcessContentBase64);
            }
            catch (FormatException)
            {
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "processContentBase64 no es base64 válido.", _user.CorrelationId, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
            }

            string extractedText;
            try
            {
                extractedText = LabProcessPdfExtractor.ExtractText(pdfBytes);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, $"No se pudo leer '{body.ProcessFileName}'. ¿Es un PDF válido y no encriptado?", _user.CorrelationId, errorCode: "PDF_EXTRACTION_FAILED", cancellationToken: cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return await ApiResponses.ProblemAsync(request, HttpStatusCode.UnprocessableEntity, $"'{body.ProcessFileName}' no tiene texto extraíble (puede ser un PDF escaneado/solo-imagen).", _user.CorrelationId, errorCode: "PDF_HAS_NO_TEXT", cancellationToken: cancellationToken);
            }

            body.ProcessContent = extractedText;
        }

        var validationErrors = AiLabBuilderValidators.ValidateRequest(body);
        if (validationErrors.Count > 0)
        {
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.BadRequest, "Solicitud inválida.", _user.CorrelationId, errors: validationErrors, errorCode: "REQUEST_INVALID", cancellationToken: cancellationToken);
        }

        try
        {
            var outcome = await _agent.GenerateDraftAsync(body, cancellationToken);
            var generation = outcome.Generation;
            var costEstimate = LabBuilderCostEstimator.Estimate(outcome.TokenUsage, _configuration);

            if (generation.NeedsInformation || generation.Draft is null)
            {
                var needsInfo = new GenerateLabDraftResponse
                {
                    Status = "NEEDS_INFORMATION",
                    Questions = generation.Questions,
                    HumanReviewRequired = true,
                    CorrelationId = _user.CorrelationId,
                    Cost = costEstimate,
                };
                return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, needsInfo, _user.CorrelationId, cancellationToken);
            }

            var warnings = new List<string>();
            var criteriaWeightSum = generation.Draft.Criteria.Sum(c => c.Weight);
            if (Math.Abs(criteriaWeightSum - 100m) > 0.5m)
            {
                warnings.Add($"La suma de pesos de los criterios es {criteriaWeightSum}, no 100. Revisar antes de aprobar.");
            }

            // Hard enforcement (don't just trust the prompt): the live conversation is free-format
            // now, driven by Scenario/Actors/Objectives/Skills/Criteria — only ONE scripted dialogue
            // (the opening line) should ever exist. If the model still generated more, keep only
            // the first (lowest Order) and drop the rest.
            if (generation.Draft.Dialogues.Count > 1)
            {
                var first = generation.Draft.Dialogues.OrderBy(d => d.Order).First();
                warnings.Add($"El agente generó {generation.Draft.Dialogues.Count} diálogos; se conservó solo el primero (apertura) porque la conversación ahora es libre.");
                generation.Draft.Dialogues = [first];
            }

            var response = new GenerateLabDraftResponse
            {
                Status = "DRAFT_READY_FOR_REVIEW",
                LabName = !string.IsNullOrWhiteSpace(body.LabName) ? body.LabName : generation.Draft.SuggestedLabName,
                TargetRole = body.TargetRole,
                Language = body.LabLanguage,
                Difficulty = body.Difficulty,
                ScoreMinimo = AiLabBuilderValidators.MinScoreFor(body.Difficulty),
                Draft = generation.Draft,
                Warnings = warnings,
                HumanReviewRequired = true,
                CorrelationId = _user.CorrelationId,
                Cost = costEstimate,
            };

            _logger.LogInformation(
                "LabBuilder_GenerateDraft ok. TenantId={TenantId} UserId={UserId} CorrelationId={CorrelationId} DialogueCount={DialogueCount} ActorCount={ActorCount} CriterionCount={CriterionCount} InputTokens={InputTokens} OutputTokens={OutputTokens} ElapsedMilliseconds={ElapsedMilliseconds} EstimatedCostUsd={EstimatedCostUsd}",
                _user.TenantId, _user.UserId, _user.CorrelationId, generation.Draft.Dialogues.Count, generation.Draft.Actors.Count, generation.Draft.Criteria.Count, outcome.TokenUsage.InputTokens, outcome.TokenUsage.OutputTokens, outcome.TokenUsage.ElapsedMilliseconds, costEstimate.EstimatedCostUsd);

            return await ApiResponses.JsonAsync(request, HttpStatusCode.OK, response, _user.CorrelationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LabBuilder_GenerateDraft failed. TenantId={TenantId} CorrelationId={CorrelationId}", _user.TenantId, _user.CorrelationId);
            return await ApiResponses.ProblemAsync(request, HttpStatusCode.InternalServerError, "Ocurrió un error inesperado generando el borrador.", _user.CorrelationId, errorCode: "INTERNAL_ERROR", cancellationToken: cancellationToken);
        }
    }
}
