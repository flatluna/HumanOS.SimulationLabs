using HumanOS.SimulationLabs.Api.Features.Objectives.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.Objectives.Validators;

public static class ObjectiveValidators
{
    public static List<string> ValidateCreate(CreateObjectiveRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 50) errors.Add("codigo es requerido y no puede exceder 50 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion es requerida.");
        if (string.IsNullOrWhiteSpace(request.TipoEvidencia) || !ObjectiveTipoEvidencia.Allowed.Contains(request.TipoEvidencia.Trim().ToUpperInvariant())) errors.Add("tipoEvidencia no es válido.");
        if (!request.EsCritico.HasValue) errors.Add("esCritico es requerido.");
        if (!request.Peso.HasValue || request.Peso.Value <= 0) errors.Add("peso es requerido y debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(request.CondicionExito)) errors.Add("condicionExito es requerida.");
        return errors;
    }

    public static List<string> ValidateUpdate(UpdateObjectiveRequest request)
    {
        var errors = new List<string>();
        if (request.Descripcion is null && request.TipoEvidencia is null && request.EsCritico is null && request.Peso is null && request.CondicionExito is null)
        {
            errors.Add("Debe proporcionar al menos un campo para actualizar.");
        }
        if (request.Descripcion is not null && string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion no puede quedar vacía.");
        if (request.TipoEvidencia is not null && !ObjectiveTipoEvidencia.Allowed.Contains(request.TipoEvidencia.Trim().ToUpperInvariant())) errors.Add("tipoEvidencia no es válido.");
        if (request.Peso.HasValue && request.Peso.Value <= 0) errors.Add("peso debe ser mayor que cero.");
        if (request.CondicionExito is not null && string.IsNullOrWhiteSpace(request.CondicionExito)) errors.Add("condicionExito no puede quedar vacía.");
        return errors;
    }

    public static List<string> ValidateAction(ObjectiveActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo)
            ? ["motivo es requerido."]
            : request.Motivo.Trim().Length > 1000
                ? ["motivo no puede exceder 1000 caracteres."]
                : [];
}
