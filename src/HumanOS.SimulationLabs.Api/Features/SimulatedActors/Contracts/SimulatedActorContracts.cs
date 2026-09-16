namespace HumanOS.SimulationLabs.Api.Features.SimulatedActors.Contracts;

public sealed class CreateSimulatedActorRequest
{
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Rol { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public string? Objetivo { get; set; }
    public string? ContextoConocido { get; set; }
    public string? BriefOculto { get; set; }
    public string? InformacionPuedeRevelar { get; set; }
    public string? InformacionNoRevelarAutomaticamente { get; set; }
    public string? Restricciones { get; set; }
    public string? Objeciones { get; set; }
    public string? Contradicciones { get; set; }
    public string? EstiloComunicacion { get; set; }
    public string? NivelConocimiento { get; set; }
    public string? Idioma { get; set; }
    public string? VoiceName { get; set; }
    public string? MensajeInicial { get; set; }
    public bool? PuedeIniciarConversacion { get; set; }
    public bool? EsPrincipal { get; set; }
}

public sealed class UpdateSimulatedActorRequest
{
    public string? Nombre { get; set; }
    public string? Rol { get; set; }
    public string? Tipo { get; set; }
    public string? Descripcion { get; set; }
    public string? Objetivo { get; set; }
    public string? ContextoConocido { get; set; }
    public string? BriefOculto { get; set; }
    public string? InformacionPuedeRevelar { get; set; }
    public string? InformacionNoRevelarAutomaticamente { get; set; }
    public string? Restricciones { get; set; }
    public string? Objeciones { get; set; }
    public string? Contradicciones { get; set; }
    public string? EstiloComunicacion { get; set; }
    public string? NivelConocimiento { get; set; }
    public string? Idioma { get; set; }
    public string? VoiceName { get; set; }
    public string? MensajeInicial { get; set; }
    public bool? PuedeIniciarConversacion { get; set; }
    public bool? EsPrincipal { get; set; }
}

public sealed class ReorderSimulatedActorsRequest { public List<ReorderSimulatedActorItem>? Actors { get; set; } }

public sealed class ReorderSimulatedActorItem
{
    public Guid IdActor { get; set; }
    public int Orden { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class SimulatedActorActionRequest { public string? Motivo { get; set; } }

public sealed class SimulatedActorResponse
{
    public Guid IdActor { get; set; }
    public Guid IdVersion { get; set; }
    public Guid IdScenario { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public string ContextoConocido { get; set; } = string.Empty;
    public string? BriefOculto { get; set; }
    public string? InformacionPuedeRevelar { get; set; }
    public string? InformacionNoRevelarAutomaticamente { get; set; }
    public string? Restricciones { get; set; }
    public string? Objeciones { get; set; }
    public string? Contradicciones { get; set; }
    public string EstiloComunicacion { get; set; } = string.Empty;
    public string NivelConocimiento { get; set; } = string.Empty;
    public string Idioma { get; set; } = string.Empty;
    public string? VoiceName { get; set; }
    public string? MensajeInicial { get; set; }
    public bool PuedeIniciarConversacion { get; set; }
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class SimulatedActorListItemResponse
{
    public Guid IdActor { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string EstiloComunicacion { get; set; } = string.Empty;
    public string NivelConocimiento { get; set; } = string.Empty;
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class SimulatedActorListResponse
{
    public IReadOnlyList<SimulatedActorListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ReorderSimulatedActorsResponse { public IReadOnlyList<ReorderSimulatedActorResponseItem> Items { get; set; } = []; }

public sealed class ReorderSimulatedActorResponseItem
{
    public Guid IdActor { get; set; }
    public int Orden { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
