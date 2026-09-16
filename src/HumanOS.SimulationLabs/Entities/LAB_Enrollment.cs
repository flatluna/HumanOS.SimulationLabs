namespace HumanOS.SimulationLabs.Entities;

public static class EnrollmentEstatus
{
    public const string Enrolled = "ENROLLED";
    public const string Cancelled = "CANCELLED";

    public static readonly string[] Allowed = [Enrolled, Cancelled];
}

/// <summary>
/// Registra que un participante se inscribió en un Lab para ejecutarlo más adelante —
/// separado de LAB_Attempt (que representa una ejecución concreta de un Scenario). Un
/// Lab puede tener 0 o más Scenarios/versiones; la inscripción es a nivel de Lab, no de
/// intento, para que "Inscribirme" funcione incluso antes de que el Lab tenga una versión
/// publicada lista para correr.
/// </summary>
public class LAB_Enrollment
{
    public Guid ENR_IdEnrollment { get; set; }

    public Guid SEG_IdTenant { get; set; }

    public Guid LAB_IdLab { get; set; }

    public Guid USR_IdParticipant { get; set; }

    public string ENR_Estatus { get; set; } = EnrollmentEstatus.Enrolled;

    public DateTimeOffset ENR_FechaInscripcion { get; set; }

    public DateTimeOffset? ENR_FechaCancelacion { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public LAB_Lab? Lab { get; set; }
}
