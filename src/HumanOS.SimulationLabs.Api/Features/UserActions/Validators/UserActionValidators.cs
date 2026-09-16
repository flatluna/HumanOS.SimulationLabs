using System.Text.Json;
using HumanOS.SimulationLabs.Api.Features.UserActions.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.UserActions.Validators;

public static class UserActionValidators
{
    public static List<string> ValidateCreate(CreateUserActionRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 50) errors.Add("codigo es requerido y no puede exceder 50 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200) errors.Add("nombre es requerido y no puede exceder 200 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Tipo) || !UserActionTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (string.IsNullOrWhiteSpace(request.Resultado) || !UserActionResultado.Allowed.Contains(request.Resultado.Trim().ToUpperInvariant())) errors.Add("resultado no es válido.");
        if (string.IsNullOrWhiteSpace(request.Severidad) || !UserActionSeveridad.Allowed.Contains(request.Severidad.Trim().ToUpperInvariant())) errors.Add("severidad no es válida.");
        if (string.IsNullOrWhiteSpace(request.Origen) || !UserActionOrigen.Allowed.Contains(request.Origen.Trim().ToUpperInvariant())) errors.Add("origen no es válido.");
        if (!IsValidJson(request.InputJson)) errors.Add("inputJson no es un JSON válido.");
        if (!IsValidJson(request.OutputJson)) errors.Add("outputJson no es un JSON válido.");
        return errors;
    }

    public static List<string> ValidateAction(UserActionActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo) ? [] : request.Motivo.Trim().Length > 1000 ? ["motivo no puede exceder 1000 caracteres."] : [];

    private static bool IsValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        try { using var _ = JsonDocument.Parse(value); return true; } catch (JsonException) { return false; }
    }
}
