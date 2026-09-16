namespace HumanOS.SimulationLabs.Api.Features.ConversationTurns.Contracts;

public sealed class CreateConversationTurnRequest
{
    public string? SpeakerType { get; set; }
    public Guid? ActorId { get; set; }
    public string? Texto { get; set; }
    public string? TipoEntrada { get; set; }
    public DateTimeOffset? FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
    public int? DuracionMs { get; set; }
    public decimal? ConfianzaTranscripcion { get; set; }
    public bool? FueInterrumpido { get; set; }
    public bool? InterrumpioTurnoAnterior { get; set; }
    public bool? EsRespuesta { get; set; }
    public Guid? IdTurnoRespondido { get; set; }
    public Guid? IdExpectedMoment { get; set; }
}

public sealed class CorrectConversationTurnRequest
{
    public string? CorrectedText { get; set; }
    public string? Reason { get; set; }
}

public sealed class ConversationTurnActionRequest { public string? Motivo { get; set; } }

public sealed class ConversationTurnResponse
{
    public Guid IdTurn { get; set; }
    public Guid IdAttempt { get; set; }
    public Guid IdVersion { get; set; }
    public int NumeroTurno { get; set; }
    public string SpeakerType { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string TipoEntrada { get; set; } = string.Empty;
    public DateTimeOffset FechaInicio { get; set; }
    public DateTimeOffset? FechaFin { get; set; }
    public int? DuracionMs { get; set; }
    public decimal? ConfianzaTranscripcion { get; set; }
    public bool FueInterrumpido { get; set; }
    public bool InterrumpioTurnoAnterior { get; set; }
    public bool EsRespuesta { get; set; }
    public Guid? IdTurnoRespondido { get; set; }
    public Guid? IdExpectedMoment { get; set; }
    public bool TextoFueEditado { get; set; }
    public string? TextoOriginal { get; set; }
    public string? EditadoPor { get; set; }
    public DateTimeOffset? FechaEdicion { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ConversationTurnListResponse
{
    public IReadOnlyList<ConversationTurnResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
