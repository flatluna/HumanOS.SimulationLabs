using System.Text;
using System.Text.Json;
using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;
using HumanOS.SimulationLabs.Data;
using HumanOS.SimulationLabs.Entities;
using Microsoft.EntityFrameworkCore;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Services;

/// <summary>
/// Builds the Realtime "instructions" system prompt for the Studio "Probar simulación" preview
/// — the AI plays the Lab's principal simulated actor (e.g. Director Financiero), grounded in
/// the Lab's own scenario + expected moments, entirely from data already stored for that Lab.
/// Read-only: never writes an Attempt/ConversationTurn, this is a preview, not a real attempt.
/// </summary>
public static class LabSimulationPromptBuilder
{
    public sealed class SimulationContext
    {
        public string LabNombre { get; set; } = string.Empty;
        public string ObjetivoGeneral { get; set; } = string.Empty;
        public string? Arquetipo { get; set; }
        public LAB_Scenario? Scenario { get; set; }
        public LAB_SimulatedActor? Actor { get; set; }
        public List<LAB_SimulatedActor> CommitteeActors { get; set; } = [];
        public List<LAB_Stage> Stages { get; set; } = [];
        public List<LAB_ExpectedMoment> Moments { get; set; } = [];
    }

    /// <summary>The REAL employee's own profile (job role + résumé), fetched by the frontend
    /// from the main HumanOS backend (GET /job-roles/{id}, GET /people/{personId}/resume) and
    /// sent along when minting a real Attempt's voice session — so the simulated actor knows who
    /// it's actually talking to (e.g. an interviewer references the candidate's résumé; a client
    /// knows the employee's job title). Same competitive-advantage grounding already used for
    /// post-attempt evaluation via EvaluateAttemptRequest, now also applied live during the call.</summary>
    public sealed class EmployeeProfileContext
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

