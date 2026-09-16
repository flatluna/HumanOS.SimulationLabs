using HumanOS.SimulationLabs.Api.Features.RubricCriteria.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.RubricCriteria.Validators;

public static class RubricCriterionValidators
{
    public static List<string> ValidateCreate(CreateRubricCriterionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 60)
            errors.Add("codigo es requerido y no puede exceder 60 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200)
            errors.Add("nombre es requerido y no puede exceder 200 caracteres.");

        if (string.IsNullOrWhiteSpace(request.Descripcion))
            errors.Add("descripcion es requerida.");

        if (string.IsNullOrWhiteSpace(request.TipoEvidencia) ||
            !CriterionTipoEvidencia.Allowed.Contains(request.TipoEvidencia.Trim().ToUpperInvariant()))
            errors.Add("tipoEvidencia es requerido y debe ser uno de: CONVERSATION, USER_ACTION, ARTIFACT, DECISION, SYSTEM_RESULT, MULTIPLE.");

        if (!request.Peso.HasValue || request.Peso.Value <= 0)
            errors.Add("peso es requerido y debe ser mayor que cero.");

        if (request.ScoreMinimoEsperado.HasValue && (request.ScoreMinimoEsperado.Value < 1.00m || request.ScoreMinimoEsperado.Value > 10.00m))
            errors.Add("scoreMinimoEsperado debe estar entre 1.00 y 10.00.");

        if (!request.EsCritico.HasValue)
            errors.Add("esCritico es requerido.");

        if (string.IsNullOrWhiteSpace(request.IndicadoresPositivos))
            errors.Add("indicadoresPositivos es requerido y no puede quedar vacío.");

        return errors;
    }

    public static List<string> ValidateUpdate(UpdateRubricCriterionRequest request)
    {
        var errors = new List<string>();

        if (request.Nombre is not null && string.IsNullOrWhiteSpace(request.Nombre))
            errors.Add("nombre no puede quedar vacío.");
        if (request.Nombre is not null && request.Nombre.Trim().Length > 200)
            errors.Add("nombre no puede exceder 200 caracteres.");

        if (request.Descripcion is not null && string.IsNullOrWhiteSpace(request.Descripcion))
            errors.Add("descripcion no puede quedar vacía.");

        if (request.TipoEvidencia is not null &&
            !CriterionTipoEvidencia.Allowed.Contains(request.TipoEvidencia.Trim().ToUpperInvariant()))
            errors.Add("tipoEvidencia debe ser uno de: CONVERSATION, USER_ACTION, ARTIFACT, DECISION, SYSTEM_RESULT, MULTIPLE.");

        if (request.Peso.HasValue && request.Peso.Value <= 0)
            errors.Add("peso debe ser mayor que cero.");

        if (request.ScoreMinimoEsperado.HasValue && (request.ScoreMinimoEsperado.Value < 1.00m || request.ScoreMinimoEsperado.Value > 10.00m))
            errors.Add("scoreMinimoEsperado debe estar entre 1.00 y 10.00.");

        if (request.IndicadoresPositivos is not null && string.IsNullOrWhiteSpace(request.IndicadoresPositivos))
            errors.Add("indicadoresPositivos no puede quedar vacío.");

        return errors;
    }

    public static List<string> ValidateAction(RubricCriterionActionRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Motivo))
            return ["motivo es requerido."];

        if (request.Motivo.Trim().Length > 1000)
            return ["motivo no puede exceder 1000 caracteres."];

        return [];
    }

    public static List<string> ValidateReorder(ReorderRubricCriteriaRequest request)
    {
        var errors = new List<string>();

        if (request.Criteria is null || request.Criteria.Count == 0)
        {
            errors.Add("criteria es requerido y debe contener al menos un elemento.");
            return errors;
        }

        if (request.Criteria.Select(x => x.IdCriterion).Distinct().Count() != request.Criteria.Count)
            errors.Add("No se permiten criterios duplicados en la lista.");

        if (request.Criteria.Select(x => x.Orden).Distinct().Count() != request.Criteria.Count)
            errors.Add("No se permiten órdenes duplicados en la lista.");

        if (request.Criteria.Any(x => x.Orden <= 0))
            errors.Add("Todos los órdenes deben ser mayores que cero.");

        if (request.Criteria.Any(x => string.IsNullOrWhiteSpace(x.RowVersion)))
            errors.Add("RowVersion es requerido para cada elemento.");

        return errors;
    }
}
