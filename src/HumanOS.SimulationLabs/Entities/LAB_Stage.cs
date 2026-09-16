namespace HumanOS.SimulationLabs.Entities;

public static class StageTipoInteraccion
{
    public const string Conversation = "CONVERSATION";
    public const string Desktop = "DESKTOP";
    public const string Hybrid = "HYBRID";
    public const string Artifact = "ARTIFACT";

    public static readonly string[] Allowed = [Conversation, Desktop, Hybrid, Artifact];
}

public static class StageEstatus
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";

    public static readonly string[] Allowed = [Draft, Active, Inactive];
}

/// <summary>
/// Representa una etapa del proceso que el participante debe recorrer o completar dentro de una versión específica del Lab.
/// Reglas de Inmutabilidad:
/// 1. Las etapas de una LAB_LabVersion con estado PUBLISHED no deberán editarse directamente.
/// 2. Para cambiar etapas de un Lab publicado deberá crearse una nueva LAB_LabVersion.
/// 3. La validación de inmutabilidad se implementará posteriormente en el servicio de aplicación.
/// </summary>
public class LAB_Stage
{
    public Guid STG_IdStage { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public string STG_Codigo { get; set; } = string.Empty;

    public string STG_Nombre { get; set; } = string.Empty;

    public string STG_Descripcion { get; set; } = string.Empty;

    public int STG_Orden { get; set; }

    public string STG_TipoInteraccion { get; set; } = string.Empty;

    public bool STG_EsObligatorio { get; set; }

    public string? STG_CondicionCompletitud { get; set; }

    public int? STG_TiempoSugeridoMinutos { get; set; }

    public bool STG_PermiteOrdenFlexible { get; set; } = false;

    public string STG_Estatus { get; set; } = StageEstatus.Draft;

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación hacia LAB_LabVersion
    public LAB_LabVersion? LabVersion { get; set; }

    // Colección de objetivos asociados específicamente a esta etapa
    public ICollection<LAB_Objective> Objectives { get; set; } = new List<LAB_Objective>();

    // Colección de momentos esperados que ocurren en esta etapa
    public ICollection<LAB_ExpectedMoment> ExpectedMoments { get; set; } = new List<LAB_ExpectedMoment>();

    // Colección de artefactos entregados durante esta etapa
    public ICollection<LAB_ArtifactSubmission> ArtifactSubmissions { get; set; } = new List<LAB_ArtifactSubmission>();
}
