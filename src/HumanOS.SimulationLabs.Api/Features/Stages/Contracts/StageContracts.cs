namespace HumanOS.SimulationLabs.Api.Features.Stages.Contracts;

public sealed class CreateStageRequest
{
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoInteraccion { get; set; }
    public bool? EsObligatorio { get; set; }
    public string? CondicionCompletitud { get; set; }
    public int? TiempoSugeridoMinutos { get; set; }
    public bool? PermiteOrdenFlexible { get; set; }
}

public sealed class UpdateStageRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoInteraccion { get; set; }
    public bool? EsObligatorio { get; set; }
    public string? CondicionCompletitud { get; set; }
    public int? TiempoSugeridoMinutos { get; set; }
    public bool? PermiteOrdenFlexible { get; set; }
}

public sealed class ReorderStagesRequest
{
    public List<ReorderStageItem>? Stages { get; set; }
}

public sealed class ReorderStageItem
{
    public Guid IdStage { get; set; }
    public int Orden { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class StageActionRequest { public string? Motivo { get; set; } }

public sealed class StageResponse
{
    public Guid IdStage { get; set; }
    public Guid IdVersion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string TipoInteraccion { get; set; } = string.Empty;
    public bool EsObligatorio { get; set; }
    public string? CondicionCompletitud { get; set; }
    public int? TiempoSugeridoMinutos { get; set; }
    public bool PermiteOrdenFlexible { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class StageListItemResponse
{
    public Guid IdStage { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string TipoInteraccion { get; set; } = string.Empty;
    public bool EsObligatorio { get; set; }
    public int? TiempoSugeridoMinutos { get; set; }
    public bool PermiteOrdenFlexible { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class StageListResponse
{
    public IReadOnlyList<StageListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ReorderStagesResponse
{
    public IReadOnlyList<ReorderStageResponseItem> Items { get; set; } = [];
}

public sealed class ReorderStageResponseItem
{
    public Guid IdStage { get; set; }
    public int Orden { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
