using Azure.AI.OpenAI;
using Azure.Identity;
using HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Services;

public sealed class AttemptEvaluationResult
{
    public decimal ScoreFinal { get; set; }

    /// <summary>PASSED, PARTIAL, REPEAT_RECOMMENDED, NOT_COMPLETED, CRITICAL_FAILURE.</summary>
    public string Resultado { get; set; } = string.Empty;

    public string Feedback { get; set; } = string.Empty;

    public List<string> Strengths { get; set; } = [];

    public List<string> Gaps { get; set; } = [];

    public List<RecommendedSkillDto> RecommendedSkills { get; set; } = [];

    public List<CriterionEvaluationDto> CriteriaScores { get; set; } = [];

    public List<TurnEvaluationDto> TurnEvaluations { get; set; } = [];
}

public sealed class AttemptEvaluationTokenUsage
{
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int CachedInputTokens { get; set; }

    public string? ModelName { get; set; }
}

/// <summary>Result of one evaluation call: the structured grading plus the token usage of the
/// LLM call, for cost tracking — see LabBuilderAgent's identical pattern.</summary>
public sealed class AttemptEvaluationOutcome
{
    public AttemptEvaluationResult Result { get; set; } = null!;

    public AttemptEvaluationTokenUsage TokenUsage { get; set; } = null!;
}

public sealed class TranscriptTurnInput
{
    public int NumeroTurno { get; set; }

    /// <summary>PARTICIPANT or SIMULATED_ACTOR.</summary>
    public string SpeakerType { get; set; } = string.Empty;

    public string Texto { get; set; } = string.Empty;
}

public sealed class RubricCriterionInput
{
    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public decimal Peso { get; set; }

    public decimal ScoreMinimoEsperado { get; set; }

    public bool EsCritico { get; set; }

    /// <summary>Observable positive behaviors that satisfy this criterion — written by the AI Lab Builder
    /// when the Lab was created. Ground truth for what "good" looks like on this criterion.</summary>
    public string IndicadoresPositivos { get; set; } = string.Empty;

    /// <summary>Observable negative behaviors/red flags for this criterion, if any.</summary>
    public string? IndicadoresNegativos { get; set; }

    /// <summary>What counts as an automatic critical failure on this criterion, if EsCritico.</summary>
    public string? ErrorCritico { get; set; }
}

/// <summary>Per-skill formative coaching guidance the AI Lab Builder wrote when the Lab was created
/// (TestedSkillDraft.FeedbackGuidance) — what to praise/correct for that specific skill.</summary>
public sealed class SkillFeedbackGuidanceInput
{
    public string SkillName { get; set; } = string.Empty;

    public string Guidance { get; set; } = string.Empty;
}

public sealed class AttemptEvaluationContext
{
    public string LabNombre { get; set; } = string.Empty;

    public string ScenarioNombre { get; set; } = string.Empty;

    public string ScenarioResultadoEsperado { get; set; } = string.Empty;
    public bool IsAcademicDefense { get; set; }

    public decimal ScoreMinimoAprobacion { get; set; } = 7.00m;

    public List<RubricCriterionInput> Criteria { get; set; } = [];

    public List<SkillFeedbackGuidanceInput> SkillFeedbackGuidance { get; set; } = [];

    public List<TranscriptTurnInput> Transcript { get; set; } = [];

    public EvaluateAttemptRequest Employee { get; set; } = new();
}

