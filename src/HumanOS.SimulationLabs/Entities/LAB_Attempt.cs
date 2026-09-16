namespace HumanOS.SimulationLabs.Entities;

public static class AttemptEstatus
{
    public const string NotStarted = "NOT_STARTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Paused = "PAUSED";
    public const string Completed = "COMPLETED";
    public const string Abandoned = "ABANDONED";
    public const string Evaluating = "EVALUATING";
    public const string Evaluated = "EVALUATED";
    public const string Cancelled = "CANCELLED";
    public const string Error = "ERROR";

    public static readonly string[] Allowed =
        [NotStarted, InProgress, Paused, Completed, Abandoned, Evaluating, Evaluated, Cancelled, Error];
}

public static class AttemptModalidad
{
    public const string Voice = "VOICE";
    public const string Desktop = "DESKTOP";
    public const string Hybrid = "HYBRID";

    public static readonly string[] Allowed = [Voice, Desktop, Hybrid];
}

public static class AttemptResultado
{
    public const string Passed = "PASSED";
    public const string Partial = "PARTIAL";
    public const string RepeatRecommended = "REPEAT_RECOMMENDED";
    public const string NotCompleted = "NOT_COMPLETED";
    public const string CriticalFailure = "CRITICAL_FAILURE";

    public static readonly string[] Allowed = [Passed, Partial, RepeatRecommended, NotCompleted, CriticalFailure];
}

/// <summary>
/// Representa un intento de un participante al ejecutar un escenario.
/// </summary>
public class LAB_Attempt
{
    public Guid ATT_IdAttempt { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public Guid SCN_IdScenario { get; set; }

    public Guid USR_IdParticipant { get; set; }

    public int ATT_NumeroIntento { get; set; }

    public string ATT_Estatus { get; set; } = AttemptEstatus.NotStarted;

    public DateTimeOffset? ATT_FechaInicio { get; set; }

    public DateTimeOffset? ATT_FechaFin { get; set; }

    public int? ATT_DuracionSegundos { get; set; }

    public string ATT_Modalidad { get; set; } = string.Empty;

    public string? ATT_SeedEjecucion { get; set; }

    public string? ATT_HashConfiguracion { get; set; }

    public decimal? ATT_ScoreFinal { get; set; }

    public string? ATT_Resultado { get; set; }

    public string ATT_Idioma { get; set; } = string.Empty;

    public bool ATT_UsaVoz { get; set; }

    public bool ATT_UsaEscritorio { get; set; }

    public bool ATT_UsaArtefactos { get; set; }

    public string? ATT_ErrorCodigo { get; set; }

    public string? ATT_ErrorDescripcion { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_LabVersion
    public LAB_LabVersion? LabVersion { get; set; }

    // Navegación obligatoria hacia LAB_Scenario
    public LAB_Scenario? Scenario { get; set; }

    // Colección de turnos de conversación pertenecientes a este intento
    public ICollection<LAB_ConversationTurn> ConversationTurns { get; set; } = new List<LAB_ConversationTurn>();

    // Colección de acciones del participante pertenecientes a este intento
    public ICollection<LAB_UserAction> UserActions { get; set; } = new List<LAB_UserAction>();

    // Colección de artefactos entregados durante este intento
    public ICollection<LAB_ArtifactSubmission> ArtifactSubmissions { get; set; } = new List<LAB_ArtifactSubmission>();
}
