namespace HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;

/// <summary>The employee's REAL required job skills, from their JobRole in the main HumanOS
/// backend (GET /job-roles/{id}). Sent by the frontend at evaluation time so recommendations
/// point at skills this specific employee's role actually requires — not generic advice. Works
/// for ANY Lab kind (interview, negotiation, discovery call, etc.), the evaluator only ever
/// reads whichever of these lists is non-empty.</summary>
public sealed class EvaluateAttemptRequest
{
    public string? EmployeeName { get; set; }

    public string? JobRoleTitle { get; set; }

    public List<string> RequiredTechnicalSkills { get; set; } = [];

    public List<string> RequiredSoftSkills { get; set; } = [];

    public List<string> RequiredDepartmentProcesses { get; set; } = [];

    public List<string> RequiredHrProcesses { get; set; } = [];
}

public sealed class CriterionEvaluationDto
{
    public string CriterionName { get; set; } = string.Empty;

    /// <summary>1.00-10.00.</summary>
    public decimal Score { get; set; }

    public string Evidence { get; set; } = string.Empty;
}

/// <summary>One of the employee's REQUIRED job skills (from their real JobRole) that this
/// conversation showed room to improve — grounded in what actually happened, not generic.</summary>
public sealed class RecommendedSkillDto
{
    public string SkillName { get; set; } = string.Empty;

    /// <summary>TECHNICAL, SOFT, DEPARTMENT_PROCESS, or HR_PROCESS — which required-skill list
    /// this came from.</summary>
    public string SkillCategory { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}

/// <summary>One participant answer paired with the actor prompt/question that preceded it,
/// scored individually with a model/ideal answer grounded in best practices, technical
/// knowledge, and soft skills - so the employee can see exactly where each response fell short
/// and what a stronger answer would have looked like.</summary>
public sealed class TurnEvaluationDto
{
    public int QuestionTurnNumber { get; set; }

    public int AnswerTurnNumber { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    /// <summary>TECHNICAL or SOFT — whichever this specific question was really testing.</summary>
    public string SkillArea { get; set; } = string.Empty;

    /// <summary>1.00-10.00.</summary>
    public decimal Score { get; set; }

    public string Assessment { get; set; } = string.Empty;

    public string RecommendedAnswer { get; set; } = string.Empty;
}

public sealed class AttemptEvaluationResponse
{
    public Guid IdEvaluation { get; set; }

    public Guid IdAttempt { get; set; }

    public decimal ScoreFinal { get; set; }

    public string Resultado { get; set; } = string.Empty;

    public string Feedback { get; set; } = string.Empty;

    public List<string> Strengths { get; set; } = [];

    public List<string> Gaps { get; set; } = [];

    public List<RecommendedSkillDto> RecommendedSkills { get; set; } = [];

    public List<CriterionEvaluationDto> CriteriaScores { get; set; } = [];

    public List<TurnEvaluationDto> TurnEvaluations { get; set; } = [];

    public DateTimeOffset FechaCreacion { get; set; }
}
