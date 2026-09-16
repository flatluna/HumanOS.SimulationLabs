namespace HumanOS.SimulationLabs.Api.Features.Scenarios.Contracts;

public sealed class CreateScenarioRequest
{
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? Tipo { get; set; }
    public string? Dificultad { get; set; }
    public string? ContextoParticipante { get; set; }
    public string? BriefOculto { get; set; }
    public string? ProblemaCentral { get; set; }
    public string? ResultadoEsperado { get; set; }
    public string? CondicionesIniciales { get; set; }
    public string? Restricciones { get; set; }
    public string? Supuestos { get; set; }
    public string? Riesgos { get; set; }
    public string? InformacionNoRevelarAutomaticamente { get; set; }
    public string? MensajeInicial { get; set; }
    public int? DuracionSugeridaMinutos { get; set; }
    public decimal? PuntuacionObjetivo { get; set; }
    public bool? PermiteReintento { get; set; }
    public int? MaximoIntentos { get; set; }
    public bool? UsaVariacion { get; set; }
    public string? SeedBase { get; set; }
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
}

public sealed class UpdateScenarioRequest
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? Tipo { get; set; }
    public string? Dificultad { get; set; }
    public string? ContextoParticipante { get; set; }
    public string? BriefOculto { get; set; }
    public string? ProblemaCentral { get; set; }
    public string? ResultadoEsperado { get; set; }
    public string? CondicionesIniciales { get; set; }
    public string? Restricciones { get; set; }
    public string? Supuestos { get; set; }
    public string? Riesgos { get; set; }
    public string? InformacionNoRevelarAutomaticamente { get; set; }
    public string? MensajeInicial { get; set; }
    public int? DuracionSugeridaMinutos { get; set; }
    public decimal? PuntuacionObjetivo { get; set; }
    public bool? PermiteReintento { get; set; }
    public int? MaximoIntentos { get; set; }
    public bool? UsaVariacion { get; set; }
    public string? SeedBase { get; set; }
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
}

public sealed class ScenarioActionRequest { public string? Motivo { get; set; } }

public sealed class ScenarioResponse
{
    public Guid IdScenario { get; set; }
    public Guid IdVersion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Dificultad { get; set; } = string.Empty;
    public string ContextoParticipante { get; set; } = string.Empty;
    public string? BriefOculto { get; set; }
    public string ProblemaCentral { get; set; } = string.Empty;
    public string ResultadoEsperado { get; set; } = string.Empty;
    public string? CondicionesIniciales { get; set; }
    public string? Restricciones { get; set; }
    public string? Supuestos { get; set; }
    public string? Riesgos { get; set; }
    public string? InformacionNoRevelarAutomaticamente { get; set; }
    public string? MensajeInicial { get; set; }
    public int? DuracionSugeridaMinutos { get; set; }
    public decimal? PuntuacionObjetivo { get; set; }
    public bool PermiteReintento { get; set; }
    public int? MaximoIntentos { get; set; }
    public bool UsaVariacion { get; set; }
    public string? SeedBase { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ScenarioListItemResponse
{
    public Guid IdScenario { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Dificultad { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public int? DuracionSugeridaMinutos { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ScenarioListResponse
{
    public IReadOnlyList<ScenarioListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
