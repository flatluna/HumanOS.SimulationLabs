namespace HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;

public sealed class StartAttemptRequest
{
    public string? Idioma { get; set; }
    public string? Modalidad { get; set; }
}

public sealed class AttemptActionRequest { public string? Motivo { get; set; } }

public sealed class AttemptResponse
{
    public Guid IdAttempt { get; set; }
    public Guid IdVersion { get; set; }
    public Guid IdScenario { get; set; }
    public Guid IdParticipant { get; set; }
    public int NumeroIntento { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset? FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
    public int? DuracionSegundos { get; set; }
    public string Modalidad { get; set; } = string.Empty;
    public string? SeedEjecucion { get; set; }
    public string? HashConfiguracion { get; set; }
    public decimal? ScoreFinal { get; set; }
    public string? Resultado { get; set; }
    public string Idioma { get; set; } = string.Empty;
    public bool UsaVoz { get; set; }
    public bool UsaEscritorio { get; set; }
    public bool UsaArtefactos { get; set; }
    public string? ErrorCodigo { get; set; }
    public string? ErrorDescripcion { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class AttemptListItemResponse
{
    public Guid IdAttempt { get; set; }
    public Guid IdScenario { get; set; }
    public int NumeroIntento { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string? Resultado { get; set; }
    public DateTimeOffset? FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public Guid? IdLab { get; set; }
    public string? LabNombre { get; set; }
    public string? ScenarioNombre { get; set; }
    public decimal? ScoreFinal { get; set; }
}

public sealed class AttemptListResponse
{
    public IReadOnlyList<AttemptListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
