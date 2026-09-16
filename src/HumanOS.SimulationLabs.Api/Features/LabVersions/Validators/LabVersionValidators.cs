using HumanOS.SimulationLabs.Api.Features.LabVersions.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.LabVersions.Validators;

public static class LabVersionValidators
{
    private static readonly string[] AllowedSortDirections = ["ASC", "DESC"];

    public static List<string> ValidateCreate(CreateLabVersionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ObjetivoGeneral))
        {
            errors.Add("objetivoGeneral es requerido.");
        }

        if (string.IsNullOrWhiteSpace(request.InstruccionesParticipante))
        {
            errors.Add("instruccionesParticipante es requerido.");
        }

        if (request.DuracionMinutos is null || request.DuracionMinutos <= 0)
        {
            errors.Add("duracionMinutos es requerido y debe ser mayor que cero.");
        }

        if (request.ScoreMinimo is null || request.ScoreMinimo < 1.00m || request.ScoreMinimo > 10.00m)
        {
            errors.Add("scoreMinimo es requerido y debe estar entre 1.00 y 10.00.");
        }

        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaHasta < request.VigenciaDesde)
        {
            errors.Add("vigenciaHasta no puede ser anterior a vigenciaDesde.");
        }

        return errors;
    }

    public static List<string> ValidateUpdate(UpdateLabVersionRequest request)
    {
        var errors = new List<string>();

        var hasAnyField = request.ObjetivoGeneral is not null ||
                          request.InstruccionesParticipante is not null ||
                          request.BriefOculto is not null ||
                          request.DuracionMinutos.HasValue ||
                          request.ScoreMinimo.HasValue ||
                          request.VigenciaDesde.HasValue ||
                          request.VigenciaHasta.HasValue;

        if (!hasAnyField)
        {
            errors.Add("Debe proporcionar al menos un campo para actualizar.");
            return errors;
        }

        if (request.ObjetivoGeneral is not null && string.IsNullOrWhiteSpace(request.ObjetivoGeneral))
        {
            errors.Add("objetivoGeneral no puede quedar vacío.");
        }

        if (request.InstruccionesParticipante is not null && string.IsNullOrWhiteSpace(request.InstruccionesParticipante))
        {
            errors.Add("instruccionesParticipante no puede quedar vacío.");
        }

        if (request.DuracionMinutos.HasValue && request.DuracionMinutos.Value <= 0)
        {
            errors.Add("duracionMinutos debe ser mayor que cero.");
        }

        if (request.ScoreMinimo.HasValue && (request.ScoreMinimo.Value < 1.00m || request.ScoreMinimo.Value > 10.00m))
        {
            errors.Add("scoreMinimo debe estar entre 1.00 y 10.00.");
        }

        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaHasta < request.VigenciaDesde)
        {
            errors.Add("vigenciaHasta no puede ser anterior a vigenciaDesde.");
        }

        return errors;
    }

    public static List<string> ValidateApprove(ApproveLabVersionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            errors.Add("motivo es requerido.");
        }
        else if (request.Motivo.Trim().Length > 1000)
        {
            errors.Add("motivo no puede exceder 1000 caracteres.");
        }

        return errors;
    }

    public static List<string> ValidatePublish(PublishLabVersionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            errors.Add("motivo es requerido.");
        }
        else if (request.Motivo.Trim().Length > 1000)
        {
            errors.Add("motivo no puede exceder 1000 caracteres.");
        }

        if (!request.VigenciaDesde.HasValue)
        {
            errors.Add("vigenciaDesde es requerida para publicar.");
        }

        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaHasta < request.VigenciaDesde)
        {
            errors.Add("vigenciaHasta no puede ser anterior a vigenciaDesde.");
        }

        return errors;
    }

    public static List<string> ValidateRetire(RetireLabVersionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Motivo))
        {
            errors.Add("motivo es requerido.");
        }
        else if (request.Motivo.Trim().Length > 1000)
        {
            errors.Add("motivo no puede exceder 1000 caracteres.");
        }

        return errors;
    }

    public static List<string> ValidateListQuery(LabVersionListQuery query)
    {
        var errors = new List<string>();

        if (query.Page <= 0)
        {
            errors.Add("page debe ser mayor que cero.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            errors.Add("pageSize debe estar entre 1 y 100.");
        }

        if (!string.IsNullOrWhiteSpace(query.Estatus) && !LabVersionEstatus.Allowed.Contains(query.Estatus))
        {
            errors.Add($"estatus debe ser uno de: {string.Join(", ", LabVersionEstatus.Allowed)}.");
        }

        if (!string.IsNullOrWhiteSpace(query.SortDirection) &&
            !AllowedSortDirections.Contains(query.SortDirection.ToUpperInvariant()))
        {
            errors.Add("sortDirection debe ser ASC o DESC.");
        }

        return errors;
    }
}
