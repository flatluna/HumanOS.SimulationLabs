namespace HumanOS.SimulationLabs.Api.Features.UserActions.Contracts;

public sealed class CreateUserActionRequest
{
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Tipo { get; set; }
    public string? Pagina { get; set; }
    public string? Componente { get; set; }
    public string? EntidadTipo { get; set; }
    public string? EntidadId { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public string? Resultado { get; set; }
    public string? MensajeResultado { get; set; }
    public bool? EraPermitida { get; set; }
    public bool? EraEsperada { get; set; }
    public bool? EsCritica { get; set; }
    public string? Severidad { get; set; }
    public string? MotivoEvaluacion { get; set; }
    public Guid? ExpectedMomentId { get; set; }
    public DateTimeOffset? FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
    public int? DuracionMs { get; set; }
    public string? Origen { get; set; }
}

public sealed class UserActionActionRequest { public string? Motivo { get; set; } }

public sealed class UserActionResponse
{
    public Guid IdUserAction { get; set; }
    public Guid IdVersion { get; set; }
    public Guid IdAttempt { get; set; }
    public int NumeroSecuencia { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Pagina { get; set; }
    public string? Componente { get; set; }
    public string? EntidadTipo { get; set; }
    public string? EntidadId { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? MensajeResultado { get; set; }
    public bool EraPermitida { get; set; }
    public bool EraEsperada { get; set; }
    public bool EsCritica { get; set; }
    public string Severidad { get; set; } = string.Empty;
    public string? MotivoEvaluacion { get; set; }
    public Guid? ExpectedMomentId { get; set; }
    public DateTimeOffset FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
    public int? DuracionMs { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class UserActionListItemResponse
{
    public Guid IdUserAction { get; set; }
    public int NumeroSecuencia { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string Severidad { get; set; } = string.Empty;
    public bool EsCritica { get; set; }
    public Guid? ExpectedMomentId { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class UserActionListResponse
{
    public IReadOnlyList<UserActionListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
