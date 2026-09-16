using HumanOS.SimulationLabs.Api.Features.Labs.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.Labs.Validators;

public static class LabValidators
{
    private static readonly string[] AllowedSortBy =
        ["codigo", "nombre", "tipo", "dominio", "estatus", "fechacreacion", "fechaactualizacion"];

    public static List<string> ValidateCreate(CreateLabRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            errors.Add("codigo es requerido.");
        }
        else if (request.Codigo.Trim().Length > 50)
        {
            errors.Add("codigo no puede exceder 50 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add("nombre es requerido.");
        }
        else if (request.Nombre.Trim().Length > 200)
        {
            errors.Add("nombre no puede exceder 200 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.Descripcion))
        {
            errors.Add("descripcion es requerida.");
        }

        if (string.IsNullOrWhiteSpace(request.Tipo) || !LabTipos.Allowed.Contains(request.Tipo))
        {
            errors.Add($"tipo es requerido y debe ser uno de: {string.Join(", ", LabTipos.Allowed)}.");
        }

        if (string.IsNullOrWhiteSpace(request.Dominio))
        {
            errors.Add("dominio es requerido.");
        }
        else if (request.Dominio.Trim().Length > 50)
        {
            errors.Add("dominio no puede exceder 50 caracteres.");
        }

        if (request.OwnerId is null || request.OwnerId == Guid.Empty)
        {
            errors.Add("ownerId es requerido y debe ser distinto de Guid.Empty.");
        }

        return errors;
    }

    public static List<string> ValidateUpdate(UpdateLabRequest request)
    {
        var errors = new List<string>();

        if (request.Nombre is not null && string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors.Add("nombre no puede quedar vacío.");
        }

        if (request.Descripcion is not null && string.IsNullOrWhiteSpace(request.Descripcion))
        {
            errors.Add("descripcion no puede quedar vacía.");
        }

        if (request.Tipo is not null && !LabTipos.Allowed.Contains(request.Tipo))
        {
            errors.Add($"tipo debe ser uno de: {string.Join(", ", LabTipos.Allowed)}.");
        }

        if (request.Dominio is not null && string.IsNullOrWhiteSpace(request.Dominio))
        {
            errors.Add("dominio no puede quedar vacío.");
        }

        if (request.OwnerId is not null && request.OwnerId == Guid.Empty)
        {
            errors.Add("ownerId no puede ser Guid.Empty.");
        }

        return errors;
    }

    public static List<string> ValidateInactivate(InactivateLabRequest request)
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

    public static List<string> ValidateListQuery(LabListQuery query)
    {
        var errors = new List<string>();

        if (query.Page < 1)
        {
            errors.Add("page debe ser mayor que cero.");
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            errors.Add("pageSize debe estar entre 1 y 100.");
        }

        if (query.Tipo is not null && !LabTipos.Allowed.Contains(query.Tipo))
        {
            errors.Add($"tipo debe ser uno de: {string.Join(", ", LabTipos.Allowed)}.");
        }

        if (query.Estatus is not null && !LabEstatus.Allowed.Contains(query.Estatus))
        {
            errors.Add($"estatus debe ser uno de: {string.Join(", ", LabEstatus.Allowed)}.");
        }

        if (query.SortBy is not null && !AllowedSortBy.Contains(query.SortBy.ToLowerInvariant()))
        {
            errors.Add($"sortBy debe ser uno de: {string.Join(", ", AllowedSortBy)}.");
        }

        if (query.SortDirection is not null &&
            !string.Equals(query.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(query.SortDirection, "DESC", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("sortDirection debe ser ASC o DESC.");
        }

        return errors;
    }
}
