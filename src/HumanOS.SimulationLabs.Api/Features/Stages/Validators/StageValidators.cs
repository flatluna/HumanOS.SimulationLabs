using HumanOS.SimulationLabs.Api.Features.Stages.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.Stages.Validators;

public static class StageValidators
{
    public static List<string> ValidateCreate(CreateStageRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 50) errors.Add("codigo es requerido y no puede exceder 50 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200) errors.Add("nombre es requerido y no puede exceder 200 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion es requerida.");
        if (string.IsNullOrWhiteSpace(request.TipoInteraccion) || !StageTipoInteraccion.Allowed.Contains(request.TipoInteraccion.Trim().ToUpperInvariant())) errors.Add("tipoInteraccion no es válido.");
        if (!request.EsObligatorio.HasValue) errors.Add("esObligatorio es requerido.");
        if (!request.PermiteOrdenFlexible.HasValue) errors.Add("permiteOrdenFlexible es requerido.");
        if (request.TiempoSugeridoMinutos.HasValue && request.TiempoSugeridoMinutos <= 0) errors.Add("tiempoSugeridoMinutos debe ser mayor que cero.");
        return errors;
    }

    public static List<string> ValidateUpdate(UpdateStageRequest request)
    {
        var errors = new List<string>();
        if (request.Nombre is null && request.Descripcion is null && request.TipoInteraccion is null && request.EsObligatorio is null && request.CondicionCompletitud is null && request.TiempoSugeridoMinutos is null && request.PermiteOrdenFlexible is null) errors.Add("Debe proporcionar al menos un campo para actualizar.");
        if (request.Nombre is not null && string.IsNullOrWhiteSpace(request.Nombre)) errors.Add("nombre no puede quedar vacío.");
        if (request.Nombre?.Trim().Length > 200) errors.Add("nombre no puede exceder 200 caracteres.");
        if (request.Descripcion is not null && string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion no puede quedar vacía.");
        if (request.TipoInteraccion is not null && !StageTipoInteraccion.Allowed.Contains(request.TipoInteraccion.Trim().ToUpperInvariant())) errors.Add("tipoInteraccion no es válido.");
        if (request.TiempoSugeridoMinutos.HasValue && request.TiempoSugeridoMinutos <= 0) errors.Add("tiempoSugeridoMinutos debe ser mayor que cero.");
        return errors;
    }

    public static List<string> ValidateAction(StageActionRequest request) => string.IsNullOrWhiteSpace(request.Motivo) ? ["motivo es requerido."] : request.Motivo.Trim().Length > 1000 ? ["motivo no puede exceder 1000 caracteres."] : [];
}
