namespace HumanOS.SimulationLabs.Entities;

public static class ObjectiveTipoEvidencia
{
    public const string Conversation = "CONVERSATION";
    public const string UserAction = "USER_ACTION";
    public const string Artifact = "ARTIFACT";
    public const string Decision = "DECISION";
    public const string SystemResult = "SYSTEM_RESULT";

    public static readonly string[] Allowed = [Conversation, UserAction, Artifact, Decision, SystemResult];
}

public static class ObjectiveEstatus
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";

    public static readonly string[] Allowed = [Draft, Active, Inactive];
}

/// <summary>
/// Representa un objetivo observable y evaluable que el participante debe alcanzar durante un Lab.
/// Puede pertenecer a una etapa específica o a toda la versión del Lab (STG_IdStage es NULL).
/// Reglas de Inmutabilidad:
/// - Los objetivos de una LAB_LabVersion publicada no deben modificarse.
/// - Para cambiar objetivos de una versión publicada debe crearse una nueva versión del Lab.
/// - La validación se implementará posteriormente en el servicio de aplicación.
/// </summary>
public class LAB_Objective
{
    public Guid OBJ_IdObjective { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public Guid? STG_IdStage { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public string OBJ_Codigo { get; set; } = string.Empty;

    public string OBJ_Descripcion { get; set; } = string.Empty;

    public string OBJ_TipoEvidencia { get; set; } = string.Empty;

    public bool OBJ_EsCritico { get; set; }

    public decimal OBJ_Peso { get; set; }

    public string OBJ_CondicionExito { get; set; } = string.Empty;

    public int OBJ_Orden { get; set; }

    public string OBJ_Estatus { get; set; } = ObjectiveEstatus.Draft;

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_LabVersion
    public LAB_LabVersion? LabVersion { get; set; }

    // Navegación opcional hacia LAB_Stage
    public LAB_Stage? Stage { get; set; }

    // Colección de momentos esperados asociados a este objetivo
    public ICollection<LAB_ExpectedMoment> ExpectedMoments { get; set; } = new List<LAB_ExpectedMoment>();

    // Colección de criterios de rúbrica que evalúan este objetivo
    public ICollection<LAB_RubricCriterion> RubricCriteria { get; set; } = new List<LAB_RubricCriterion>();

    // Colección de artefactos que demuestran este objetivo
    public ICollection<LAB_ArtifactSubmission> ArtifactSubmissions { get; set; } = new List<LAB_ArtifactSubmission>();
}
