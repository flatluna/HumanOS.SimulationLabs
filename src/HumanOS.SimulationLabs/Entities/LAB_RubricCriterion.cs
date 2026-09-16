namespace HumanOS.SimulationLabs.Entities;

public static class CriterionTipoEvidencia
{
    public const string Conversation = "CONVERSATION";
    public const string UserAction = "USER_ACTION";
    public const string Artifact = "ARTIFACT";
    public const string Decision = "DECISION";
    public const string SystemResult = "SYSTEM_RESULT";
    public const string Multiple = "MULTIPLE";

    public static readonly string[] Allowed = [Conversation, UserAction, Artifact, Decision, SystemResult, Multiple];
}

public static class CriterionEstatus
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";

    public static readonly string[] Allowed = [Draft, Active, Inactive];
}

/// <summary>
/// Representa cada criterio específico utilizado para evaluar el desempeño del participante en un Lab.
/// 
/// Reglas de Inmutabilidad, Evaluación y Coherencia Documentadas:
/// 1. El score del criterio se calculará posteriormente.
/// 2. Esta tabla no almacena resultados de participantes ni scores obtenidos.
/// 3. La respuesta del participante no se comparará palabra por palabra con una respuesta ejemplar.
/// 4. La evaluación debe utilizar comportamientos observables y evidencia.
/// 5. Cuando un criterio sea crítico (CRT_EsCritico = true), su falla podrá impedir la aprobación según el método de LAB_Rubric.
/// 6. No se deben evaluar acento, personalidad, atributos protegidos o inferencias personales.
/// 7. Los criterios de una rúbrica publicada no deben modificarse directamente.
/// 8. Para cambiar criterios publicados debe crearse una nueva LAB_LabVersion y una nueva LAB_Rubric.
/// 9. No implementar triggers SQL para estas reglas.
/// 10. Coherencia entre objetivo y momento:
///     Cuando el criterio tenga tanto OBJ_IdObjective como MOM_IdExpectedMoment:
///     - El momento esperado debe estar asociado con el mismo objetivo o no tener objetivo.
///     - No debe permitirse asociar el criterio con un objetivo y con un momento que pertenece explícitamente a otro objetivo.
///     - Esta validación se implementará posteriormente en el servicio de aplicación.
///     - La protección de tenant y versión se mantiene estrictamente a nivel de Azure SQL mediante Foreign Keys compuestas.
/// </summary>
public class LAB_RubricCriterion
{
    // 1. CRT_IdCriterion - Primary Key
    public Guid CRT_IdCriterion { get; set; }

    // 2. RUB_IdRubric - Identifica la rúbrica a la que pertenece el criterio
    public Guid RUB_IdRubric { get; set; }

    // 3. LAB_IdVersion - Versión del Lab (integridad tenant/versión)
    public Guid LAB_IdVersion { get; set; }

    // 4. SEG_IdTenant - Tenant al que pertenece
    public Guid SEG_IdTenant { get; set; }

    // 5. OBJ_IdObjective - Opcional. Objetivo evaluado
    public Guid? OBJ_IdObjective { get; set; }

    // 6. MOM_IdExpectedMoment - Opcional. Momento esperado relacionado
    public Guid? MOM_IdExpectedMoment { get; set; }

    // 7. CRT_Codigo - Código único dentro de la rúbrica
    public string CRT_Codigo { get; set; } = string.Empty;

    // 8. CRT_Nombre - Nombre visible del criterio
    public string CRT_Nombre { get; set; } = string.Empty;

    // 9. CRT_Descripcion - Describe comportamiento o resultado evaluado
    public string CRT_Descripcion { get; set; } = string.Empty;

    // 10. CRT_TipoEvidencia - Fuente de evidencia
    public string CRT_TipoEvidencia { get; set; } = string.Empty;

    // 11. CRT_Peso - Importancia dentro de la rúbrica (> 0)
    public decimal CRT_Peso { get; set; }

    // 12. CRT_ScoreMinimoEsperado - Entre 1.00 y 10.00
    public decimal CRT_ScoreMinimoEsperado { get; set; } = 7.00m;

    // 13. CRT_EsCritico - Si fallar impide la aprobación
    public bool CRT_EsCritico { get; set; }

    // 14. CRT_IndicadoresPositivos - Comportamientos observables positivos
    public string CRT_IndicadoresPositivos { get; set; } = string.Empty;

    // 15. CRT_IndicadoresNegativos - Señales de desempeño deficiente (opcional)
    public string? CRT_IndicadoresNegativos { get; set; }

    // 16. CRT_ErrorCritico - Comportamiento de falla crítica (opcional)
    public string? CRT_ErrorCritico { get; set; }

    // 17. CRT_RecomendacionBase - Recomendación general para feedback (opcional)
    public string? CRT_RecomendacionBase { get; set; }

    // 18. CRT_Orden - Posición dentro de la rúbrica (> 0)
    public int CRT_Orden { get; set; }

    // 19. CRT_Estatus - DRAFT, ACTIVE, INACTIVE
    public string CRT_Estatus { get; set; } = CriterionEstatus.Draft;

    // 20. FechaCreacion
    public DateTimeOffset FechaCreacion { get; set; }

    // 21. CreadoPor
    public string CreadoPor { get; set; } = string.Empty;

    // 22. FechaActualizacion (opcional)
    public DateTimeOffset? FechaActualizacion { get; set; }

    // 23. ActualizadoPor (opcional)
    public string? ActualizadoPor { get; set; }

    // 24. RowVersion - Concurrency token
    public byte[] RowVersion { get; set; } = [];

    // Navegación obligatoria hacia LAB_Rubric
    public LAB_Rubric Rubric { get; set; } = null!;

    // Navegación opcional hacia LAB_Objective
    public LAB_Objective? Objective { get; set; }

    // Navegación opcional hacia LAB_ExpectedMoment
    public LAB_ExpectedMoment? ExpectedMoment { get; set; }
}