        public bool HasAnyData =>
            !string.IsNullOrWhiteSpace(EmployeeName) ||
            !string.IsNullOrWhiteSpace(CompanyName) ||
            !string.IsNullOrWhiteSpace(JobRoleTitle) ||
            !string.IsNullOrWhiteSpace(ResumeSummary) ||
            RequiredTechnicalSkills.Count > 0 ||
            RequiredSoftSkills.Count > 0;
    }

    public static async Task<SimulationContext?> LoadAsync(
        SimulationLabsDbContext db, Guid tenantId, Guid idLab, CancellationToken cancellationToken)
    {
        var lab = await db.Labs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == idLab, cancellationToken);
        if (lab is null) return null;

        var version = await db.LabVersions.AsNoTracking()
            .Where(v => v.SEG_IdTenant == tenantId && v.LAB_IdLab == idLab)
            .OrderByDescending(v => v.LAB_NumeroVersion)
            .FirstOrDefaultAsync(cancellationToken);
        if (version is null) return null;

        var scenario = await db.Scenarios.AsNoTracking()
            .Where(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == version.LAB_IdVersion)
            .OrderBy(s => s.FechaCreacion)
            .FirstOrDefaultAsync(cancellationToken);

        LAB_SimulatedActor? actor = null;
        var committeeActors = new List<LAB_SimulatedActor>();
        if (scenario is not null)
        {
            var actorsQuery = db.SimulatedActors.AsNoTracking()
                .Where(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenario.SCN_IdScenario)
                .OrderByDescending(a => a.ACT_EsPrincipal)
                .ThenBy(a => a.ACT_Orden);
            actor = await actorsQuery.FirstOrDefaultAsync(cancellationToken);
            if (string.Equals(lab.LAB_Arquetipo, LabArquetipos.AcademicDefense, StringComparison.OrdinalIgnoreCase))
            {
                committeeActors = await actorsQuery.OrderBy(a => a.ACT_Orden).Take(3).ToListAsync(cancellationToken);
            }
        }

        // Stages are the real "Casos" (STG_Orden is the actual sequence, e.g. Caso 1, Caso 2) —
        // loaded so moments can be grouped/presented per case instead of one flat, undifferentiated list.
        var stages = await db.Stages.AsNoTracking()
            .Where(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == version.LAB_IdVersion)
            .OrderBy(s => s.STG_Orden)
            .ToListAsync(cancellationToken);

        var moments = await db.ExpectedMoments.AsNoTracking()
            .Where(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == version.LAB_IdVersion)
            .OrderBy(m => m.MOM_OrdenSugerido)
            .ToListAsync(cancellationToken);

        return new SimulationContext
        {
            LabNombre = lab.LAB_Nombre,
            ObjetivoGeneral = version.LAB_ObjetivoGeneral,
            Arquetipo = lab.LAB_Arquetipo,
            Scenario = scenario,
            Actor = actor,
            CommitteeActors = committeeActors,
            Stages = stages,
            Moments = moments
        };
    }

    /// <summary>Same as <see cref="LoadAsync"/> but for a REAL Attempt, which already pins an
    /// exact LAB_IdVersion + SCN_IdScenario (no "latest version"/"first scenario" guessing).</summary>
    public static async Task<SimulationContext?> LoadForAttemptAsync(
        SimulationLabsDbContext db, Guid tenantId, Guid idVersion, Guid idScenario, CancellationToken cancellationToken)
    {
        var version = await db.LabVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.SEG_IdTenant == tenantId && v.LAB_IdVersion == idVersion, cancellationToken);
        if (version is null) return null;

        var lab = await db.Labs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.SEG_IdTenant == tenantId && l.LAB_IdLab == version.LAB_IdLab, cancellationToken);
        if (lab is null) return null;

        var scenario = await db.Scenarios.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SEG_IdTenant == tenantId && s.SCN_IdScenario == idScenario, cancellationToken);

        LAB_SimulatedActor? actor = null;
        var committeeActors = new List<LAB_SimulatedActor>();
        if (scenario is not null)
        {
            var actorsQuery = db.SimulatedActors.AsNoTracking()
                .Where(a => a.SEG_IdTenant == tenantId && a.SCN_IdScenario == scenario.SCN_IdScenario)
                .OrderByDescending(a => a.ACT_EsPrincipal)
                .ThenBy(a => a.ACT_Orden);
            actor = await actorsQuery.FirstOrDefaultAsync(cancellationToken);
            if (string.Equals(lab.LAB_Arquetipo, LabArquetipos.AcademicDefense, StringComparison.OrdinalIgnoreCase))
            {
                committeeActors = await actorsQuery.OrderBy(a => a.ACT_Orden).Take(3).ToListAsync(cancellationToken);
            }
        }

        var stages = await db.Stages.AsNoTracking()
            .Where(s => s.SEG_IdTenant == tenantId && s.LAB_IdVersion == version.LAB_IdVersion)
            .OrderBy(s => s.STG_Orden)
            .ToListAsync(cancellationToken);

        var moments = await db.ExpectedMoments.AsNoTracking()
            .Where(m => m.SEG_IdTenant == tenantId && m.LAB_IdVersion == version.LAB_IdVersion)
            .OrderBy(m => m.MOM_OrdenSugerido)
            .ToListAsync(cancellationToken);

        return new SimulationContext
        {
            LabNombre = lab.LAB_Nombre,
            ObjetivoGeneral = version.LAB_ObjetivoGeneral,
            Arquetipo = lab.LAB_Arquetipo,
            Scenario = scenario,
            Actor = actor,
            CommitteeActors = committeeActors,
            Stages = stages,
            Moments = moments
        };
    }

    public static string BuildInstructions(SimulationContext context, string? adminDisplayName, bool isRealAttempt = false, EmployeeProfileContext? employeeProfile = null, LAB_SimulatedActor? actorOverride = null, bool isFirstCommitteeSpeaker = true, string? priorConversationTranscript = null, bool isIntroductionOnlyTurn = false)
    {
        var sb = new StringBuilder();
        var actor = actorOverride ?? context.Actor;

        // LANGUAGE HARD RULE — FIXED (2026-09-15, "agent speaks Spanish with an English accent"):
        // every other working Realtime agent in this codebase (VoiceTutorSessionFunction.BuildInstructions,
        // EngramReviewSessionFunction) writes its language directive AS THE FIRST THING IN THE PROMPT and,
        // for Spanish, phrases the rule itself IN SPANISH — the model's accent visibly entrains on the
        // language its instructions are written in, not just on a single English meta-sentence buried
        // among dozens of other English rules ("ALWAYS speak in Español, native accent"). That weak,
        // English-authored rule was the actual root cause of the English-accented Spanish. Detect the
        // actor's language and lead with a same-language hard rule instead.
        var actorLanguage = actor?.ACT_Idioma;
        var isSpanish = actorLanguage is { Length: > 0 }
            && (actorLanguage.Contains("esp", StringComparison.OrdinalIgnoreCase) || actorLanguage.Equals("es", StringComparison.OrdinalIgnoreCase));
        sb.AppendLine(isSpanish
            ? "REGLA DE IDIOMA — NUNCA LA ROMPAS: habla SIEMPRE en español, con acento nativo de español (nunca acento en inglés " +
              "ni ningún otro), en cada turno, desde el primer segundo de la llamada hasta el último. Nunca cambies de idioma o " +
              "acento aunque el estudiante lo haga."
            : $"LANGUAGE — HARD RULE, NEVER BREAK IT: speak ONLY in {actorLanguage ?? "English"}, at a natural pace, native accent, " +
              "every single turn, from the first second of the call to the last. Never switch language or accent even if the student does.");
        sb.AppendLine();

        // Simple, labeled-field structure (per explicit product decision) instead of a long prose
        // rule list — easier for the model to follow without confusing who plays which role.
        // Prompt is authored in English regardless of the actor's spoken language (ACT_Idioma
        // controls only the language the actor SPEAKS, not the instruction language).
        sb.AppendLine("CRITICAL: YOU SPEAK FIRST. The instant this call connects, before the student has said a single word,");
        sb.AppendLine("you must start talking — greet them and open with your first topic/ask. Never wait in silence for the");
        sb.AppendLine("student to speak first; you are the one leading and driving this conversation from turn one.");
        sb.AppendLine();
        var archetype = LabArchetypes.Resolve(context.Arquetipo);
        var isAcademicDefense = string.Equals(context.Arquetipo, LabArquetipos.AcademicDefense, StringComparison.OrdinalIgnoreCase);
        sb.AppendLine(archetype.InstructionBlock);
        sb.AppendLine();
        sb.AppendLine("CRITICAL: WHO GIVES WHAT, NEVER CHANGES. Read this fact and hold onto it for the ENTIRE call, every single turn:");
        sb.AppendLine($"  - \"I\" / \"we\" / \"us\" = YOU, \"{actor?.ACT_Nombre ?? "the character"}\". \"You\"/\"your\" = the STUDENT. Never swap these.");
        sb.AppendLine("  - Only the STUDENT can offer discounts, free extras, special terms, or any other concession. YOU never offer, propose, or hand out ANY of those things — not even as a suggestion, not even hypothetically. YOU only ask for what you want, then accept / reject / counter-ask when the student proposes something.");
        sb.AppendLine("  - Before you speak, silently check: \"am I about to offer, give, or propose a concession?\" If yes, STOP — that line belongs to the student, not you. Ask for what YOU want instead, or react to what THEY already offered.");
        sb.AppendLine("  - WHOEVER ASKED FOR SOMETHING FIRST KEEPS ASKING FOR IT. If YOU are the one who requested a discount/concession/favor at the start of the call, YOU remain the one requesting it for the rest of the call — never flip into asking the STUDENT to justify wanting it (that request was yours, not theirs). Before each turn, silently check: \"who originally asked for this thing we're discussing — me or the student?\" and stay on that side.");
        sb.AppendLine();
        sb.AppendLine("Your role: You are an AI playing a character in a simulated practice conversation, evaluating a student.");
        sb.AppendLine($"Your character: \"{actor?.ACT_Nombre ?? "the client"}\". Title: \"{actor?.ACT_Rol ?? "-"}\".");
        if (context.Scenario is not null)
        {
            sb.AppendLine($"What this exam evaluates: {context.Scenario.SCN_ResultadoEsperado}");
        }
        sb.AppendLine();
        sb.AppendLine("IMPORTANT:");
        sb.AppendLine($"- Never take the student's role. You are \"{actor?.ACT_Nombre ?? "the character"}\"; the student is the other person on the call.");
        sb.AppendLine("- This is a free-flowing, natural conversation, not a scripted questionnaire. Don't recite fixed lines — react like a real person would, in your own words, to whatever the student actually says or asks.");
        sb.AppendLine("- The GOALS/TOPICS list below are things you need the student to address by the end of the call — not a script and not a fixed order. Bring them up naturally whenever they fit the conversation's flow, ask as many follow-up questions as a real person in your position would need to get a genuinely good answer, and skip/reorder freely based on how the student is steering things.");
        sb.AppendLine("- NEVER RE-ASK A TOPIC YOU'VE ALREADY COVERED. Before speaking, mentally review the ENTIRE conversation so far: if the student has already substantively answered a topic (even briefly, even earlier in the call), do NOT circle back and ask it again in different words — move to a topic that is still genuinely uncovered. Repeating yourself wastes the student's time and is exactly what a real interviewer/counterpart would never do.");
        sb.AppendLine("- ONLY your very first turn is fixed (topic 1 below, near-verbatim). Every turn after that must be a NEW question you generate yourself in the moment, reacting to what the student just said — never mechanically read down the TOPICS list one by one. Treat the TOPICS list as a minimum checklist of ground to cover, not an exhaustive script: once you've cycled through it, keep going by drawing fresh, specific follow-up questions from ALL the rich context you were given (your character's knowledge, hidden brief, objections, contradictions, the scenario description) — there is always more real substance to probe than the fixed list alone.");
        sb.AppendLine("- Say ONE short thing per turn, then wait for the student's real response before continuing.");
        sb.AppendLine("- If the student asks or comments something, respond to that first — never ignore it to jump to your own agenda.");
        sb.AppendLine("- BREADTH OVER DEPTH: never stay on the same topic/question thread indefinitely, even if the student's answer invites");
        sb.AppendLine("  more follow-up. Ask at most 1-2 follow-ups on any single point, then move to a genuinely different topic/skill area —");
        sb.AppendLine("  you must cover a MIX of different skills across the conversation (e.g. both TECHNICAL and SOFT skills from the list");
        sb.AppendLine("  below, not just one type), not exhaustively drill one question forever. Treat the remaining time as a budget across");
        sb.AppendLine("  ALL topics, not just the current one.");
        sb.AppendLine("- Be demanding but reasonable: if an answer is empty or evasive, push back or ask for more detail; once they give something substantial, accept it and move on. If their behavior is unprofessional enough that the meeting no longer makes sense, you may end the call early instead of forcing every topic.");
        sb.AppendLine("- This exam evaluates the STUDENT's skill (selling, negotiating, interviewing, giving feedback, diagnosing, running discovery, etc.), never yours. Never do that task for them, and never turn their question/task back onto them — that flips the roles and breaks the exam. You only react in character to what they do or say, driven by your own goals/context below.");
        sb.AppendLine();

        if (actor is not null)
        {
            var a = actor;
            sb.AppendLine($"YOUR CHARACTER: {a.ACT_Nombre} — {a.ACT_Rol}");
            sb.AppendLine($"Description: {a.ACT_Descripcion}");
            sb.AppendLine($"Your goal in this conversation: {a.ACT_Objetivo}");
            sb.AppendLine($"What you already know/context: {a.ACT_ContextoConocido}");
            if (!string.IsNullOrWhiteSpace(a.ACT_BriefOculto))
                sb.AppendLine($"Information only you know (hidden brief — don't reveal it unless asked directly): {a.ACT_BriefOculto}");
            if (!string.IsNullOrWhiteSpace(a.ACT_InformacionNoRevelarAutomaticamente))
                sb.AppendLine($"NEVER reveal this automatically, only if asked explicitly: {a.ACT_InformacionNoRevelarAutomaticamente}");
            if (!string.IsNullOrWhiteSpace(a.ACT_Objeciones))
                sb.AppendLine($"Objections you can raise: {a.ACT_Objeciones}");
            if (!string.IsNullOrWhiteSpace(a.ACT_Contradicciones))
                sb.AppendLine($"Contradictions you can introduce if relevant: {a.ACT_Contradicciones}");
            sb.AppendLine($"Communication style: {a.ACT_EstiloComunicacion}. Knowledge level: {a.ACT_NivelConocimiento}.");
        }

        if (context.Scenario is not null)
        {
            var s = context.Scenario;
            sb.AppendLine();
            sb.AppendLine($"SCENARIO: {s.SCN_Nombre}");
            if (!string.IsNullOrWhiteSpace(s.SCN_Descripcion))
                sb.AppendLine($"Meeting/situation description: {s.SCN_Descripcion}");
            sb.AppendLine($"Context for the participant: {s.SCN_ContextoParticipante}");
            sb.AppendLine($"Core problem: {s.SCN_ProblemaCentral}");
            if (!string.IsNullOrWhiteSpace(s.SCN_BriefOculto))
            {
                var skills = TryParseTestedSkills(s.SCN_BriefOculto);
                if (skills is { Count: > 0 })
                {
                    sb.AppendLine("SKILLS TO EVALUATE (internal grading reference — never read aloud, never mention to the student):");
                    foreach (var skill in skills)
                    {
                        sb.AppendLine($"  - {skill.SkillName} ({skill.SkillType}): good demonstration = {skill.DemonstrationStandard}");
                    }
                }
                else
                {
                    sb.AppendLine($"Scenario's hidden brief: {s.SCN_BriefOculto}");
                }
            }
            if (!string.IsNullOrWhiteSpace(s.SCN_InformacionNoRevelarAutomaticamente))
                sb.AppendLine($"Don't reveal automatically: {s.SCN_InformacionNoRevelarAutomaticamente}");
        }

        if (context.Stages.Count > 0 && context.Moments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("GOALS/TOPICS FOR THE STUDENT TO COVER (internal checklist — never read aloud, never treat as a script):");
            sb.AppendLine("Topics tagged [NEGOTIATION] are hard asks (discounts, deadlines): if the student offers something concrete and reasonable,");
            sb.AppendLine("accept it and move on; if they dodge with nothing concrete, push back up to 3 times, varying your wording; if they still");
            sb.AppendLine("won't budge after that, end the call in character instead of forcing the rest of the topics.");
            var orderedStageIds = context.Stages.OrderBy(s => s.STG_Orden).Select(s => s.STG_IdStage).ToList();
            var stageRank = orderedStageIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
            var flatMoments = context.Moments
                .OrderBy(m => stageRank.TryGetValue(m.STG_IdStage, out var rank) ? rank : int.MaxValue)
                .ThenBy(m => m.MOM_OrdenSugerido)
                .ToList();
            var pointIndex = 1;
            foreach (var m in flatMoments)
            {
                // MOM_RespuestaEjemplar and MOM_IntencionEsperada stay excluded — only the topic itself
                // is given, so the model asks about it in its own words instead of reciting/leaking anything.
                var tag = m.MOM_EsCritico ? " [NEGOTIATION]" : "";
                sb.AppendLine($"  {pointIndex}.{tag} Topic: \"{m.MOM_Trigger}\"");
                pointIndex++;
            }
        }

        var isResumeAwareArchetype = string.Equals(context.Arquetipo, "JOB_INTERVIEW", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Arquetipo, "CAREER_MENTORING", StringComparison.OrdinalIgnoreCase);

        sb.AppendLine();
        if (isRealAttempt)
        {
            var realName = !string.IsNullOrWhiteSpace(employeeProfile?.EmployeeName) ? employeeProfile!.EmployeeName : adminDisplayName;
            sb.AppendLine(isAcademicDefense
                ? $"You're talking with {(string.IsNullOrWhiteSpace(realName) ? "the candidate" : realName)}, a REAL Tecnológico de Monterrey " +
                  "student defending her actual professional thesis for real — she is a STUDENT, never a HumanOS employee/company staff " +
                  "member. Keep the conversation natural and in character the whole time."
                : $"You're talking with {(string.IsNullOrWhiteSpace(realName) ? "the participant" : realName)}, " +
                  "a real employee taking this exam for real. Keep the conversation natural and in character the whole time.");
            if (isResumeAwareArchetype && employeeProfile is { HasAnyData: true })
            {
                sb.AppendLine();
                sb.AppendLine("BEFORE THE CALL — MANDATORY SILENT PREP (never speak these steps out loud, never mention doing this):");
                sb.AppendLine("  1. READ THEIR RÉSUMÉ below in full — real past employers, projects, achievements, skills.");
                sb.AppendLine("  2. READ THE JOB DESCRIPTION/ROLE SUMMARY below — what this role actually requires day to day.");
                sb.AppendLine("  3. READ THE REQUIRED TECHNICAL AND SOFT SKILLS below — this is what you must probe for during the interview.");
                sb.AppendLine("You are interviewing this specific candidate for this specific role — every question you ask for the rest of");
                sb.AppendLine("the call must be informed by these three things, even if the TOPICS checklist elsewhere in this prompt doesn't");
                sb.AppendLine("explicitly say to reference the résumé/role — ground your questions in their real background and this role's");
                sb.AppendLine("real requirements regardless.");
            }
            if (!isAcademicDefense && employeeProfile is { HasAnyData: true })
            {
                // A student (Tecnológico de Monterrey demo) has an academic profile but usually no
                // real company/job role — never default to "at their real job" framing for them.
                var isStudentProfile = !string.IsNullOrWhiteSpace(employeeProfile.AcademicProfileSummary);
                sb.AppendLine();
                if (isStudentProfile)
                {
                    sb.AppendLine("ABOUT THE REAL PERSON YOU'RE TALKING TO — they are a real STUDENT at Tecnológico de Monterrey practicing this");
                    sb.AppendLine("business case as a course exercise, NOT a HumanOS employee and NOT actually employed at any company mentioned in");
                    sb.AppendLine("this scenario. Treat the scenario's company/role as a role-play backdrop for their learning, not their real job —");
                    sb.AppendLine("never imply they actually work there. Use their real academic background to make the conversation specific and");
                    sb.AppendLine("grounded (greet them by name, reference their real degree/academic record naturally) — never recite this list back");
                    sb.AppendLine("to them like a script:");
                }
                else
                {
                    sb.AppendLine("ABOUT THE REAL PERSON YOU'RE TALKING TO — this is who they actually are at their real job. Use it to make");
                    sb.AppendLine("the conversation specific and grounded (e.g. greet them by name, reference their actual company/role naturally,");
                    sb.AppendLine("the way someone who genuinely knew this about them would) — never recite this list back to them like a script:");
                }
                if (!string.IsNullOrWhiteSpace(employeeProfile.EmployeeName))
                    sb.AppendLine($"  - Their real name: {employeeProfile.EmployeeName}");
                if (isStudentProfile)
                {
                    sb.AppendLine($"  - Their real academic record (Tecnológico de Monterrey): {employeeProfile.AcademicProfileSummary}");
                    if (!string.IsNullOrWhiteSpace(employeeProfile.ProfessionalObjective))
                        sb.AppendLine($"  - Their professional objective: {employeeProfile.ProfessionalObjective}");
                }
                // CompanyName is just the platform tenant name (e.g. "HumanOS" itself) — never a real
                // employer for a student, so skip it (and job role/skills, which don't apply either)
                // entirely for student profiles instead of accidentally implying they work there.
                if (!isStudentProfile)
                {
                    if (!string.IsNullOrWhiteSpace(employeeProfile.CompanyName))
                        sb.AppendLine($"  - The real company they work for: {employeeProfile.CompanyName}");
                    if (!string.IsNullOrWhiteSpace(employeeProfile.JobRoleTitle))
                    {
                        sb.AppendLine($"  - Their real job role: {employeeProfile.JobRoleTitle}" +
                                       (string.IsNullOrWhiteSpace(employeeProfile.JobRoleSummary) ? "" : $" — {employeeProfile.JobRoleSummary}"));
                    }
                    if (employeeProfile.RequiredTechnicalSkills.Count > 0)
                        sb.AppendLine($"  - Their role's technical skills: {string.Join(", ", employeeProfile.RequiredTechnicalSkills)}");
                    if (employeeProfile.RequiredSoftSkills.Count > 0)
                        sb.AppendLine($"  - Their role's soft skills: {string.Join(", ", employeeProfile.RequiredSoftSkills)}");
                }
                if (!string.IsNullOrWhiteSpace(employeeProfile.ResumeSummary))
                    sb.AppendLine($"  - Their résumé/professional background: {employeeProfile.ResumeSummary}");
                sb.AppendLine("  - Only reference details your character would realistically know (e.g. an interviewer or mentor may know");
                sb.AppendLine("    their résumé; a client/prospect would only know their name, company and job title, not their résumé). If your");
                sb.AppendLine("    character wouldn't plausibly know a given detail, let it silently inform your judgment instead of mentioning it.");
                if (!isStudentProfile && (!string.IsNullOrWhiteSpace(employeeProfile.CompanyName) || !string.IsNullOrWhiteSpace(employeeProfile.JobRoleTitle)))
                {
                    sb.AppendLine();
                    sb.AppendLine("CRITICAL — PROVE YOU KNOW WHO THEY ARE, DON'T STAY VAGUE: at some point in your first 1-2 turns, explicitly and");
                    sb.AppendLine("specifically name their real company and/or real job title as given above (e.g. \"como Software Engineer en " +
                                   $"{(string.IsNullOrWhiteSpace(employeeProfile.CompanyName) ? "tu empresa" : employeeProfile.CompanyName)}\"" +
                                   " or similar, adapted naturally to your character's voice and the conversation's language). NEVER stay generic");
                    sb.AppendLine("(\"tu entorno corporativo\", \"tu situación laboral\", \"your company\", \"your role\") when you actually have their");
                    sb.AppendLine("real company/job title above — a generic answer here is a FAILURE, it means you're not actually using this context.");
                    sb.AppendLine("Say the specific company name and/or job title OUT LOUD, in your own words, early in the call.");
                }
                if (isResumeAwareArchetype && !string.IsNullOrWhiteSpace(employeeProfile.ResumeSummary))
                {
                    sb.AppendLine();
                    sb.AppendLine("CRITICAL — YOU HAVE ALREADY READ THEIR RÉSUMÉ: as the interviewer/mentor in this call, you received and reviewed");
                    sb.AppendLine("this person's actual résumé before the call started — it's given above. You MUST reference specific, concrete details");
                    sb.AppendLine("from it (a real past employer, a real skill, a real project or achievement) at least once, ideally to open your first");
                    sb.AppendLine("substantive question (e.g. \"I saw you worked at X doing Y — tell me about...\"). NEVER say you don't have access to");
                    sb.AppendLine("their résumé/background/profile, and NEVER ask them to summarize their own background from scratch as if you knew");
                    sb.AppendLine("nothing — that would be a FAILURE, since you already have it in front of you.");
                }
            }
            if (isAcademicDefense)
            {
                sb.AppendLine();
                sb.AppendLine("ACADEMIC DEFENSE CONTEXT — use this only for the Tec de Monterrey professional thesis exam:");
                sb.AppendLine("  - The thesis and process description are stored in the Lab scenario below. Treat that stored process description as the authoritative thesis source.");
                sb.AppendLine("  - Do not invent thesis facts. If the stored description does not contain a detail, ask the student to clarify it instead of guessing.");
                if (!string.IsNullOrWhiteSpace(employeeProfile?.ProfessionalObjective))
                    sb.AppendLine($"  - Student professional objective: {employeeProfile.ProfessionalObjective}");
                if (!string.IsNullOrWhiteSpace(employeeProfile?.AcademicProfileSummary))
                    sb.AppendLine($"  - Student academic record: {employeeProfile.AcademicProfileSummary}");
                if (!string.IsNullOrWhiteSpace(employeeProfile?.ResumeSummary))
                    sb.AppendLine($"  - Student résumé context: {employeeProfile.ResumeSummary}");
                sb.AppendLine("  - Use academic record and résumé only to make relevant application questions; never treat them as evidence that replaces the thesis.");
            }
            if (!string.IsNullOrWhiteSpace(employeeProfile?.EmployeeName))
            {
                var firstName = employeeProfile!.EmployeeName!.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? employeeProfile.EmployeeName!;
                sb.AppendLine();
                sb.AppendLine($"CRITICAL — ADDRESS THEM BY NAME: their real name is \"{employeeProfile.EmployeeName}\". You MUST use their first name");
                sb.AppendLine($"(\"{firstName}\") in your very first turn/greeting, so they immediately know you have their real context and this isn't");
                sb.AppendLine($"a generic call. After that, naturally say \"{firstName}\" again a few more times over the course of the conversation");
                sb.AppendLine("(e.g. when greeting, transitioning topics, or making a point) — the way a real person genuinely talking to them would,");
                sb.AppendLine("not on every single turn (that would sound robotic/scripted). Never call them by the wrong name or a generic term");
                sb.AppendLine("like \"sir\"/\"ma'am\"/\"candidate\" instead of their real name.");
            }
        }
        else
        {
            sb.AppendLine($"You're talking with {(string.IsNullOrWhiteSpace(adminDisplayName) ? "a Studio administrator" : adminDisplayName)} " +
                           "who is testing the Lab, not a real participant. Keep the conversation natural and in character the whole time.");
        }
        sb.AppendLine();
        sb.AppendLine($"REMINDER before you speak: you are \"{actor?.ACT_Nombre ?? "the character"}\". You only ask for things; the STUDENT is the only one who can offer discounts/concessions/extras. Never say a line that gives the student something — that line is theirs to say, not yours.");
        if (isAcademicDefense)
        {
            sb.AppendLine("ROLE LOCK — ACADEMIC DEFENSE: you are the sinodales, the examining judges. The student is Sofia, the candidate.");
            sb.AppendLine("Never speak as Sofia. Never say that you are ready to defend a thesis, that you will present your thesis, or any other first-person candidate statement.");
            sb.AppendLine("Never read or imitate a scenario line written for the candidate. Convert candidate instructions into questions asked by the committee.");
        }
        sb.AppendLine();
        var useInterviewOpening = isRealAttempt && isResumeAwareArchetype && employeeProfile is { HasAnyData: true }
            && (!string.IsNullOrWhiteSpace(employeeProfile.ResumeSummary) || !string.IsNullOrWhiteSpace(employeeProfile.JobRoleSummary));
        if (isAcademicDefense)
        {
            if (isIntroductionOnlyTurn && isFirstCommitteeSpeaker)
            {
                sb.AppendLine("YOUR FIRST TURN: greet Sofia in ONE short sentence as the president of the examining committee — say only your name and role,");
                sb.AppendLine("then one brief sentence that the other sinodales will introduce themselves next. Then stop talking.");
            }
            else if (isIntroductionOnlyTurn)
            {
                sb.AppendLine("YOUR FIRST TURN: introduce yourself to Sofia in ONE short sentence — your name and role, nothing else. Then stop talking.");
                if (!string.IsNullOrWhiteSpace(priorConversationTranscript))
                {
                    sb.AppendLine();
                    sb.AppendLine("CONVERSATION SO FAR (already happened before you joined — read it so you don't repeat a name/role already said):");
                    sb.AppendLine(priorConversationTranscript);
                }
            }
            else if (isFirstCommitteeSpeaker)
            {
                sb.AppendLine("YOUR FIRST TURN: the whole committee has already introduced itself by name in prior turns — do NOT introduce anyone again.");
                sb.AppendLine("Say a brief transition (e.g. 'Bien, comencemos') and ask Sofia to begin by stating the title, central problem, objective, and main contribution of her thesis.");
                sb.AppendLine("Do not say that you are ready to defend a thesis. Sofia is the person who defends; you are the judges who ask and evaluate.");
                sb.AppendLine("Ignore any first-turn topic text that is written in the candidate's voice, such as 'estoy lista para defender mi tesis'.");
                if (!string.IsNullOrWhiteSpace(priorConversationTranscript))
                {
                    sb.AppendLine();
                    sb.AppendLine("CONVERSATION SO FAR (the committee's introductions — read it so you don't repeat any name/role):");
                    sb.AppendLine(priorConversationTranscript);
                }
            }
            else
            {
                sb.AppendLine("YOUR FIRST TURN — YOU ARE JOINING AN ALREADY-STARTED DEFENSE: the committee has already greeted Sofia and other sinodales");
                sb.AppendLine("have already spoken. Do NOT greet her again, do NOT reintroduce the committee, and do NOT repeat the opening request to");
                sb.AppendLine("state the thesis title/problem/objective. Speak as if you were listening the whole time — briefly acknowledge the topic");
                sb.AppendLine("only if natural, then go straight into your OWN first question from your area of expertise.");
                if (!string.IsNullOrWhiteSpace(priorConversationTranscript))
                {
                    sb.AppendLine();
                    sb.AppendLine("CONVERSATION SO FAR (already happened before you joined this call — read it carefully):");
                    sb.AppendLine(priorConversationTranscript);
                    sb.AppendLine("Never repeat a question another sinodal already asked. Never ask Sofia to repeat something she already answered above.");
                    sb.AppendLine("Build your own question on what she has already said, as a real committee member who was listening would.");
                }
            }
        }
        else if (useInterviewOpening)
        {
            sb.AppendLine("YOUR FIRST TURN (OVERRIDES THE GENERIC RULE BELOW): warmly greet the candidate by name, briefly confirm they're");
            sb.AppendLine("ready to start (e.g. ask if they're ready and, if the language wasn't already obvious, which language they'd");
            sb.AppendLine("prefer — English or Spanish) — then move directly into your first substantive question, grounded in something");
            sb.AppendLine("concrete from their résumé and/or this role's requirements above (a real past employer, project, or skill). Do");
            sb.AppendLine("NOT just recite topic 1 from the checklist verbatim as your opener — use it only as a guide for subject matter,");
            sb.AppendLine("your actual opening question must reference their real background even if the checklist text doesn't say to.");
        }
        else if (context.Moments.Count > 0)
        {
            sb.AppendLine("YOUR FIRST TURN: start by stating your name and role/title in a short natural phrase (e.g. \"Soy " +
                $"{(actor?.ACT_Nombre ?? "...")}, {(actor?.ACT_Rol ?? "...")}\" adapted to your character's voice), then immediately");
            sb.AppendLine("say topic 1 above almost verbatim (adapt only tone, not content/intent) as the rest of your opening");
            sb.AppendLine("line — together that IS your whole greeting and ask/problem statement, don't add another separate greeting");
            sb.AppendLine("before or after it. State YOUR position/ask directly. Never turn it into a question asking the student to explain");
            sb.AppendLine("or justify their own goals/motives first — you already know what you want, you're the one asking for it.");
            sb.AppendLine("If topic 1's text itself describes exam logistics (question counts, categories, time limits) instead of real");
            sb.AppendLine("in-character speech, ignore that meta-text and just open naturally in character based on your context above —");
            sb.AppendLine("never say anything like \"we'll ask about N questions\" out loud, a real person in this situation never would.");
        }
        else
        {
            sb.AppendLine("YOUR FIRST TURN: greet briefly in character, starting by stating your name and role/title (e.g. \"Soy " +
                $"{(actor?.ACT_Nombre ?? "...")}, {(actor?.ACT_Rol ?? "...")}\" adapted to your character's voice), then bring up the first");
            sb.AppendLine("thing on your mind from your own goals/context above, in your own words. State YOUR position directly, don't ask");
            sb.AppendLine("the student to explain or justify their own goals/motives first.");
        }

        return sb.ToString();
    }

    /// <summary>SCN_BriefOculto stores either the AI Lab Builder's serialized List&lt;TestedSkillDraft&gt;
    /// JSON, or (for manually-created scenarios) a plain free-text brief — try the structured form
    /// first and fall back to null so the caller can render the raw text instead.</summary>
    private static List<TestedSkillDraft>? TryParseTestedSkills(string briefOculto)
    {
        try
        {
            return JsonSerializer.Deserialize<List<TestedSkillDraft>>(briefOculto, ApiJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions ApiJsonOptions = new(JsonSerializerDefaults.Web);
}
