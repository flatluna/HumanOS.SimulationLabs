namespace HumanOS.SimulationLabs.Api.Features.LabVersions.Contracts;

public sealed class CreateLabVersionRequest
{
    public string? ObjetivoGeneral { get; set; }

    public string? InstruccionesParticipante { get; set; }

    public string? BriefOculto { get; set; }

    public int? DuracionMinutos { get; set; }

    public decimal? ScoreMinimo { get; set; }

    public DateTime? VigenciaDesde { get; set; }

    public DateTime? VigenciaHasta { get; set; }
}

public sealed class UpdateLabVersionRequest
{
    public string? ObjetivoGeneral { get; set; }

    public string? InstruccionesParticipante { get; set; }

    public string? BriefOculto { get; set; }

    public int? DuracionMinutos { get; set; }

    public decimal? ScoreMinimo { get; set; }

    public DateTime? VigenciaDesde { get; set; }

    public DateTime? VigenciaHasta { get; set; }
}

public sealed class ApproveLabVersionRequest
{
    public string? Motivo { get; set; }
}

public sealed class PublishLabVersionRequest
{
    public DateTime? VigenciaDesde { get; set; }

    public DateTime? VigenciaHasta { get; set; }

    public string? Motivo { get; set; }
}

public sealed class RetireLabVersionRequest
{
    public string? Motivo { get; set; }

    public DateTime? VigenciaHasta { get; set; }
}

public sealed class LabVersionListQuery
{
    public string? Estatus { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? SortDirection { get; set; } = "DESC";
}

public sealed class LabVersionDetailResponse
{
    public Guid IdVersion { get; set; }

    public Guid IdLab { get; set; }

    public int NumeroVersion { get; set; }

    public string ObjetivoGeneral { get; set; } = string.Empty;

    public string InstruccionesParticipante { get; set; } = string.Empty;

    public string? BriefOculto { get; set; }

    public int DuracionMinutos { get; set; }

    public decimal ScoreMinimo { get; set; }

    public string Estatus { get; set; } = string.Empty;

    public DateTime? VigenciaDesde { get; set; }

    public DateTime? VigenciaHasta { get; set; }

    public string? HashConfiguracion { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class LabVersionListItemResponse
{
    public Guid IdVersion { get; set; }

    public int NumeroVersion { get; set; }

    public string ObjetivoGeneral { get; set; } = string.Empty;

    public int DuracionMinutos { get; set; }

    public decimal ScoreMinimo { get; set; }

    public string Estatus { get; set; } = string.Empty;

    public DateTime? VigenciaDesde { get; set; }

    public DateTime? VigenciaHasta { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class PublishLabVersionResponse
{
    public Guid IdVersion { get; set; }

    public int NumeroVersion { get; set; }

    public string Estatus { get; set; } = string.Empty;

    public DateTime? VigenciaDesde { get; set; }

    public DateTime? VigenciaHasta { get; set; }

    public string? HashConfiguracion { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class RetireLabVersionResponse
{
    public Guid IdVersion { get; set; }

    public int NumeroVersion { get; set; }

    public string Estatus { get; set; } = string.Empty;

    public DateTime? VigenciaHasta { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
