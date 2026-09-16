namespace HumanOS.SimulationLabs.Entities;

public static class TurnSpeakerType
{
    public const string Participant = "PARTICIPANT";
    public const string SimulatedActor = "SIMULATED_ACTOR";
    public const string System = "SYSTEM";

    public static readonly string[] Allowed = [Participant, SimulatedActor, System];
}

public static class TurnTipoEntrada
{
    public const string Voice = "VOICE";
    public const string Text = "TEXT";
    public const string System = "SYSTEM";

    public static readonly string[] Allowed = [Voice, Text, System];
}

public static class TurnEstatus
{
    public const string Partial = "PARTIAL";
    public const string Final = "FINAL";
    public const string Corrected = "CORRECTED";
    public const string Excluded = "EXCLUDED";
    public const string Error = "ERROR";

    public static readonly string[] Allowed = [Partial, Final, Corrected, Excluded, Error];
}

/// <summary>
/// Representa una intervención (participante, actor simulado o sistema) dentro de la conversación de un intento.
/// </summary>
public class LAB_ConversationTurn
{
    public Guid TRN_IdTurn { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid ATT_IdAttempt { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public int TRN_NumeroTurno { get; set; }

    public string TRN_SpeakerType { get; set; } = string.Empty;

    public Guid? ACT_IdActor { get; set; }

    public string TRN_Texto { get; set; } = string.Empty;

    public string TRN_TipoEntrada { get; set; } = string.Empty;

    public DateTimeOffset TRN_FechaInicio { get; set; }

    public DateTimeOffset? TRN_FechaFin { get; set; }

    public int? TRN_DuracionMs { get; set; }

    public decimal? TRN_ConfianzaTranscripcion { get; set; }

    public bool TRN_FueInterrumpido { get; set; }

    public bool TRN_InterrumpioTurnoAnterior { get; set; }

    public bool TRN_EsRespuesta { get; set; }

    public Guid? TRN_IdTurnoRespondido { get; set; }

    public Guid? TRN_IdExpectedMoment { get; set; }

    public bool TRN_TextoFueEditado { get; set; }

    public string? TRN_TextoOriginal { get; set; }

    public string? TRN_EditadoPor { get; set; }

    public DateTimeOffset? TRN_FechaEdicion { get; set; }

    public string TRN_Estatus { get; set; } = TurnEstatus.Final;

    public string? TRN_ErrorCodigo { get; set; }

    public string? TRN_ErrorDescripcion { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_Attempt
    public LAB_Attempt? Attempt { get; set; }

    // Navegación opcional hacia LAB_SimulatedActor
    public LAB_SimulatedActor? SimulatedActor { get; set; }

    // Navegación opcional hacia LAB_ExpectedMoment
    public LAB_ExpectedMoment? ExpectedMoment { get; set; }

    // Navegación opcional de autorreferencia (turno respondido)
    public LAB_ConversationTurn? RespondedTurn { get; set; }

    // Colección de turnos que respondieron a este turno
    public ICollection<LAB_ConversationTurn> Responses { get; set; } = new List<LAB_ConversationTurn>();
}
