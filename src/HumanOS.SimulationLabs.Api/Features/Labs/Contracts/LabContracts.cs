namespace HumanOS.SimulationLabs.Api.Features.Labs.Contracts;

public sealed class CreateLabRequest
{
    public string? Codigo { get; set; }

    public string? Nombre { get; set; }

    public string? Descripcion { get; set; }

    public string? Tipo { get; set; }

    public string? Dominio { get; set; }

    public Guid? OwnerId { get; set; }
}

public sealed class UpdateLabRequest
{
    public string? Nombre { get; set; }

    public string? Descripcion { get; set; }

    public string? Tipo { get; set; }

    public string? Dominio { get; set; }

    public Guid? OwnerId { get; set; }

    /// <summary>When set, updates whether this Lab is visible to every tenant.</summary>
    public bool? EsGlobal { get; set; }
}

public sealed class InactivateLabRequest
{
    public string? Motivo { get; set; }
}

public sealed class LabResponse
{
    public Guid IdLab { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    /// <summary>One of <see cref="HumanOS.SimulationLabs.Entities.LabArquetipos"/>, or null for Labs
    /// created before this concept existed (treated as CLIENT_NEGOTIATION at runtime).</summary>
    public string? Arquetipo { get; set; }

    public string Dominio { get; set; } = string.Empty;

    public string Estatus { get; set; } = string.Empty;

    public bool EsGlobal { get; set; }

    public Guid OwnerId { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public string CreadoPor { get; set; } = string.Empty;

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string? ActualizadoPor { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class LabListItemResponse
{
    public Guid IdLab { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Dominio { get; set; } = string.Empty;

    public string Estatus { get; set; } = string.Empty;

    public bool EsGlobal { get; set; }

    public Guid OwnerId { get; set; }

    public DateTimeOffset FechaCreacion { get; set; }

    public DateTimeOffset? FechaActualizacion { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class InactivateLabResponse
{
    public Guid IdLab { get; set; }

    public string Estatus { get; set; } = string.Empty;

    public DateTimeOffset FechaActualizacion { get; set; }

    public string ActualizadoPor { get; set; } = string.Empty;

    public string RowVersion { get; set; } = string.Empty;
}

public sealed class LabListQuery
{
    public string? Search { get; set; }

    public string? Tipo { get; set; }

    public string? Dominio { get; set; }

    public string? Estatus { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? SortBy { get; set; }

    public string? SortDirection { get; set; }
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }
}
