namespace HumanOS.SimulationLabs.Entities;

public static class LabTipos
{
    public const string Conversational = "CONVERSATIONAL";
    public const string Desktop = "DESKTOP";
    public const string Hybrid = "HYBRID";

    public static readonly string[] Allowed = [Conversational, Desktop, Hybrid];
}

/// <summary>
/// "Archetype" of a Lab — the specialized agent role + real-world process/methodology this Lab's
/// simulated actor follows (interviewer, negotiator, coaching manager, etc.). Distinct from
/// <see cref="LabTipos"/> (which is the voice/desktop/hybrid MODALITY). Drives which instruction
/// block <c>LabSimulationPromptBuilder</c> injects — see <c>LabArchetypes.cs</c>.
/// </summary>
public static class LabArquetipos
{
    public const string JobInterview = "JOB_INTERVIEW";
    public const string ClientNegotiation = "CLIENT_NEGOTIATION";
    public const string PerformanceReview = "PERFORMANCE_REVIEW";
    public const string ConflictResolution = "CONFLICT_RESOLUTION";
    public const string SalesDiscovery = "SALES_DISCOVERY";
    public const string DifficultFeedback = "DIFFICULT_FEEDBACK";
    public const string Onboarding = "ONBOARDING";
    public const string AngryCustomer = "ANGRY_CUSTOMER";
    public const string ExecutivePitch = "EXECUTIVE_PITCH";
    public const string CareerMentoring = "CAREER_MENTORING";
    public const string CustomerSupport = "CUSTOMER_SUPPORT";
    public const string RequirementsGathering = "REQUIREMENTS_GATHERING";
    public const string BusinessReportUpdate = "BUSINESS_REPORT_UPDATE";
    public const string TeachingProcess = "TEACHING_PROCESS";
    public const string AcademicDefense = "ACADEMIC_DEFENSE";

    public static readonly string[] Allowed =
    [
        JobInterview, ClientNegotiation, PerformanceReview, ConflictResolution, SalesDiscovery,
        DifficultFeedback, Onboarding, AngryCustomer, ExecutivePitch, CareerMentoring,
        CustomerSupport, RequirementsGathering, BusinessReportUpdate, TeachingProcess, AcademicDefense
    ];
}

public static class LabEstatus
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";
    public const string Retired = "RETIRED";

    public static readonly string[] Allowed = [Draft, Published, Retired];
}

public class LAB_Lab
{
    public Guid LAB_IdLab { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public string LAB_Codigo { get; set; } = string.Empty;

    public string LAB_Nombre { get; set; } = string.Empty;

    public string LAB_Descripcion { get; set; } = string.Empty;

    public string LAB_Tipo { get; set; } = string.Empty;

    /// <summary>One of <see cref="LabArquetipos"/>, or null for Labs created before this concept
    /// existed (treated as <see cref="LabArquetipos.ClientNegotiation"/> at prompt-build time).</summary>
    public string? LAB_Arquetipo { get; set; }

    public string LAB_Dominio { get; set; } = string.Empty;

    public string LAB_Estatus { get; set; } = string.Empty;

    /// <summary>When true, this Lab is visible in EVERY tenant's catalog, not just
    /// <see cref="SEG_IdTenant"/>'s own — for shared/reference Labs like interview or
    /// thesis-defense simulations meant to be reused across all tenants. Defaults to false
    /// (tenant-scoped) for every Lab created before this concept existed.</summary>
    public bool LAB_EsGlobal { get; set; }

    public Guid LAB_OwnerId { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    // Colección de versiones de este Lab
    public ICollection<LAB_LabVersion> Versiones { get; set; } = new List<LAB_LabVersion>();
}
