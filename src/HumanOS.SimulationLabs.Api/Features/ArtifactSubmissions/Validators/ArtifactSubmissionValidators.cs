using System.Text.Json;
using HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions.Validators;

public static class ArtifactSubmissionValidators
{
    public static List<string> ValidateCreate(CreateArtifactSubmissionRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CodigoArtefacto) || request.CodigoArtefacto.Trim().Length > 50) errors.Add("codigoArtefacto es requerido y no puede exceder 50 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200) errors.Add("nombre es requerido y no puede exceder 200 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Tipo) || !ArtifactSubmissionTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (string.IsNullOrWhiteSpace(request.Formato) || !ArtifactSubmissionFormato.Allowed.Contains(request.Formato.Trim().ToUpperInvariant())) errors.Add("formato no es válido.");
        if (request.TipoAsistencia is not null && !ArtifactSubmissionTipoAsistencia.Allowed.Contains(request.TipoAsistencia.Trim().ToUpperInvariant())) errors.Add("tipoAsistencia no es válido.");
        if (!IsValidJson(request.ContenidoJson)) errors.Add("contenidoJson no es un JSON válido.");
        return errors;
    }

    public static List<string> ValidateUpdate(UpdateArtifactSubmissionRequest request)
    {
        var errors = new List<string>();
        if (request.Nombre is null && request.ContenidoTexto is null && request.ContenidoJson is null && request.BlobPath is null &&
            request.NombreArchivo is null && request.MimeType is null && request.RequiereEvaluacion is null &&
            request.FueGeneradoConAsistencia is null && request.TipoAsistencia is null)
        {
            errors.Add("Debe proporcionar al menos un campo para actualizar.");
        }
        if (request.Nombre is not null && string.IsNullOrWhiteSpace(request.Nombre)) errors.Add("nombre no puede quedar vacío.");
        if (request.TipoAsistencia is not null && !ArtifactSubmissionTipoAsistencia.Allowed.Contains(request.TipoAsistencia.Trim().ToUpperInvariant())) errors.Add("tipoAsistencia no es válido.");
        if (!IsValidJson(request.ContenidoJson)) errors.Add("contenidoJson no es un JSON válido.");
        return errors;
    }

    public static List<string> ValidateAction(ArtifactSubmissionActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo) ? [] : request.Motivo.Trim().Length > 1000 ? ["motivo no puede exceder 1000 caracteres."] : [];

    private static bool IsValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        try { using var _ = JsonDocument.Parse(value); return true; } catch (JsonException) { return false; }
    }
}
