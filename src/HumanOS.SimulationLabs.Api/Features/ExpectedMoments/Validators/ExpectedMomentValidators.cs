namespace HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Validators;

using HumanOS.SimulationLabs.Api.Features.ExpectedMoments.Contracts;
using HumanOS.SimulationLabs.Entities;

public static class ExpectedMomentValidators
{
    public static List<string> ValidateCreate(CreateExpectedMomentRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 60) errors.Add("codigo es requerido y no puede exceder 60 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200) errors.Add("nombre es requerido y no puede exceder 200 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Tipo) || !MomentTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (string.IsNullOrWhiteSpace(request.Trigger)) errors.Add("trigger es requerido.");
        if (string.IsNullOrWhiteSpace(request.IntencionEsperada)) errors.Add("intencionEsperada es requerida.");
        if (!request.EsCritico.HasValue) errors.Add("esCritico es requerido.");
        if (!request.PermiteOrdenFlexible.HasValue) errors.Add("permiteOrdenFlexible es requerido.");
        if (!request.RequiereRespuesta.HasValue) errors.Add("requiereRespuesta es requerido.");
        return errors;
    }

    public static List<string> ValidateUpdate(UpdateExpectedMomentRequest request)
    {
        var errors = new List<string>();
        if (request.Nombre is not null && (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200)) errors.Add("nombre no puede quedar vacío ni exceder 200 caracteres.");
        if (request.Tipo is not null && !MomentTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (request.Trigger is not null && string.IsNullOrWhiteSpace(request.Trigger)) errors.Add("trigger no puede quedar vacío.");
        if (request.IntencionEsperada is not null && string.IsNullOrWhiteSpace(request.IntencionEsperada)) errors.Add("intencionEsperada no puede quedar vacía.");
        return errors;
    }

    public static List<string> ValidateAction(ExpectedMomentActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo)
            ? ["motivo es requerido."]
            : request.Motivo.Trim().Length > 1000
                ? ["motivo no puede exceder 1000 caracteres."]
                : [];

    public static List<string> ValidateReorder(ReorderExpectedMomentsRequest request)
    {
        var errors = new List<string>();
        if (request.Moments is null || request.Moments.Count == 0)
        {
            errors.Add("moments es requerido y debe contener al menos un elemento.");
            return errors;
        }

        if (request.Moments.Any(m => m.IdExpectedMoment == Guid.Empty || string.IsNullOrWhiteSpace(m.RowVersion) || m.Orden <= 0))
        {
            errors.Add("Cada elemento debe tener idExpectedMoment, rowVersion y un orden mayor que cero.");
        }

        if (request.Moments.Select(m => m.IdExpectedMoment).Distinct().Count() != request.Moments.Count)
        {
            errors.Add("No se permiten idExpectedMoment repetidos.");
        }

        if (request.Moments.Select(m => m.Orden).Distinct().Count() != request.Moments.Count)
        {
            errors.Add("No se permiten valores de orden repetidos.");
        }

        return errors;
    }
}
