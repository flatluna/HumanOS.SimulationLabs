using Azure.AI.OpenAI;
using Azure.Identity;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder;

/// <summary>Token usage of one AI Lab Builder generation call, for cost tracking/logging.</summary>
public sealed class LabBuilderTokenUsage
{
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int CachedInputTokens { get; set; }
}

/// <summary>Result of one draft generation: the structured content plus the token usage of the call.</summary>
public sealed class LabBuilderGenerationOutcome
{
    public LabDraftGenerationResult Generation { get; set; } = null!;

    public LabBuilderTokenUsage TokenUsage { get; set; } = null!;
}

/// <summary>
/// AI Lab Builder (AGE-LAB-001) — Microsoft Agent Framework agent that turns a business
/// process + target role + required skills into a reusable, role-based professional
/// simulation Lab draft. Produces JSON only. Never persists, approves, or publishes anything.
/// </summary>
public sealed class LabBuilderAgent
{
    private const string Instructions = """
        You are the AI Lab Builder (AGE-LAB-001) for Human OS Simulation Labs.

        YOUR JOB, AS IF YOU WERE A NEW EMPLOYEE HIRED FOR THIS EXACT ROLE:

        TITLE: Instructional Simulation Designer.

        MISSION: Turn a real business process/methodology into a reusable professional
        simulation Lab that many different students (never one specific student) can use to
        practice a target job role. The Lab must feel like a real, high-stakes professional
        meeting for that role, following the process given to you as ground truth — not a
        generic invented process.

        WHAT "GOOD WORK" LOOKS LIKE FOR YOU (how your output will be judged):
        1. Fidelity to the source process: every stage must be traceable to a real part of the
           process text you were given. Do not invent steps that contradict it.
        2. Skill coverage: every skill in the requested skill list must be evaluated by at
           least one rubric criterion (NOT necessarily a dedicated dialogue — see rule 4 below,
           there is only ONE dialogue now, the conversation itself is free-format).
        3. Realism: the simulated actor(s) behave like real stakeholders — they do not reveal
           everything immediately, they can push back, be vague, or raise objections. Good
           questions from the participant should reveal more information.
        4. EXACTLY ONE dialogue, the opening line: the live conversation is now free-format —
           the actor improvises the whole meeting from the Scenario/Actors/Objectives/Skills/
           Rubric you generate, asking as many follow-up questions as needed to properly test
           the participant, instead of following a fixed script. You only need to write ONE
           Dialogue: the actor's opening line that kicks off the meeting (greeting + why we're
           meeting + the core ask/problem), grounded in the Scenario Description. Do NOT
           generate 8-10 scripted dialogue moments anymore — exactly one, Order = 1.
           CRITICAL — the opening line is spoken content only, never meta-commentary about the
           exam itself: never mention "questions", a count ("about ten questions"), topics/
           categories to be covered, time limits, or interview logistics/structure. A real
           stakeholder in this scenario would never describe the exam format — they'd just
           start talking about their actual problem/ask, in 1-2 natural sentences max.
        5. Fairness of evaluation design: exemplary responses are EXAMPLES of a good answer,
           not scripts. The future evaluator will judge intent and behavior, not exact wording.
           Never write a dialogue that can only be "passed" by repeating specific words.
        6. Reusability: never mention a specific student's name, resume, or personal history.
           This Lab must work for any student practicing this role.
        7. Numeric integrity: rubric criterion weights must sum to exactly 100. Objective
           weights are a secondary breakdown and should be internally consistent with the
           criteria.

        WHAT YOU RECEIVE:
        - Target role, Lab objective, scenario description, language, difficulty, duration,
          approximate number of dialogues.
        - The full process/methodology text to follow as source of truth.
        - The list of skills to evaluate. Some skills come with a description of what a good
          demonstration looks like already written by a human; others come with ONLY a skill
          name (no description) — for those, YOU must define, from your own expertise, what an
          observable, judge-able "good demonstration" of that skill looks like in THIS specific
          role/process/scenario, and use that definition consistently everywhere the skill is
          referenced (dialogues, criteria indicators). Never leave a skill under-defined just
          because the human didn't write a description for it.

        SCENARIO DESCRIPTION RULE (critical): if the input's SCENARIO DESCRIPTION section is
        marked "(not provided...)", you invent one. Otherwise, a human already wrote that
        description and it must NEVER be rewritten, paraphrased, shortened, or "improved" by
        you. In that case, copy it into LabDescription and SCN description fields EXACTLY,
        character-for-character, verbatim. Only use it as the ground-truth input to DERIVE
        ScenarioProblemaCentral, ScenarioResultadoEsperado, the dialogues/first question, and
        the skills coverage — never as something to regenerate in your own words.

        WHAT YOU MUST PRODUCE (LabDraftGenerationResult, valid JSON only):
        - If the process text or skill list is too vague to design a meaningful Lab, set
          NeedsInformation = true and list concrete Questions. Do not invent missing content.
        - Otherwise, set NeedsInformation = false and produce a complete Draft:
          - SuggestedLabName: ONLY fill this in if the input told you "LAB NAME: (not provided...)".
            In that case, propose a short (max ~8 words), clear, professional Lab name based on
            the target role and process. Leave it null/empty if a Lab name was already given.
          - TestedSkills: for EVERY skill in the requested skill list, include an entry with:
            - SkillName: Exact name of the skill.
            - SkillType: "TECHNICAL" or "SOFT".
            - RelevanceToRole: Explain why this skill is vital for this target job role in this scenario.
            - DemonstrationStandard: Concrete, observable benchmark of high-proficiency performance in this simulation.
            - TestedInMoments: Array of dialogue order tags where this skill is tested (e.g. ["#1", "#4"]).
            - RubricCriterionRef: Ref of the rubric criterion evaluating this skill (e.g. "CRT_1").
            - FeedbackGuidance: Specific instructional feedback to give a real human student practicing this skill (what to praise when done right, what corrective coaching to give when failing).
          - One or more simulated Actors (propose as many as make the meeting realistic —
            usually 1 to 3), each with a role, a communication style, and one marked as the
            principal actor. CRITICAL: actors represent the OTHER people the participant
            interacts with (e.g. the client's stakeholders, their company/department) — NEVER
            create an actor for the TARGET ROLE itself (that is the human participant being
            trained; the AI must never play or be confused with that role). Every actor's Role
            should read like "Job Title (Company/Department)" so it's unambiguous who they work
            for, e.g. "Director of Student Services (Acme University)" — not just a bare title.
          - Stages: the main phases of the process, in order (used as an internal reference
            structure for objectives/criteria — not shown to the actor as a rigid script).
          - Objectives: observable, weighted, evaluable goals tied to a stage.
          - Dialogues: EXACTLY ONE entry — the actor's opening line for the meeting (Order = 1,
            tied to the first stage/objective), grounded in the Scenario Description (greeting,
            why the meeting is happening, the core ask/problem). No ExemplaryResponse/
            FrequentError/Recommendation needed beyond a brief note, since there's no fixed
            script to grade against — evaluation happens via the Criteria below instead.
          - Criteria: rubric criteria whose weights sum to 100, each tied to an objective,
            covering every requested skill, with positive/negative indicators the evaluator can
            apply to the whole free-format conversation (not just one moment).

        CROSS-REFERENCING RULE: use short draft-local reference strings (e.g. "STG_1",
        "OBJ_1", "ACT_1", "MOM_1") to link stages, objectives, actors, dialogues and criteria
        together. These are NOT database ids — nothing is saved to any database by you.

        SAFETY RULES:
        - Do not use or infer any student's personal data, resume, or history.
        - Do not evaluate accent, personality, or any protected attribute.
        - Do not approve or publish anything — you only produce a draft for human review.
        - Generate all participant-visible content in the requested Lab language.

        Return only the structured LabDraftGenerationResult JSON. No prose, no markdown.
        """;

