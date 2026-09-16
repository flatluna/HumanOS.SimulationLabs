namespace HumanOS.SimulationLabs.Entities;

public static class ScenarioTipo
{
    public const string Conversational = "CONVERSATIONAL";
    public const string Desktop = "DESKTOP";
    public const string Hybrid = "HYBRID";

    public static readonly string[] Allowed = [Conversational, Desktop, Hybrid];
}

public static class ScenarioDificultad
{
    public const string Beginner = "BEGINNER";
    public const string Intermediate = "INTERMEDIATE";
    public const string Advanced = "ADVANCED";
    public const string Expert = "EXPERT";

    public static readonly string[] Allowed = [Beginner, Intermediate, Advanced, Expert];
}

public static class ScenarioEstatus
{
    public const string Draft = "DRAFT";
    public const string Approved = "APPROVED";
    public const string Published = "PUBLISHED";
    public const string Retired = "RETIRED";

    public static readonly string[] Allowed = [Draft, Approved, Published, Retired];
}

/// <summary>
/// Representa una situación concreta de práctica dentro de una versión del Lab.
/// </summary>
public class LAB_Scenario
{
    public Guid SCN_IdScenario { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public string SCN_Codigo { get; set; } = string.Empty;

    public string SCN_Nombre { get; set; } = string.Empty;

    public string SCN_Descripcion { get; set; } = string.Empty;

    public string SCN_Tipo { get; set; } = string.Empty;

    public string SCN_Dificultad { get; set; } = string.Empty;

    public string SCN_ContextoParticipante { get; set; } = string.Empty;

    public string SCN_BriefOculto { get; set; } = string.Empty;

    public string SCN_ProblemaCentral { get; set; } = string.Empty;

    public string SCN_ResultadoEsperado { get; set; } = string.Empty;

    public string? SCN_CondicionesIniciales { get; set; }

    public string? SCN_Restricciones { get; set; }

    public string? SCN_Supuestos { get; set; }

    public string? SCN_Riesgos { get; set; }

    public string? SCN_InformacionNoRevelarAutomaticamente { get; set; }

    public string? SCN_MensajeInicial { get; set; }

    public int? SCN_DuracionSugeridaMinutos { get; set; }

    public decimal? SCN_PuntuacionObjetivo { get; set; }

    public bool SCN_PermiteReintento { get; set; } = true;

    public int? SCN_MaximoIntentos { get; set; }

    public bool SCN_UsaVariacion { get; set; }

    public string? SCN_SeedBase { get; set; }

    public string SCN_Estatus { get; set; } = ScenarioEstatus.Draft;

    public DateTime? SCN_VigenciaDesde { get; set; }

    public DateTime? SCN_VigenciaHasta { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_LabVersion
    public LAB_LabVersion? LabVersion { get; set; }

    // Navegación hacia los actores simulados del escenario
    public ICollection<LAB_SimulatedActor> SimulatedActors { get; set; } = new List<LAB_SimulatedActor>();

    // Navegación hacia los intentos de participantes en el escenario
    public ICollection<LAB_Attempt> Attempts { get; set; } = new List<LAB_Attempt>();
}
