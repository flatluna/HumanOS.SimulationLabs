namespace HumanOS.SimulationLabs.Entities;

/// <summary>
/// Result of the post-conversation evaluation pass over a completed LAB_Attempt — works for
/// ANY Lab kind (interview, negotiation, discovery call, feedback conversation, etc.), not just
/// interviews. Grounded in the full transcript (LAB_ConversationTurn), the Lab's own rubric
/// criteria, AND the employee's real required job skills (technical/soft/department/HR
/// processes) so recommendations point at the specific skills this employee's role actually
/// requires, not generic advice.
/// </summary>
public class LAB_AttemptEvaluation
{
    public Guid EVL_IdEvaluation { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid ATT_IdAttempt { get; set; }

    public decimal EVL_ScoreFinal { get; set; }

    /// <summary>PASSED, PARTIAL, REPEAT_RECOMMENDED, NOT_COMPLETED, CRITICAL_FAILURE — same
    /// vocabulary as AttemptResultado, kept in sync onto LAB_Attempt.ATT_Resultado too.</summary>
    public string EVL_Resultado { get; set; } = string.Empty;

    /// <summary>2-4 sentence narrative summary for the employee, in the same language as the
    /// conversation.</summary>
    public string EVL_Feedback { get; set; } = string.Empty;

    /// <summary>JSON string[] — top demonstrated strengths.</summary>
    public string EVL_StrengthsJson { get; set; } = "[]";

    /// <summary>JSON string[] — top gaps/areas needing improvement.</summary>
    public string EVL_GapsJson { get; set; } = "[]";

    /// <summary>JSON-serialized list of RecommendedSkillDto — the required job skills (from the
    /// employee's real JobRole) this conversation showed room to improve, each with a short
    /// reason grounded in what actually happened in the call.</summary>
    public string EVL_RecommendedSkillsJson { get; set; } = "[]";

    /// <summary>JSON-serialized list of per-criterion scores (CriterionEvaluationDto), one per
    /// LAB_RubricCriterion evaluated.</summary>
    public string EVL_CriteriaScoresJson { get; set; } = "[]";

    /// <summary>JSON-serialized list of TurnEvaluationDto - one per substantive question/answer
    /// exchange in the transcript, each with its own score, assessment, and a recommended
    /// best-practice model answer.</summary>
    public string EVL_TurnEvaluationsJson { get; set; } = "[]";

    public string? EVL_GeneratedModel { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;
}