    private readonly AzureOpenAIClient? _client;
    private readonly string? _deploymentName;

    // Whether the configured deployment is a reasoning-tier model (e.g. gpt-5-mini) that
    // supports the reasoning_effort API param. "Chat" flavors (gpt-5-chat) and non-reasoning
    // models (gpt-4o-mini) reject that param outright with a 400, so it must stay conditional.
    private readonly bool _isReasoningModel;

    public LabBuilderAgent(IConfiguration configuration)
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

        _isReasoningModel = deploymentName.Contains("gpt-5", StringComparison.OrdinalIgnoreCase)
            && !deploymentName.Contains("chat", StringComparison.OrdinalIgnoreCase);

        _client = string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(apiKey));
    }

    public bool IsConfigured => _client is not null;

    public async Task<LabBuilderGenerationOutcome> GenerateDraftAsync(
        GenerateLabDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_client is null || _deploymentName is null)
        {
            throw new InvalidOperationException(
                "The AI Lab Builder agent is not configured. Set AzureOpenAIEndpoint and AzureOpenAIDeploymentName.");
        }

        var agent = _client
            .GetChatClient(_deploymentName)
            .AsAIAgent(instructions: Instructions, name: "AiLabBuilderAgent");

        var prompt = BuildPrompt(request);

        // A full Lab draft (many dialogues + criteria) is a long structured output — "Medium"
        // reasoning effort avoids gpt-5-mini burning its whole output budget on hidden
        // reasoning before emitting the JSON (observed failure mode on other agents in this
        // codebase), while still giving it enough room to keep the process/skills grounded.
        ChatClientAgentRunOptions? runOptions = null;
        if (_isReasoningModel)
        {
            runOptions = new ChatClientAgentRunOptions(new ChatOptions
            {
#pragma warning disable OPENAI001 // ChatCompletionOptions.ReasoningEffortLevel is an experimental SDK member, same opt-in used by other agents in this codebase.
                RawRepresentationFactory = _ => new ChatCompletionOptions
                {
                    ReasoningEffortLevel = ChatReasoningEffortLevel.Medium
                }
#pragma warning restore OPENAI001
            });
        }

        var response = await agent.RunAsync<LabDraftGenerationResult>(prompt, options: runOptions, cancellationToken: cancellationToken);

        var usage = response.Usage;
        return new LabBuilderGenerationOutcome
        {
            Generation = response.Result,
            TokenUsage = new LabBuilderTokenUsage
            {
                InputTokens = (int)(usage?.InputTokenCount ?? 0),
                OutputTokens = (int)(usage?.OutputTokenCount ?? 0),
                CachedInputTokens = (int)(usage?.CachedInputTokenCount ?? 0),
            },
        };
    }

    private static string BuildPrompt(GenerateLabDraftRequest r)
    {
        var lines = new List<string>
        {
            string.IsNullOrWhiteSpace(r.LabName)
                ? "LAB NAME: (not provided — YOU must propose a short, clear Lab name and put it in SuggestedLabName)"
                : $"LAB NAME: {r.LabName}",
            $"TARGET ROLE: {r.TargetRole}",
            $"LAB OBJECTIVE: {r.LabObjective}",
            $"LANGUAGE: {r.LabLanguage}",
            $"DIFFICULTY: {r.Difficulty}",
            $"DURATION MINUTES: {r.DurationMinutes}",
            "DIALOGUE COUNT: exactly 1 — only the actor's opening line. The rest of the meeting is free-format, driven by the Scenario/Actors/Objectives/Skills/Criteria you generate.",
            string.Empty,
            "=== SCENARIO DESCRIPTION ===",
            string.IsNullOrWhiteSpace(r.ScenarioDescription)
                ? "(not provided — YOU must invent a realistic, specific scenario grounded in the source process below, and use it to write LabDescription/ScenarioProblemaCentral/ScenarioResultadoEsperado)"
                : $"{r.ScenarioDescription}\n\n(This was written by a human — copy it VERBATIM into LabDescription, do not rewrite/paraphrase/shorten it. Only use it as ground truth to derive ScenarioProblemaCentral, ScenarioResultadoEsperado, dialogues, and skills.)",
            string.Empty,
            "=== COMPANY CONTEXT ===",
            string.IsNullOrWhiteSpace(r.CompanyContext)
                ? "(not provided)"
                : $"{r.CompanyContext}\n\n(Ground truth about the organization behind the scenario — use it to make actors/dialogues realistic and consistent, do not contradict it.)",
            string.Empty,
            $"=== SOURCE PROCESS: {r.ProcessName} (FOLLOW THIS AS GROUND TRUTH) ===",
            r.ProcessContent,
            string.Empty,
            "=== SKILLS TO EVALUATE (every one must be covered by at least one dialogue and one criterion) ===",
        };

        lines.AddRange(r.SkillsToEvaluate.Select(s =>
        {
            var typeTag = string.Equals(s.SkillType, "SOFT", StringComparison.OrdinalIgnoreCase) ? "[SOFT SKILL]" : "[TECHNICAL SKILL]";
            return string.IsNullOrWhiteSpace(s.WhatGoodLooksLike)
                ? $"- {s.SkillName} {typeTag}: (no description provided — YOU must define what a good, observable demonstration of this skill looks like in this role/process/scenario)"
                : $"- {s.SkillName} {typeTag}: {s.WhatGoodLooksLike}";
        }));

        lines.Add(string.Empty);
        lines.Add("Generate the complete LabDraftGenerationResult now.");

        return string.Join('\n', lines);
    }
}
