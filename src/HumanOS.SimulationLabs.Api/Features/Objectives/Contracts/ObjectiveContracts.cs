namespace HumanOS.SimulationLabs.Api.Features.Objectives.Contracts;

public sealed class CreateObjectiveRequest
{
    public Guid? StageId { get; set; }
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoEvidencia { get; set; }
    public bool? EsCritico { get; set; }
    public decimal? Peso { get; set; }
    public string? CondicionExito { get; set; }
}

public sealed class UpdateObjectiveRequest
{
    public string? Descripcion { get; set; }
    public string? TipoEvidencia { get; set; }
    public bool? EsCritico { get; set; }
    public decimal? Peso { get; set; }
    public string? CondicionExito { get; set; }
}

public sealed class ReorderObjectivesRequest
{
    public Guid? StageId { get; set; }
    public List<ReorderObjectiveItem>? Objectives { get; set; }
}

public sealed class ReorderObjectiveItem
{
    public Guid IdObjective { get; set; }
    public int Orden { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class ObjectiveActionRequest
{
    public string? Motivo { get; set; }
}

public sealed class ObjectiveResponse
{
    public Guid IdObjective { get; set; }
    public Guid IdVersion { get; set; }
    public Guid? IdStage { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TipoEvidencia { get; set; } = string.Empty;
    public bool EsCritico { get; set; }
    public decimal Peso { get; set; }
    public string CondicionExito { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ObjectiveListItemResponse
{
    public Guid IdObjective { get; set; }
    public Guid IdVersion { get; set; }
    public Guid? IdStage { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TipoEvidencia { get; set; } = string.Empty;
    public bool EsCritico { get; set; }
    public decimal Peso { get; set; }
    public string CondicionExito { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ObjectiveListResponse
{
    public IReadOnlyList<ObjectiveListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ReorderObjectivesResponse
{
    public IReadOnlyList<ReorderObjectiveResponseItem> Items { get; set; } = [];
}

public sealed class ReorderObjectiveResponseItem
{
    public Guid IdObjective { get; set; }
    public int Orden { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
