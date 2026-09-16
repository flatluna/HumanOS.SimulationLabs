namespace HumanOS.SimulationLabs.Api.Features.Rubrics.Validators;

using HumanOS.SimulationLabs.Api.Features.Rubrics.Contracts;
using HumanOS.SimulationLabs.Entities;

public static class RubricValidators
{
    public static List<string> ValidateCreate(CreateRubricRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 60) errors.Add("codigo es requerido y no puede exceder 60 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200) errors.Add("nombre es requerido y no puede exceder 200 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion es requerida.");
        if (string.IsNullOrWhiteSpace(request.TipoEvaluacion) || !RubricTipoEvaluacion.Allowed.Contains(request.TipoEvaluacion.Trim().ToUpperInvariant())) errors.Add("tipoEvaluacion no es válido.");
        if (!request.ScoreMinimoAprobacion.HasValue || request.ScoreMinimoAprobacion.Value < 1.00m || request.ScoreMinimoAprobacion.Value > 10.00m) errors.Add("scoreMinimoAprobacion es requerido y debe estar entre 1.00 y 10.00.");
        if (!request.RequiereEvidencia.HasValue) errors.Add("requiereEvidencia es requerido.");
        if (!request.PermiteFallaCritica.HasValue) errors.Add("permiteFallaCritica es requerido.");
        if (string.IsNullOrWhiteSpace(request.MetodoCalculo) || !RubricMetodoCalculo.Allowed.Contains(request.MetodoCalculo.Trim().ToUpperInvariant())) errors.Add("metodoCalculo no es válido.");
        if (string.IsNullOrWhiteSpace(request.InstruccionesEvaluador)) errors.Add("instruccionesEvaluador es requerida.");
        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaHasta.Value < request.VigenciaDesde.Value) errors.Add("vigenciaHasta no puede ser anterior a vigenciaDesde.");
        return errors;
    }

    public static List<string> ValidateUpdate(UpdateRubricRequest request)
    {
        var errors = new List<string>();
        if (request.Nombre is not null && string.IsNullOrWhiteSpace(request.Nombre)) errors.Add("nombre no puede quedar vacío.");
        if (request.Nombre is not null && request.Nombre.Trim().Length > 200) errors.Add("nombre no puede exceder 200 caracteres.");
        if (request.Descripcion is not null && string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion no puede quedar vacía.");
        if (request.TipoEvaluacion is not null && !RubricTipoEvaluacion.Allowed.Contains(request.TipoEvaluacion.Trim().ToUpperInvariant())) errors.Add("tipoEvaluacion no es válido.");
        if (request.ScoreMinimoAprobacion.HasValue && (request.ScoreMinimoAprobacion.Value < 1.00m || request.ScoreMinimoAprobacion.Value > 10.00m)) errors.Add("scoreMinimoAprobacion debe estar entre 1.00 y 10.00.");
        if (request.MetodoCalculo is not null && !RubricMetodoCalculo.Allowed.Contains(request.MetodoCalculo.Trim().ToUpperInvariant())) errors.Add("metodoCalculo no es válido.");
        if (request.InstruccionesEvaluador is not null && string.IsNullOrWhiteSpace(request.InstruccionesEvaluador)) errors.Add("instruccionesEvaluador no puede quedar vacía.");
        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaHasta.Value < request.VigenciaDesde.Value) errors.Add("vigenciaHasta no puede ser anterior a vigenciaDesde.");
        return errors;
    }

    public static List<string> ValidateAction(string? motivo) =>
        string.IsNullOrWhiteSpace(motivo)
            ? ["motivo es requerido."]
            : motivo.Trim().Length > 1000
                ? ["motivo no puede exceder 1000 caracteres."]
                : [];

    public static List<string> ValidatePublish(PublishRubricRequest request)
    {
        var errors = ValidateAction(request.Motivo);
        if (!request.VigenciaDesde.HasValue) errors.Add("vigenciaDesde es requerida.");
        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaHasta.Value < request.VigenciaDesde.Value) errors.Add("vigenciaHasta no puede ser anterior a vigenciaDesde.");
        return errors;
    }

    public static List<string> ValidateRetire(RetireRubricRequest request) => ValidateAction(request.Motivo);
}