/// <summary>
/// Post-conversation evaluator (Microsoft Agent Framework) — reads the FULL transcript of a
/// completed LAB_Attempt of ANY kind (interview, negotiation, discovery call, feedback
/// conversation, etc.), the Lab's own rubric criteria, and the employee's real required job
/// skills, and produces a score, pass/fail result, narrative feedback, strengths/gaps, and a
/// list of specifically REQUIRED skills this employee should improve — never generic advice
/// disconnected from their actual role.
/// </summary>
public sealed class AttemptEvaluationAgent
{
    private const string Instructions = """
        You are the Attempt Evaluation agent for Human OS Simulation Labs. You grade ONE
        completed practice conversation (any kind: job interview, sales negotiation, discovery
        call, performance feedback conversation, customer support escalation, etc.) between a
        real employee (PARTICIPANT turns) and an AI-played character (SIMULATED_ACTOR turns).

        CRITICAL RULE - GROUND EVERYTHING IN WHAT WAS ACTUALLY ASKED: you must NEVER penalize,
        list as a weakness, or otherwise judge the employee on any topic or skill that was NOT
        actually raised as a question/prompt in THIS conversation. Only ever judge the employee
        on the exact questions/prompts that were really asked and the exact answers they really
        gave. Do not reference any rubric, checklist, or job-description topic that never came up.

        YOU RECEIVE:
        - The Lab/Scenario context (what this exam is meant to evaluate) and the minimum passing
          score, for context only.
        - The full ordered transcript of the conversation.
        - The employee's REAL required job skills, broken into four lists: Technical Skills,
          Soft Skills, Department Processes, HR/Compliance Processes. Some or all lists may be
          empty — only use the lists that are non-empty; never invent required skills that
          weren't given to you.

        YOUR JOB, IN THIS ORDER:
        1. TurnEvaluations (this is the ONLY scoring mechanism — do not score anything else):
           walk the transcript and identify every SIMULATED_ACTOR turn that is a real question or
           prompt requiring a substantive PARTICIPANT response, paired with the PARTICIPANT
           turn(s) that answered it. Skip small-talk/greetings that aren't real evaluation
           questions. For EACH such question/answer pair, produce one entry with:
           QuestionTurnNumber/AnswerTurnNumber (the transcript turn numbers), Question (the
           actor's exact question/prompt, verbatim or lightly trimmed), Answer (the participant's
           exact response, verbatim or lightly trimmed), SkillArea ("TECHNICAL" or "SOFT" -
           whichever this specific question was really testing), Score (1.00-5.00 for that single
           answer, based ONLY on how well it answered THIS question), Assessment (1-2 sentences on
           what was good/missing in THIS answer specifically), and RecommendedAnswer (a concrete,
           best-practice model answer — grounded in real technical knowledge, industry best
           practices, and strong soft-skill communication for this exact question, showing what a
           top-tier response would have said). Be thorough and exhaustive: cover EVERY substantive
           question actually asked, in order, not just a sample.
           SCORING SCALE — 1.00 to 5.00, and be generous: this is a formative practice exercise,
           not a hard exam. Use 5 for a strong, correct, complete answer; 4 for a solid answer with
           only minor gaps; 3 for an adequate/partial answer that gets the main idea across even if
           imprecise or incomplete; 2 for a weak answer that shows some relevant effort but misses
           most of what was asked; 1 only for a missing, irrelevant, or seriously wrong answer. Most
           real, on-topic attempts should land on 3, 4, or 5 — reserve 1-2 for genuinely poor or
           absent answers, not merely imperfect ones.
           IMPORTANT — if RUBRIC CRITERIA were provided below, they are the GROUND TRUTH standard for
           what "good" looks like on this Lab: when scoring and writing Assessment for a turn, check
           the answer against the criterion's positive/negative indicators that apply to that
           topic (not your own invented standard), and if a criterion is marked CRITICAL and its
           "automatic critical failure" condition clearly happened, say so explicitly in Assessment.
        2. Resultado: PASSED if the average of your TurnEvaluations' Scores is >= the Lab's
           ScoreMinimoAprobacion; CRITICAL_FAILURE if answers showed a severe, safety/compliance-
           relevant failure; PARTIAL if close but below the minimum; REPEAT_RECOMMENDED if
           performance was weak but showed some real effort; NOT_COMPLETED only if no substantive
           question/answer pairs exist at all.
        3. Feedback: 2-4 sentences, direct and constructive, in the SAME language the employee
           spoke in the transcript, addressed to the employee, referencing only what actually
           happened in the conversation.
        4. Strengths: 2-4 natural-language sentences summarizing patterns of what the employee did
           well across the conversation, grounded in the actual TurnEvaluations but written as
           plain prose for a human to read — NEVER format entries as "TurnEvaluation
           (questionTurnNumber X / answerTurnNumber Y): ...", never mention turn numbers or the
           word "TurnEvaluation" at all, and never just restate/list what's already shown in the
           question-by-question review below. Summarize the overall pattern instead of repeating
           every single instance. If SKILL-SPECIFIC COACHING GUIDANCE was provided for a skill the
           employee did well on, reuse its language/framing here.
        5. Weaknesses (JSON field name: Gaps): same rule as Strengths above — 2-4 natural-language
           sentences summarizing the real recurring patterns of where the employee's answers fell
           short of what was ACTUALLY asked, written as plain prose (no turn-number references, no
           "TurnEvaluation" labels, no bullet-per-turn listing). NEVER mention a topic/skill that
           was never asked about. If SKILL-SPECIFIC COACHING GUIDANCE was provided for a skill the
           employee struggled with, reuse its corrective-coaching language/framing here.
        6. RecommendedSkills — cross-reference the Weaknesses against the employee's REAL required
           skill lists (Technical/Soft/Department/HR), using each weak TurnEvaluation's SkillArea
           to decide which list to check. For each of the employee's required skills that a real,
           actually-asked question showed room to improve, add one entry with SkillName (copied
           verbatim from the list they gave you), SkillCategory (TECHNICAL, SOFT,
           DEPARTMENT_PROCESS, or HR_PROCESS matching which list it came from), and a Reason
           grounded in the specific question/answer that showed the gap. Only recommend skills
           that are in the employee's own required lists — never invent a skill name that wasn't
           given to you, and never recommend a skill unrelated to a question actually asked. If
           nothing in their required lists relates to a real weakness you found, leave
           RecommendedSkills covering only genuine, evidence-backed matches (can be empty).

        Leave CriteriaScores and ScoreFinal as empty/zero in your output — those are computed
        separately from your TurnEvaluations, not by you.

        Never invent transcript content or skills beyond what you were given, and never judge
        anything that wasn't actually asked about. Return only the structured JSON result, no
        prose, no markdown.
        """;

