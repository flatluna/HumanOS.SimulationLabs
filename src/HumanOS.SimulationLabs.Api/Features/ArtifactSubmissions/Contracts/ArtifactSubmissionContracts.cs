namespace HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Contracts;

public sealed class CreateArtifactSubmissionRequest
{
    public Guid? StageId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public string? CodigoArtefacto { get; set; }
    public string? Nombre { get; set; }
    public string? Tipo { get; set; }
    public string? Formato { get; set; }
    public string? ContenidoTexto { get; set; }
    public string? ContenidoJson { get; set; }
    public string? BlobPath { get; set; }
    public string? NombreArchivo { get; set; }
    public string? MimeType { get; set; }
    public bool? RequiereEvaluacion { get; set; }
    public bool? FueGeneradoConAsistencia { get; set; }
    public string? TipoAsistencia { get; set; }
}

public sealed class UpdateArtifactSubmissionRequest
{
    public string? Nombre { get; set; }
    public string? ContenidoTexto { get; set; }
    public string? ContenidoJson { get; set; }
    public string? BlobPath { get; set; }
    public string? NombreArchivo { get; set; }
    public string? MimeType { get; set; }
    public bool? RequiereEvaluacion { get; set; }
    public bool? FueGeneradoConAsistencia { get; set; }
    public string? TipoAsistencia { get; set; }
}

public sealed class ArtifactSubmissionActionRequest { public string? Motivo { get; set; } }

public sealed class ArtifactSubmissionResponse
{
    public Guid IdSubmission { get; set; }
    public Guid IdVersion { get; set; }
    public Guid IdAttempt { get; set; }
    public Guid? StageId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public string CodigoArtefacto { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Formato { get; set; } = string.Empty;
    public int Version { get; set; }
    public string? ContenidoTexto { get; set; }
    public string? ContenidoJson { get; set; }
    public string? BlobPath { get; set; }
    public string? NombreArchivo { get; set; }
    public string? MimeType { get; set; }
    public string? HashSHA256 { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset? FechaInicio { get; set; }
    public DateTimeOffset? FechaEnvio { get; set; }
    public bool EsEntregaFinal { get; set; }
    public bool RequiereEvaluacion { get; set; }
    public bool FueGeneradoConAsistencia { get; set; }
    public string? TipoAsistencia { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ArtifactSubmissionListItemResponse
{
    public Guid IdSubmission { get; set; }
    public string CodigoArtefacto { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Formato { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public bool EsEntregaFinal { get; set; }
    public Guid? StageId { get; set; }
    public Guid? ObjectiveId { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class ArtifactSubmissionListResponse
{
    public IReadOnlyList<ArtifactSubmissionListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
