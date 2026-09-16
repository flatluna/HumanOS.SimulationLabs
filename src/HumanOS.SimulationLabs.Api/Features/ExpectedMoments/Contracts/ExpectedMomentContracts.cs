namespace HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Contracts;

public sealed class CreateExpectedMomentRequest
{
    public Guid? ObjectiveId { get; set; }
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Tipo { get; set; }
    public string? Trigger { get; set; }
    public string? IntencionEsperada { get; set; }
    public string? RespuestaEjemplar { get; set; }
    public string? InformacionDescubrible { get; set; }
    public string? ErrorFrecuente { get; set; }
    public string? Recomendacion { get; set; }
    public bool? EsCritico { get; set; }
    public bool? PermiteOrdenFlexible { get; set; }
    public bool? RequiereRespuesta { get; set; }
}

public sealed class UpdateExpectedMomentRequest
{
    public Guid? ObjectiveId { get; set; }
    // Move this moment to a different Stage within the same LabVersion — appended at the end of the target stage's order.
    public Guid? StageId { get; set; }
    public string? Nombre { get; set; }
    public string? Tipo { get; set; }
    public string? Trigger { get; set; }
    public string? IntencionEsperada { get; set; }
    public string? RespuestaEjemplar { get; set; }
    public string? InformacionDescubrible { get; set; }
    public string? ErrorFrecuente { get; set; }
    public string? Recomendacion { get; set; }
    public bool? EsCritico { get; set; }
    public bool? PermiteOrdenFlexible { get; set; }
    public bool? RequiereRespuesta { get; set; }
}

public sealed class ReorderExpectedMomentsRequest
{
    public List<ReorderExpectedMomentItem>? Moments { get; set; }
}

public sealed class ReorderExpectedMomentItem
{
    public Guid IdExpectedMoment { get; set; }
    public int Orden { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class ExpectedMomentActionRequest
{
    public string? Motivo { get; set; }
}

public sealed class ExpectedMomentResponse
{
    public Guid IdExpectedMoment { get; set; }
    public Guid IdVersion { get; set; }
    public Guid StageId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string IntencionEsperada { get; set; } = string.Empty;
    public string? RespuestaEjemplar { get; set; }
    public string? InformacionDescubrible { get; set; }
    public string? ErrorFrecuente { get; set; }
    public string? Recomendacion { get; set; }
    public bool EsCritico { get; set; }
    public int OrdenSugerido { get; set; }
    public bool PermiteOrdenFlexible { get; set; }
    public bool RequiereRespuesta { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ExpectedMomentListItemResponse
{
    public Guid IdExpectedMoment { get; set; }
    public Guid IdVersion { get; set; }
    public Guid StageId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string IntencionEsperada { get; set; } = string.Empty;
    public string? RespuestaEjemplar { get; set; }
    public string? ErrorFrecuente { get; set; }
    public string? Recomendacion { get; set; }
    public bool EsCritico { get; set; }
    public bool RequiereRespuesta { get; set; }
    public int OrdenSugerido { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ExpectedMomentListResponse
{
    public IReadOnlyList<ExpectedMomentListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ReorderExpectedMomentsResponse
{
    public IReadOnlyList<ReorderExpectedMomentResponseItem> Items { get; set; } = [];
}

public sealed class ReorderExpectedMomentResponseItem
{
    public Guid IdExpectedMoment { get; set; }
    public int Orden { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
