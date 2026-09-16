namespace HumanOS.SimulationLabs.Entities;

public static class ActorTipo
{
    public const string Client = "CLIENT";
    public const string Stakeholder = "STAKEHOLDER";
    public const string Manager = "MANAGER";
    public const string SubjectMatterExpert = "SUBJECT_MATTER_EXPERT";
    public const string Approver = "APPROVER";
    public const string SystemOperator = "SYSTEM_OPERATOR";
    public const string Observer = "OBSERVER";

    public static readonly string[] Allowed = [Client, Stakeholder, Manager, SubjectMatterExpert, Approver, SystemOperator, Observer];
}

public static class ActorEstiloComunicacion
{
    public const string Direct = "DIRECT";
    public const string Collaborative = "COLLABORATIVE";
    public const string Reserved = "RESERVED";
    public const string Skeptical = "SKEPTICAL";
    public const string Impatient = "IMPATIENT";
    public const string DetailOriented = "DETAIL_ORIENTED";
    public const string Executive = "EXECUTIVE";

    public static readonly string[] Allowed = [Direct, Collaborative, Reserved, Skeptical, Impatient, DetailOriented, Executive];
}

public static class ActorNivelConocimiento
{
    public const string Low = "LOW";
    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
    public const string Expert = "EXPERT";

    public static readonly string[] Allowed = [Low, Medium, High, Expert];
}

public static class ActorEstatus
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";

    public static readonly string[] Allowed = [Draft, Active, Inactive];
}

/// <summary>
/// Representa una persona o stakeholder simulado que participa en un escenario.
/// </summary>
public class LAB_SimulatedActor
{
    public Guid ACT_IdActor { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public Guid SCN_IdScenario { get; set; }

    public string ACT_Codigo { get; set; } = string.Empty;

    public string ACT_Nombre { get; set; } = string.Empty;

    public string ACT_Rol { get; set; } = string.Empty;

    public string ACT_Tipo { get; set; } = string.Empty;

    public string ACT_Descripcion { get; set; } = string.Empty;

    public string ACT_Objetivo { get; set; } = string.Empty;

    public string ACT_ContextoConocido { get; set; } = string.Empty;

    public string ACT_BriefOculto { get; set; } = string.Empty;

    public string? ACT_InformacionPuedeRevelar { get; set; }

    public string? ACT_InformacionNoRevelarAutomaticamente { get; set; }

    public string? ACT_Restricciones { get; set; }

    public string? ACT_Objeciones { get; set; }

    public string? ACT_Contradicciones { get; set; }

    public string ACT_EstiloComunicacion { get; set; } = string.Empty;

    public string ACT_NivelConocimiento { get; set; } = string.Empty;

    public string ACT_Idioma { get; set; } = string.Empty;

    public string? ACT_VoiceName { get; set; }

    public string? ACT_MensajeInicial { get; set; }

    public bool ACT_PuedeIniciarConversacion { get; set; }

    public bool ACT_EsPrincipal { get; set; }

    public int ACT_Orden { get; set; }

    public string ACT_Estatus { get; set; } = ActorEstatus.Draft;

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_Scenario
    public LAB_Scenario? Scenario { get; set; }
}
