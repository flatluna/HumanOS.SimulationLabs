namespace HumanOS.SimulationLabs.Entities;

public static class MomentTipo
{
    public const string Dialogue = "DIALOGUE";
    public const string UserAction = "USER_ACTION";
    public const string SystemEvent = "SYSTEM_EVENT";
    public const string DecisionPoint = "DECISION_POINT";
    public const string ArtifactReview = "ARTIFACT_REVIEW";

    public static readonly string[] Allowed = [Dialogue, UserAction, SystemEvent, DecisionPoint, ArtifactReview];
}

public static class MomentEstatus
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";

    public static readonly string[] Allowed = [Draft, Active, Inactive];
}

/// <summary>
/// Representa una situación, intervención, acción o momento que puede ocurrir durante una etapa del Lab
/// y que permite observar cómo responde el participante.
/// Reglas de Inmutabilidad:
/// 1. Los momentos de una versión publicada no deberán modificarse directamente.
/// 2. Para cambiar momentos de una versión publicada deberá crearse una nueva LAB_LabVersion.
/// 3. La respuesta ejemplar no se utilizará como comparación textual exacta.
/// 4. La evaluación posterior comparará intención, evidencia y comportamiento.
/// </summary>
public class LAB_ExpectedMoment
{
    public Guid MOM_IdExpectedMoment { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public Guid STG_IdStage { get; set; }

    public Guid? OBJ_IdObjective { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public string MOM_Codigo { get; set; } = string.Empty;

    public string MOM_Nombre { get; set; } = string.Empty;

    public string MOM_Tipo { get; set; } = string.Empty;

    public string MOM_Trigger { get; set; } = string.Empty;

    public string MOM_IntencionEsperada { get; set; } = string.Empty;

    public string? MOM_RespuestaEjemplar { get; set; }

    public string? MOM_InformacionDescubrible { get; set; }

    public string? MOM_ErrorFrecuente { get; set; }

    public string? MOM_Recomendacion { get; set; }

    public bool MOM_EsCritico { get; set; }

    public int MOM_OrdenSugerido { get; set; }

    public bool MOM_PermiteOrdenFlexible { get; set; } = true;

    public bool MOM_RequiereRespuesta { get; set; }

    public string MOM_Estatus { get; set; } = MomentEstatus.Draft;

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegaciones
    public LAB_LabVersion? LabVersion { get; set; }

    public LAB_Stage? Stage { get; set; }

    public LAB_Objective? Objective { get; set; }

    // Colección de criterios de rúbrica relacionados con este momento esperado
    public ICollection<LAB_RubricCriterion> RubricCriteria { get; set; } = new List<LAB_RubricCriterion>();

    // Colección de acciones del participante relacionadas con este momento esperado
    public ICollection<LAB_UserAction> UserActions { get; set; } = new List<LAB_UserAction>();
}
