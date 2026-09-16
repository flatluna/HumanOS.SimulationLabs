namespace HumanOS.SimulationLabs.Api.Features.Rubrics.Contracts;

public sealed class CreateRubricRequest
{
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoEvaluacion { get; set; }
    public decimal? ScoreMinimoAprobacion { get; set; }
    public bool? RequiereEvidencia { get; set; }
    public bool? PermiteFallaCritica { get; set; }
    public string? MetodoCalculo { get; set; }
    public string? InstruccionesEvaluador { get; set; }
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
}

public sealed class UpdateRubricRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoEvaluacion { get; set; }
    public decimal? ScoreMinimoAprobacion { get; set; }
    public bool? RequiereEvidencia { get; set; }
    public bool? PermiteFallaCritica { get; set; }
    public string? MetodoCalculo { get; set; }
    public string? InstruccionesEvaluador { get; set; }
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
}

public sealed class RubricActionRequest
{
    public string? Motivo { get; set; }
}

public sealed class PublishRubricRequest
{
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public string? Motivo { get; set; }
}

public sealed class RetireRubricRequest
{
    public string? Motivo { get; set; }
    public DateTime? VigenciaHasta { get; set; }
}

public sealed class RubricResponse
{
    public Guid IdRubric { get; set; }
    public Guid IdVersion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TipoEvaluacion { get; set; } = string.Empty;
    public decimal EscalaMinima { get; set; }
    public decimal EscalaMaxima { get; set; }
    public decimal ScoreMinimoAprobacion { get; set; }
    public bool RequiereEvidencia { get; set; }
    public bool PermiteFallaCritica { get; set; }
    public string MetodoCalculo { get; set; } = string.Empty;
    public string InstruccionesEvaluador { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
