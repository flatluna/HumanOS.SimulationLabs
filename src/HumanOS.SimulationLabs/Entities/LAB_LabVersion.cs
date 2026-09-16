namespace HumanOS.SimulationLabs.Entities;

public static class LabVersionEstatus
{
    public const string Draft = "DRAFT";
    public const string Approved = "APPROVED";
    public const string Published = "PUBLISHED";
    public const string Retired = "RETIRED";

    public static readonly string[] Allowed = [Draft, Approved, Published, Retired];

    // Reglas de transición futuras documentadas en el modelo:
    // DRAFT -> APPROVED
    // APPROVED -> PUBLISHED
    // DRAFT -> RETIRED
    // APPROVED -> RETIRED
    // PUBLISHED -> RETIRED
}

/// <summary>
/// Representa una versión específica e inmutable de un Lab de simulación.
/// Regla de Inmutabilidad:
/// Una versión con estado PUBLISHED no deberá modificarse directamente.
/// Los cambios posteriores deberán crear una nueva LAB_LabVersion.
/// Esta regla se implementará en los servicios de dominio / APIs.
/// </summary>
public class LAB_LabVersion
{
    public Guid LAB_IdVersion { get; set; }

    public Guid LAB_IdLab { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public int LAB_NumeroVersion { get; set; }

    public string LAB_ObjetivoGeneral { get; set; } = string.Empty;

    public string LAB_InstruccionesParticipante { get; set; } = string.Empty;

    public string? LAB_BriefOculto { get; set; }

    public int LAB_DuracionMinutos { get; set; }

    public decimal LAB_ScoreMinimo { get; set; }

    public string LAB_Estatus { get; set; } = string.Empty;

    public DateTime? LAB_VigenciaDesde { get; set; }

    public DateTime? LAB_VigenciaHasta { get; set; }

    public string? LAB_HashConfiguracion { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Relación de navegación hacia LAB_Lab
    public LAB_Lab? Lab { get; set; }

    // Colección de etapas (Stages) pertenecientes a esta versión del Lab
    public ICollection<LAB_Stage> Stages { get; set; } = new List<LAB_Stage>();

    // Colección de objetivos (Objectives) pertenecientes a esta versión del Lab
    public ICollection<LAB_Objective> Objectives { get; set; } = new List<LAB_Objective>();

    // Colección de momentos esperados pertenecientes a esta versión del Lab
    public ICollection<LAB_ExpectedMoment> ExpectedMoments { get; set; } = new List<LAB_ExpectedMoment>();

    // Rúbrica de evaluación de esta versión del Lab (1 a 0..1)
    public LAB_Rubric? Rubric { get; set; }

    // Colección de escenarios de práctica pertenecientes a esta versión del Lab
    public ICollection<LAB_Scenario> Scenarios { get; set; } = new List<LAB_Scenario>();

    // Colección de intentos de participantes pertenecientes a esta versión del Lab
    public ICollection<LAB_Attempt> Attempts { get; set; } = new List<LAB_Attempt>();
}
