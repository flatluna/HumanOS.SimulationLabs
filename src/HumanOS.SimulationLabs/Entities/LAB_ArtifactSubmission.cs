namespace HumanOS.SimulationLabs.Entities;

public static class ArtifactSubmissionTipo
{
    public const string Form = "FORM";
    public const string Worksheet = "WORKSHEET";
    public const string Matrix = "MATRIX";
    public const string Document = "DOCUMENT";
    public const string Diagram = "DIAGRAM";
    public const string DecisionRecord = "DECISION_RECORD";
    public const string Checklist = "CHECKLIST";
    public const string JsonArtifact = "JSON_ARTIFACT";
    public const string FileReference = "FILE_REFERENCE";

    public static readonly string[] Allowed =
        [Form, Worksheet, Matrix, Document, Diagram, DecisionRecord, Checklist, JsonArtifact, FileReference];
}

public static class ArtifactSubmissionFormato
{
    public const string Text = "TEXT";
    public const string Markdown = "MARKDOWN";
    public const string Html = "HTML";
    public const string Json = "JSON";
    public const string StructuredForm = "STRUCTURED_FORM";
    public const string File = "FILE";
    public const string DiagramData = "DIAGRAM_DATA";

    public static readonly string[] Allowed =
        [Text, Markdown, Html, Json, StructuredForm, File, DiagramData];
}

public static class ArtifactSubmissionEstatus
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string Final = "FINAL";
    public const string Replaced = "REPLACED";
    public const string Excluded = "EXCLUDED";
    public const string Error = "ERROR";

    public static readonly string[] Allowed =
        [Draft, Submitted, Final, Replaced, Excluded, Error];
}

public static class ArtifactSubmissionTipoAsistencia
{
    public const string None = "NONE";
    public const string Hint = "HINT";
    public const string Template = "TEMPLATE";
    public const string AiAssistance = "AI_ASSISTANCE";
    public const string HumanAssistance = "HUMAN_ASSISTANCE";
    public const string SystemSuggestion = "SYSTEM_SUGGESTION";

    public static readonly string[] Allowed =
        [None, Hint, Template, AiAssistance, HumanAssistance, SystemSuggestion];
}

/// <summary>
/// Representa un entregable (artefacto) creado o actualizado por un participante durante un intento.
/// Reglas:
/// - Puede guardar texto, JSON o una referencia de archivo.
/// - Una entrega FINAL no debe modificarse directamente; una corrección debe crear una nueva SUB_Version.
/// </summary>
public class LAB_ArtifactSubmission
{
    public Guid SUB_IdSubmission { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public Guid ATT_IdAttempt { get; set; }

    public Guid? STG_IdStage { get; set; }

    public Guid? OBJ_IdObjective { get; set; }

    public string SUB_CodigoArtefacto { get; set; } = string.Empty;

    public string SUB_Nombre { get; set; } = string.Empty;

    public string SUB_Tipo { get; set; } = string.Empty;

    public string SUB_Formato { get; set; } = string.Empty;

    public int SUB_Version { get; set; }

    public string? SUB_ContenidoTexto { get; set; }

    public string? SUB_ContenidoJson { get; set; }

    public string? SUB_BlobPath { get; set; }

    public string? SUB_NombreArchivo { get; set; }

    public string? SUB_MimeType { get; set; }

    public string? SUB_HashSHA256 { get; set; }

    public string SUB_Estatus { get; set; } = ArtifactSubmissionEstatus.Draft;

    public DateTimeOffset? SUB_FechaInicio { get; set; }

    public DateTimeOffset? SUB_FechaEnvio { get; set; }

    public bool SUB_EsEntregaFinal { get; set; }

    public bool SUB_RequiereEvaluacion { get; set; } = true;

    public bool SUB_FueGeneradoConAsistencia { get; set; }

    public string? SUB_TipoAsistencia { get; set; }

    public string? SUB_ErrorCodigo { get; set; }

    public string? SUB_ErrorDescripcion { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_Attempt
    public LAB_Attempt? Attempt { get; set; }

    // Navegación opcional hacia LAB_Stage
    public LAB_Stage? Stage { get; set; }

    // Navegación opcional hacia LAB_Objective
    public LAB_Objective? Objective { get; set; }
}