    private readonly AzureOpenAIClient? _client;
    private readonly string? _deploymentName;

    public AttemptEvaluationAgent(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAIEndpoint"];
        var deploymentName = configuration["AzureOpenAIDeploymentName"];
        var apiKey = configuration["AzureOpenAIApiKey"];

        _deploymentName = deploymentName;

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deploymentName))
        {
            _client = null;
            return;
        }

        _client = string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));
    }

    public bool IsConfigured => _client is not null;

    public async Task<AttemptEvaluationOutcome> EvaluateAsync(AttemptEvaluationContext context, CancellationToken cancellationToken = default)
    {
        if (_client is null || _deploymentName is null)
        {
            throw new InvalidOperationException("The Attempt Evaluation agent is not configured. Set AzureOpenAIEndpoint and AzureOpenAIDeploymentName.");
        }

        var agent = _client.GetChatClient(_deploymentName).AsAIAgent(instructions: Instructions, name: "AttemptEvaluationAgent");
        var prompt = BuildPrompt(context);

        var response = await agent.RunAsync<AttemptEvaluationResult>(prompt, cancellationToken: cancellationToken);
        var usage = response.Usage;
        return new AttemptEvaluationOutcome
        {
            Result = response.Result,
            TokenUsage = new AttemptEvaluationTokenUsage
            {
                InputTokens = (int)(usage?.InputTokenCount ?? 0),
                OutputTokens = (int)(usage?.OutputTokenCount ?? 0),
                CachedInputTokens = (int)(usage?.CachedInputTokenCount ?? 0),
                ModelName = _deploymentName,
            },
        };
    }

    private static string BuildPrompt(AttemptEvaluationContext context)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"LAB: {context.LabNombre}");
        sb.AppendLine($"SCENARIO: {context.ScenarioNombre}");
        sb.AppendLine($"What this exam evaluates: {context.ScenarioResultadoEsperado}");
        sb.AppendLine($"Minimum passing score: {context.ScoreMinimoAprobacion}");
        if (context.IsAcademicDefense)
        {
            sb.AppendLine();
            sb.AppendLine("=== ACADEMIC DEFENSE SCORING CALIBRATION ===");
            sb.AppendLine("This is a Tec de Monterrey thesis defense. The sinodales evaluate Sofia as a candidate, but this is a formative professional examination, not a demand for a perfect thesis recital.");
            sb.AppendLine("Score each answer for the specific question actually asked. Give fair partial credit when the candidate demonstrates a correct idea but omits detail, uses imprecise wording, or needs a follow-up.");
            sb.AppendLine("Do not require one answer to cover the entire thesis, every methodological detail, or every rubric dimension. Do not give a very low score merely because the candidate needs clarification or a second attempt.");
            sb.AppendLine("Use approximately 4-5 for a correct, relevant and sufficiently supported answer; 3 for a substantially correct answer with meaningful gaps or a partial but relevant answer; 2 for a weak but on-topic attempt; 1 only for no answer, an irrelevant answer, or a serious factual/conceptual failure.");
            sb.AppendLine("A weak answer to one question is not by itself a critical failure. Reserve CRITICAL_FAILURE for a clearly severe issue supported by the transcript and an applicable critical criterion.");
        }
        sb.AppendLine();

        var e = context.Employee;
        sb.AppendLine("=== EMPLOYEE REQUIRED SKILLS (this employee's real job role) ===");
        if (!string.IsNullOrWhiteSpace(e.EmployeeName)) sb.AppendLine($"Employee: {e.EmployeeName}");
        if (!string.IsNullOrWhiteSpace(e.JobRoleTitle)) sb.AppendLine($"Job Role: {e.JobRoleTitle}");
        AppendSkillList(sb, "Technical Skills", e.RequiredTechnicalSkills);
        AppendSkillList(sb, "Soft Skills", e.RequiredSoftSkills);
        AppendSkillList(sb, "Department Processes", e.RequiredDepartmentProcesses);
        AppendSkillList(sb, "HR/Compliance Processes", e.RequiredHrProcesses);
        sb.AppendLine();

        if (context.Criteria.Count > 0)
        {
            sb.AppendLine("=== RUBRIC CRITERIA (ground truth for scoring — use these indicators, not your own invented standard) ===");
            foreach (var c in context.Criteria)
            {
                var criticalTag = c.EsCritico ? " [CRITICAL — failing this can force CRITICAL_FAILURE]" : "";
                sb.AppendLine($"- {c.Nombre} (weight {c.Peso}, min expected score {c.ScoreMinimoEsperado}){criticalTag}: {c.Descripcion}");
                sb.AppendLine($"    Positive indicators (what a good answer looks like): {c.IndicadoresPositivos}");
                if (!string.IsNullOrWhiteSpace(c.IndicadoresNegativos))
                    sb.AppendLine($"    Negative indicators (red flags): {c.IndicadoresNegativos}");
                if (!string.IsNullOrWhiteSpace(c.ErrorCritico))
                    sb.AppendLine($"    Automatic critical failure if: {c.ErrorCritico}");
            }
            sb.AppendLine();
        }

        if (context.SkillFeedbackGuidance.Count > 0)
        {
            sb.AppendLine("=== SKILL-SPECIFIC COACHING GUIDANCE (use this language/framing in Feedback, Strengths and Gaps for the matching skill) ===");
            foreach (var g in context.SkillFeedbackGuidance)
            {
                sb.AppendLine($"- {g.SkillName}: {g.Guidance}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("=== FULL TRANSCRIPT (in order) ===");
        foreach (var t in context.Transcript)
        {
            sb.AppendLine($"[{t.NumeroTurno}] {t.SpeakerType}: {t.Texto}");
        }

        return sb.ToString();
    }

    private static void AppendSkillList(System.Text.StringBuilder sb, string label, List<string> skills)
    {
        if (skills.Count == 0) return;
        sb.AppendLine($"{label}: {string.Join(", ", skills)}");
    }
}
