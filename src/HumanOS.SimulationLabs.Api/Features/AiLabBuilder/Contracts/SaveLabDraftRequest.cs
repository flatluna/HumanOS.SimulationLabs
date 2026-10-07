namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;

using HumanOS.SimulationLabs.Api.Features.AiLabBuilder;

public sealed class SaveLabDraftRequest
{
    public string? LabName { get; set; }

    public string? LabCodigo { get; set; }

    public string? TargetRole { get; set; }

    /// <summary>One of <see cref="HumanOS.SimulationLabs.Entities.LabArquetipos"/>. Optional — falls
    /// back to CLIENT_NEGOTIATION when omitted.</summary>
    public string? Archetype { get; set; }

    /// <summary>Human-authored scenario description from the original generate-draft request,
    /// if any \u2014 preferred verbatim over the agent's own LabDescription at save time.</summary>
    public string? ScenarioDescription { get; set; }
    /// <summary>Human-authored company/organization context, if any — saved verbatim into SCN_Supuestos.</summary>
    public string? CompanyContext { get; set; }
    public string? Dominio { get; set; }

    public string? Difficulty { get; set; }

    public int? DurationMinutes { get; set; }

    public decimal? ScoreMinimo { get; set; }

    public string? Language { get; set; }

    public LabDraftContent Draft { get; set; } = null!;

    /// <summary>Cost estimate returned by generate-draft (GenerateLabDraftResponse.Cost) — echoed
    /// back here so it gets persisted onto LAB_Lab at save time instead of being lost.</summary>
    public LabBuilderCostEstimate? Cost { get; set; }
}
