namespace HumanOS.SimulationLabs.Entities;

public static class RubricTipoEvaluacion
{
    public const string Conversation = "CONVERSATION";
    public const string Desktop = "DESKTOP";
    public const string Hybrid = "HYBRID";
    public const string Artifact = "ARTIFACT";
    public const string MultiSource = "MULTI_SOURCE";

    public static readonly string[] Allowed = [Conversation, Desktop, Hybrid, Artifact, MultiSource];
}

public static class RubricMetodoCalculo
{
    public const string WeightedAverage = "WEIGHTED_AVERAGE";
    public const string SimpleAverage = "SIMPLE_AVERAGE";
    public const string CriticalGate = "CRITICAL_GATE";
    public const string WeightedWithCriticalGate = "WEIGHTED_WITH_CRITICAL_GATE";

    public static readonly string[] Allowed = [WeightedAverage, SimpleAverage, CriticalGate, WeightedWithCriticalGate];
}

public static class RubricEstatus
{
    public const string Draft = "DRAFT";
    public const string Approved = "APPROVED";
    public const string Published = "PUBLISHED";
    public const string Retired = "RETIRED";

    public static readonly string[] Allowed = [Draft, Approved, Published, Retired];
}

/// <summary>
/// Define las reglas generales utilizadas para evaluar una versión específica de un Lab.
/// Relación 1 a 0..1 con LAB_LabVersion.
/// Reglas de Inmutabilidad y Evaluación:
/// 1. Una rúbrica publicada no debe modificarse directamente.
/// 2. Para cambiar una rúbrica publicada debe crearse una nueva LAB_LabVersion.
/// 3. La evaluación debe basarse en comportamientos y resultados observables.
/// 4. Toda evaluación debe citar evidencia cuando RUB_RequiereEvidencia sea true.
/// 5. No se debe evaluar acento, género, edad inferida, origen, personalidad ni otros atributos protegidos.
/// 6. Estas reglas se implementarán en los servicios de aplicación y evaluadores.
/// </summary>
public class LAB_Rubric
{
    public Guid RUB_IdRubric { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdVersion { get; set; }

    public string RUB_Codigo { get; set; } = string.Empty;

    public string RUB_Nombre { get; set; } = string.Empty;

    public string RUB_Descripcion { get; set; } = string.Empty;

    public string RUB_TipoEvaluacion { get; set; } = string.Empty;

    public decimal RUB_EscalaMinima { get; set; } = 1.00m;

    public decimal RUB_EscalaMaxima { get; set; } = 10.00m;

    public decimal RUB_ScoreMinimoAprobacion { get; set; } = 7.00m;

    public string RUB_MetodoCalculo { get; set; } = RubricMetodoCalculo.WeightedWithCriticalGate;

    public bool RUB_RequiereEvidencia { get; set; } = true;

    public bool RUB_PermiteFallaCritica { get; set; } = true;

    public string RUB_InstruccionesEvaluador { get; set; } = string.Empty;

    public string RUB_Estatus { get; set; } = RubricEstatus.Draft;

    public DateTime? RUB_VigenciaDesde { get; set; }

    public DateTime? RUB_VigenciaHasta { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_LabVersion
    public LAB_LabVersion? LabVersion { get; set; }

    // Colección de criterios que componen esta rúbrica
    public ICollection<LAB_RubricCriterion> Criteria { get; set; } = new List<LAB_RubricCriterion>();
}
