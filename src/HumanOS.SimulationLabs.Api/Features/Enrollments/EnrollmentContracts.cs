namespace HumanOS.SimulationLabs.Api.Features.Enrollments;

public sealed class EnrollmentResponse
{
    public Guid IdEnrollment { get; set; }
    public Guid IdLab { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaInscripcion { get; set; }
    public DateTimeOffset? FechaCancelacion { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class EnrollmentNotFoundException : Exception { }
