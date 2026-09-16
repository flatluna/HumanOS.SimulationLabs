namespace HumanOS.SimulationLabs.Entities;

public static class TestedSkillTipo
{
    public const string Technical = "TECHNICAL";
    public const string Soft = "SOFT";

    public static readonly string[] Allowed = [Technical, Soft];
}

/// <summary>
/// A skill the AI Lab Builder identified as tested by this Lab version — first-class, queryable
/// record of TestedSkillDraft (see GenerateLabDraftResponse.cs), instead of the JSON blob
/// previously stuffed into LAB_LabVersion/LAB_Scenario's BriefOculto columns. Read by
/// AttemptEvaluationAgent (via AttemptEvaluationService) to ground per-skill coaching feedback,
/// and available for future cross-Lab skills reporting.
/// </summary>
public class LAB_TestedSkill
{
    public Guid SKL_IdSkill { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdVersion { get; set; }

    /// <summary>Optional link to the rubric criterion that grades this skill (TestedSkillDraft.RubricCriterionRef
    /// resolved to a real CRT_IdCriterion at save time) — null if the AI didn't dedicate a criterion to it.</summary>
    public Guid? RUB_IdCriterion { get; set; }

    public string SKL_Nombre { get; set; } = string.Empty;

    public string SKL_Tipo { get; set; } = TestedSkillTipo.Technical;

    public string SKL_RelevanceToRole { get; set; } = string.Empty;

    public string SKL_DemonstrationStandard { get; set; } = string.Empty;

    public string? SKL_FeedbackGuidance { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public LAB_LabVersion? LabVersion { get; set; }

    public LAB_RubricCriterion? Criterion { get; set; }
}
