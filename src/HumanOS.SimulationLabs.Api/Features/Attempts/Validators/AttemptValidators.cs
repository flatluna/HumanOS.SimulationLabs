using HumanOS.SimulationLabs.Api.Features.Attempts.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.Attempts.Validators;

public static class AttemptValidators
{
    public static List<string> ValidateStart(StartAttemptRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Idioma)) errors.Add("idioma es requerido.");
        if (string.IsNullOrWhiteSpace(request.Modalidad) || !AttemptModalidad.Allowed.Contains(request.Modalidad.Trim().ToUpperInvariant())) errors.Add("modalidad no es válida.");
        return errors;
    }

    public static List<string> ValidateAction(AttemptActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo) ? [] : request.Motivo.Trim().Length > 1000 ? ["motivo no puede exceder 1000 caracteres."] : [];
}
