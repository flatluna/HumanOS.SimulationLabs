namespace HumanOS.SimulationLabs.Entities;

public static class UserActionTipo
{
    public const string Navigation = "NAVIGATION";
    public const string View = "VIEW";
    public const string Search = "SEARCH";
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Select = "SELECT";
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string Escalate = "ESCALATE";
    public const string Submit = "SUBMIT";
    public const string Download = "DOWNLOAD";
    public const string Upload = "UPLOAD";
    public const string Decision = "DECISION";
    public const string SystemCommand = "SYSTEM_COMMAND";

    public static readonly string[] Allowed =
        [Navigation, View, Search, Create, Update, Select, Approve, Reject, Escalate, Submit, Download, Upload, Decision, SystemCommand];
}

public static class UserActionResultado
{
    public const string Success = "SUCCESS";
    public const string Blocked = "BLOCKED";
    public const string Warning = "WARNING";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";
    public const string Pending = "PENDING";

    public static readonly string[] Allowed = [Success, Blocked, Warning, Failed, Cancelled, Pending];
}

public static class UserActionSeveridad
{
    public const string Info = "INFO";
    public const string Low = "LOW";
    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
    public const string Critical = "CRITICAL";

    public static readonly string[] Allowed = [Info, Low, Medium, High, Critical];
}

public static class UserActionOrigen
{
    public const string Participant = "PARTICIPANT";
    public const string System = "SYSTEM";
    public const string Simulation = "SIMULATION";

    public static readonly string[] Allowed = [Participant, System, Simulation];
}

public static class UserActionEstatus
{
    public const string Started = "STARTED";
    public const string Final = "FINAL";
    public const string Reversed = "REVERSED";
    public const string Excluded = "EXCLUDED";
    public const string Error = "ERROR";

    public static readonly string[] Allowed = [Started, Final, Reversed, Excluded, Error];
}

/// <summary>
/// Representa una acción observable ejecutada por el participante durante un intento de Lab de escritorio o híbrido.
/// </summary>
public class LAB_UserAction
{
    public Guid ACT_IdUserAction { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public Guid ATT_IdAttempt { get; set; }

    public int ACT_NumeroSecuencia { get; set; }

    public string ACT_Codigo { get; set; } = string.Empty;

    public string ACT_Nombre { get; set; } = string.Empty;

    public string ACT_Tipo { get; set; } = string.Empty;

    public string? ACT_Pagina { get; set; }

    public string? ACT_Componente { get; set; }

    public string? ACT_EntidadTipo { get; set; }

    public string? ACT_EntidadId { get; set; }

    public string? ACT_ValorAnterior { get; set; }

    public string? ACT_ValorNuevo { get; set; }

    public string? ACT_InputJson { get; set; }

    public string? ACT_OutputJson { get; set; }

    public string ACT_Resultado { get; set; } = string.Empty;

    public string? ACT_MensajeResultado { get; set; }

    public bool ACT_EraPermitida { get; set; }

    public bool ACT_EraEsperada { get; set; }

    public bool ACT_EsCritica { get; set; }

    public string ACT_Severidad { get; set; } = string.Empty;

    public string? ACT_MotivoEvaluacion { get; set; }

    public Guid? MOM_IdExpectedMoment { get; set; }

    public DateTimeOffset ACT_FechaInicio { get; set; }

    public DateTimeOffset? ACT_FechaFin { get; set; }

    public int? ACT_DuracionMs { get; set; }

    public string ACT_Origen { get; set; } = string.Empty;

    public string ACT_Estatus { get; set; } = UserActionEstatus.Final;

    public string? ACT_ErrorCodigo { get; set; }

    public string? ACT_ErrorDescripcion { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_Attempt
    public LAB_Attempt? Attempt { get; set; }

    // Navegación opcional hacia LAB_ExpectedMoment
    public LAB_ExpectedMoment? ExpectedMoment { get; set; }
}
