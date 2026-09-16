namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;

/// <summary>A skill the Lab must let the participant demonstrate. WhatGoodLooksLike is optional —
/// when omitted, the agent itself defines the observable "good" criteria from the skill name,
/// process content and scenario (see LabBuilderAgent.BuildPrompt).</summary>
public sealed class SkillToEvaluate
{
    public string SkillName { get; set; } = string.Empty;

    /// <summary>"TECHNICAL" or "SOFT". Defaults to "TECHNICAL" if omitted.</summary>
    public string SkillType { get; set; } = "TECHNICAL";

    public string? WhatGoodLooksLike { get; set; }
}

/// <summary>
/// Input received from the UX to generate a reusable Lab draft.
/// Not tied to any specific student; produces DRAFT-only content for human review.
/// </summary>
public sealed class GenerateLabDraftRequest
{
    public string LabName { get; set; } = string.Empty;

    public string TargetRole { get; set; } = string.Empty;

    public string LabObjective { get; set; } = string.Empty;

    /// <summary>One of <see cref="HumanOS.SimulationLabs.Entities.LabArquetipos"/>, e.g. "JOB_INTERVIEW".
    /// Selects the specialized instruction block the simulated actor follows. Optional — falls
    /// back to CLIENT_NEGOTIATION when omitted.</summary>
    public string? Archetype { get; set; }

    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Full process/methodology text, plain text. Either supply this directly, or
    /// supply <see cref="ProcessFileName"/> + <see cref="ProcessContentBase64"/> and the
    /// server will extract it from the uploaded PDF instead (see <see cref="LabBuilderAgent"/>).</summary>
    public string ProcessContent { get; set; } = string.Empty;

    /// <summary>Original file name of the uploaded process PDF, e.g. "onboarding-process.pdf".
    /// Required together with <see cref="ProcessContentBase64"/>; mutually exclusive with a manually
    /// typed <see cref="ProcessContent"/> (the server overwrites ProcessContent with the extracted text).</summary>
    public string? ProcessFileName { get; set; }

    /// <summary>Base64-encoded bytes of the uploaded process PDF. Same JSON+base64-body convention
    /// used elsewhere in HumanOS (see ExtractCapabilityMaterialPdfFunction) rather than multipart/form-data.</summary>
    public string? ProcessContentBase64 { get; set; }

    public List<SkillToEvaluate> SkillsToEvaluate { get; set; } = [];

    public string ScenarioDescription { get; set; } = string.Empty;

    /// <summary>Optional human-authored context about the company/organization behind the
    /// scenario (industry, size, structure, etc.) — used as ground truth for the agent, never
    /// rewritten, saved verbatim into SCN_Supuestos.</summary>
    public string? CompanyContext { get; set; }

    /// <summary>BCP-47 language tag, e.g. en-US, es-MX.</summary>
    public string LabLanguage { get; set; } = string.Empty;

    /// <summary>BEGINNER, INTERMEDIATE or ADVANCED.</summary>
    public string Difficulty { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    /// <summary>Recommended between 5 and 20.</summary>
    public int DialogueCount { get; set; }
}
