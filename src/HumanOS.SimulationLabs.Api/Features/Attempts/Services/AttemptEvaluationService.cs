using System.Text.Json;
using HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Services;

public sealed class AttemptNotFoundForEvaluationException : Exception { }
public sealed class AttemptNotCompletedException : Exception { }

public sealed class AttemptEvaluationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> ValidResultados = ["PASSED", "PARTIAL", "REPEAT_RECOMMENDED", "NOT_COMPLETED", "CRITICAL_FAILURE"];

    private readonly SimulationLabsDbContext _db; // marker123
    private readonly AttemptEvaluationAgent _agent;

    public AttemptEvaluationService(SimulationLabsDbContext db, AttemptEvaluationAgent agent)
    {
        _db = db;
        _agent = agent;
    }

    public async Task<AttemptEvaluationResponse> EvaluateAsync(
        Guid tenantId, Guid idAttempt, Guid participantId, EvaluateAttemptRequest request, CancellationToken ct)
    {
        var attempt = await _db.Attempts
            .FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == idAttempt, ct)
            ?? throw new AttemptNotFoundForEvaluationException();
        if (attempt.USR_IdParticipant != participantId) throw new AttemptNotFoundForEvaluationException();
        if (attempt.ATT_Estatus != AttemptEstatus.Completed) throw new AttemptNotCompletedException();

        var lab = await (
            from v in _db.LabVersions.AsNoTracking()
            join l in _db.Labs.AsNoTracking() on v.LAB_IdLab equals l.LAB_IdLab
            where v.LAB_IdVersion == attempt.LAB_IdVersion
            select l).FirstOrDefaultAsync(ct);

        // Global labs store their whole definition graph under the LAB'S OWN tenant, not the
        // participant's — read scenario/rubric/criteria with that tenant, never the caller's.
        var ownerTenantId = lab?.SEG_IdTenant ?? tenantId;

        var scenario = await _db.Scenarios.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SEG_IdTenant == ownerTenantId && s.SCN_IdScenario == attempt.SCN_IdScenario, ct);

        var rubric = await _db.Rubrics.AsNoTracking()
            .FirstOrDefaultAsync(r => r.SEG_IdTenant == ownerTenantId && r.LAB_IdVersion == attempt.LAB_IdVersion, ct);

        var criteria = rubric is null
            ? []
            : await _db.RubricCriteria.AsNoTracking()
                .Where(c => c.SEG_IdTenant == ownerTenantId && c.RUB_IdRubric == rubric.RUB_IdRubric)
                .OrderBy(c => c.CRT_Orden)
                .Select(c => new RubricCriterionInput
                {
                    Nombre = c.CRT_Nombre,
                    Descripcion = c.CRT_Descripcion,
                    Peso = c.CRT_Peso,
                    ScoreMinimoEsperado = c.CRT_ScoreMinimoEsperado,
                    EsCritico = c.CRT_EsCritico,
                    IndicadoresPositivos = c.CRT_IndicadoresPositivos,
                    IndicadoresNegativos = c.CRT_IndicadoresNegativos,
                    ErrorCritico = c.CRT_ErrorCritico,
                })
                .ToListAsync(ct);

        var skillFeedbackGuidance = await _db.TestedSkills.AsNoTracking()
            .Where(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == attempt.LAB_IdVersion && s.SKL_FeedbackGuidance != null)
            .Select(s => new SkillFeedbackGuidanceInput
            {
                SkillName = s.SKL_Nombre,
                Guidance = s.SKL_FeedbackGuidance!,
            })
            .ToListAsync(ct);

        var transcript = await _db.ConversationTurns.AsNoTracking()
            .Where(t => t.SEG_IdTenant == tenantId && t.ATT_IdAttempt == idAttempt && t.TRN_Estatus != TurnEstatus.Excluded)
            .OrderBy(t => t.TRN_NumeroTurno)
            .Select(t => new TranscriptTurnInput
            {
                NumeroTurno = t.TRN_NumeroTurno,
                SpeakerType = t.TRN_SpeakerType,
                Texto = t.TRN_Texto,
            })
            .ToListAsync(ct);
        transcript = MergeConsecutiveSameSpeakerTurns(transcript);

        var context = new AttemptEvaluationContext
        {
            LabNombre = lab?.LAB_Nombre ?? string.Empty,
            ScenarioNombre = scenario?.SCN_Nombre ?? string.Empty,
            ScenarioResultadoEsperado = scenario?.SCN_ResultadoEsperado ?? string.Empty,
            IsAcademicDefense = string.Equals(lab?.LAB_Arquetipo, LabArquetipos.AcademicDefense, StringComparison.OrdinalIgnoreCase),
            // Rubric scores are still stored on a 1.00-10.00 scale, but attempts are now graded
            // 1.00-5.00 — halve the configured passing bar to keep the same relative threshold.
            ScoreMinimoAprobacion = Math.Round((rubric?.RUB_ScoreMinimoAprobacion ?? 7.00m) / 2m, 2),
            Criteria = criteria,
            SkillFeedbackGuidance = skillFeedbackGuidance,
            Transcript = transcript,
            Employee = request,
        };

        var result = await _agent.EvaluateAsync(context, ct);
        var evaluationOutcome = result;
        var tokenUsage = evaluationOutcome.TokenUsage;
        var evaluationResult = evaluationOutcome.Result;

        // ScoreFinal and CriteriaScores are computed here from the model's own TurnEvaluations
        // (never from the LLM directly) so a topic that was never actually asked about can never
        // appear or drag down the score — grouped by SkillArea (TECHNICAL/SOFT), one entry per
        // area actually tested in the conversation.
        if (evaluationResult.TurnEvaluations.Count > 0)
        {
            evaluationResult.ScoreFinal = Math.Round(evaluationResult.TurnEvaluations.Average(t => t.Score), 2);
            evaluationResult.CriteriaScores = evaluationResult.TurnEvaluations
                .GroupBy(t => t.SkillArea)
                .Select(g => new CriterionEvaluationDto
                {
                    CriterionName = g.Key == "TECHNICAL" ? "Technical questions asked" : "Soft-skill questions asked",
                    Score = Math.Round(g.Average(t => t.Score), 2),
                    Evidence = string.Join(" | ", g.Select(t => t.Question)),
                })
                .ToList();
        }
        else
        {
            // No substantive question/answer pairs found at all — force a safe, DB-valid
            // minimum score rather than trusting whatever the model left ScoreFinal at (it's
            // instructed to leave it 0, which violates the 1.00-5.00 check constraint).
            evaluationResult.ScoreFinal = 1.00m;
            evaluationResult.Resultado = "NOT_COMPLETED";
            evaluationResult.CriteriaScores = [];
        }
        evaluationResult.ScoreFinal = Math.Clamp(evaluationResult.ScoreFinal, 1.00m, 5.00m);
        if (!ValidResultados.Contains(evaluationResult.Resultado)) evaluationResult.Resultado = "NOT_COMPLETED";

        var now = DateTimeOffset.UtcNow;
        var existing = await _db.AttemptEvaluations
            .FirstOrDefaultAsync(e => e.SEG_IdTenant == tenantId && e.ATT_IdAttempt == idAttempt, ct);

        var evaluation = existing ?? new LAB_AttemptEvaluation
        {
            EVL_IdEvaluation = Guid.NewGuid(),
            SEG_IdTenant = tenantId,
            ATT_IdAttempt = idAttempt,
            FechaCreacion = now,
            CreadoPor = "AttemptEvaluationAgent",
        };

        evaluation.EVL_ScoreFinal = evaluationResult.ScoreFinal;
        evaluation.EVL_Resultado = evaluationResult.Resultado;
        evaluation.EVL_Feedback = evaluationResult.Feedback;
        evaluation.EVL_StrengthsJson = JsonSerializer.Serialize(evaluationResult.Strengths, JsonOptions);
        evaluation.EVL_GapsJson = JsonSerializer.Serialize(evaluationResult.Gaps, JsonOptions);
        evaluation.EVL_RecommendedSkillsJson = JsonSerializer.Serialize(evaluationResult.RecommendedSkills, JsonOptions);
        evaluation.EVL_CriteriaScoresJson = JsonSerializer.Serialize(evaluationResult.CriteriaScores, JsonOptions);
        evaluation.EVL_TurnEvaluationsJson = JsonSerializer.Serialize(evaluationResult.TurnEvaluations, JsonOptions);
        evaluation.EVL_GeneratedModel = tokenUsage.ModelName;
        evaluation.EVL_InputTokens = tokenUsage.InputTokens;
        evaluation.EVL_OutputTokens = tokenUsage.OutputTokens;
        evaluation.EVL_CachedInputTokens = tokenUsage.CachedInputTokens;

        if (existing is null)
        {
            _db.AttemptEvaluations.Add(evaluation);
        }

        attempt.ATT_ScoreFinal = evaluationResult.ScoreFinal;
        attempt.ATT_Resultado = evaluationResult.Resultado;
        attempt.FechaActualizacion = now;

        await _db.SaveChangesAsync(ct);

        return ToResponse(evaluation);
    }

    public async Task<AttemptEvaluationResponse?> GetByAttemptAsync(Guid tenantId, Guid idAttempt, Guid participantId, CancellationToken ct)
    {
        var attempt = await _db.Attempts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.SEG_IdTenant == tenantId && a.ATT_IdAttempt == idAttempt, ct);
        if (attempt is null || attempt.USR_IdParticipant != participantId) return null;

        var evaluation = await _db.AttemptEvaluations.AsNoTracking()
            .FirstOrDefaultAsync(e => e.SEG_IdTenant == tenantId && e.ATT_IdAttempt == idAttempt, ct);
        return evaluation is null ? null : ToResponse(evaluation);
    }

    /// <summary>People naturally pause mid-answer and continue, and speech-to-text also sometimes
    /// splits one utterance into several transcription events — both produce multiple consecutive
    /// PARTICIPANT (or SIMULATED_ACTOR) rows in the DB for what is really ONE logical turn. Collapse
    /// consecutive same-speaker rows into a single combined turn (keeping the first turn number)
    /// before handing the transcript to the evaluator, so it always sees one clean Q/A per exchange
    /// instead of fragmented partial answers.</summary>
    private static List<TranscriptTurnInput> MergeConsecutiveSameSpeakerTurns(List<TranscriptTurnInput> turns)
    {
        var merged = new List<TranscriptTurnInput>();
        foreach (var turn in turns)
        {
            var last = merged.Count > 0 ? merged[^1] : null;
            if (last is not null && last.SpeakerType == turn.SpeakerType)
            {
                last.Texto = JoinFluently(last.Texto, turn.Texto);
            }
            else
            {
                merged.Add(new TranscriptTurnInput { NumeroTurno = turn.NumeroTurno, SpeakerType = turn.SpeakerType, Texto = turn.Texto });
            }
        }
        return merged;
    }

    /// <summary>Joins two fragments of the same speaker's turn so the result reads as one fluid
    /// sentence/paragraph instead of two clauses mashed together: if the first fragment already
    /// ended with sentence punctuation, capitalize the next fragment as a new sentence; otherwise
    /// just join with a space so it reads as one continuous sentence (the common case — someone
    /// paused mid-thought and continued).</summary>
    private static string JoinFluently(string first, string next)
    {
        first = first.Trim();
        next = next.Trim();
        if (first.Length == 0) return next;
        if (next.Length == 0) return first;

        var endsSentence = first[^1] is '.' or '!' or '?';
        if (!endsSentence)
        {
            return $"{first} {next}";
        }

        var capitalized = char.ToUpperInvariant(next[0]) + next[1..];
        return $"{first} {capitalized}";
    }

    private static AttemptEvaluationResponse ToResponse(LAB_AttemptEvaluation e) => new()
    {
        IdEvaluation = e.EVL_IdEvaluation,
        IdAttempt = e.ATT_IdAttempt,
        ScoreFinal = e.EVL_ScoreFinal,
        Resultado = e.EVL_Resultado,
        Feedback = e.EVL_Feedback,
        Strengths = JsonSerializer.Deserialize<List<string>>(e.EVL_StrengthsJson, JsonOptions) ?? [],
        Gaps = JsonSerializer.Deserialize<List<string>>(e.EVL_GapsJson, JsonOptions) ?? [],
        RecommendedSkills = JsonSerializer.Deserialize<List<RecommendedSkillDto>>(e.EVL_RecommendedSkillsJson, JsonOptions) ?? [],
        CriteriaScores = JsonSerializer.Deserialize<List<CriterionEvaluationDto>>(e.EVL_CriteriaScoresJson, JsonOptions) ?? [],
        TurnEvaluations = JsonSerializer.Deserialize<List<TurnEvaluationDto>>(e.EVL_TurnEvaluationsJson, JsonOptions) ?? [],
        FechaCreacion = e.FechaCreacion,
    };
}
