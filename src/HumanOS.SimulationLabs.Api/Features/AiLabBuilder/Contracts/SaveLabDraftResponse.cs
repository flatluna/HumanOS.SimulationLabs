namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;

public sealed class SaveLabDraftResponse
{
    public Guid IdLab { get; set; }

    public Guid IdVersion { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Estatus { get; set; } = string.Empty;

    public int StagesSaved { get; set; }

    public int ObjectivesSaved { get; set; }

    public int MomentsSaved { get; set; }

    public int CriteriaSaved { get; set; }

    public int ActorsSaved { get; set; }

    public int SkillsRegistered { get; set; }

    public string Message { get; set; } = string.Empty;
}
