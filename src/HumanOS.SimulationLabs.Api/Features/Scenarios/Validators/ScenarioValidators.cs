using HumanOS.SimulationLabs.Api.Features.Scenarios.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.Scenarios.Validators;

public static class ScenarioValidators
{
    public static List<string> ValidateCreate(CreateScenarioRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 50) errors.Add("codigo es requerido y no puede exceder 50 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200) errors.Add("nombre es requerido y no puede exceder 200 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion es requerida.");
        if (string.IsNullOrWhiteSpace(request.Tipo) || !ScenarioTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (string.IsNullOrWhiteSpace(request.Dificultad) || !ScenarioDificultad.Allowed.Contains(request.Dificultad.Trim().ToUpperInvariant())) errors.Add("dificultad no es válida.");
        if (string.IsNullOrWhiteSpace(request.ContextoParticipante)) errors.Add("contextoParticipante es requerido.");
        if (string.IsNullOrWhiteSpace(request.BriefOculto)) errors.Add("briefOculto es requerido.");
        if (string.IsNullOrWhiteSpace(request.ProblemaCentral)) errors.Add("problemaCentral es requerido.");
        if (string.IsNullOrWhiteSpace(request.ResultadoEsperado)) errors.Add("resultadoEsperado es requerido.");
        if (request.DuracionSugeridaMinutos.HasValue && request.DuracionSugeridaMinutos <= 0) errors.Add("duracionSugeridaMinutos debe ser mayor que cero.");
        if (request.MaximoIntentos.HasValue && request.MaximoIntentos <= 0) errors.Add("maximoIntentos debe ser mayor que cero.");
        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaDesde > request.VigenciaHasta) errors.Add("vigenciaDesde no puede ser posterior a vigenciaHasta.");
        return errors;
    }

    public static List<string> ValidateUpdate(UpdateScenarioRequest request)
    {
        var errors = new List<string>();
        if (request.Nombre is null && request.Descripcion is null && request.Tipo is null && request.Dificultad is null && request.ContextoParticipante is null &&
            request.BriefOculto is null && request.ProblemaCentral is null && request.ResultadoEsperado is null && request.CondicionesIniciales is null &&
            request.Restricciones is null && request.Supuestos is null && request.Riesgos is null && request.InformacionNoRevelarAutomaticamente is null &&
            request.MensajeInicial is null && request.DuracionSugeridaMinutos is null && request.PuntuacionObjetivo is null && request.PermiteReintento is null &&
            request.MaximoIntentos is null && request.UsaVariacion is null && request.SeedBase is null && request.VigenciaDesde is null && request.VigenciaHasta is null)
        {
            errors.Add("Debe proporcionar al menos un campo para actualizar.");
        }
        if (request.Nombre is not null && string.IsNullOrWhiteSpace(request.Nombre)) errors.Add("nombre no puede quedar vacío.");
        if (request.Nombre?.Trim().Length > 200) errors.Add("nombre no puede exceder 200 caracteres.");
        if (request.Descripcion is not null && string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion no puede quedar vacía.");
        if (request.Tipo is not null && !ScenarioTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (request.Dificultad is not null && !ScenarioDificultad.Allowed.Contains(request.Dificultad.Trim().ToUpperInvariant())) errors.Add("dificultad no es válida.");
        if (request.DuracionSugeridaMinutos.HasValue && request.DuracionSugeridaMinutos <= 0) errors.Add("duracionSugeridaMinutos debe ser mayor que cero.");
        if (request.MaximoIntentos.HasValue && request.MaximoIntentos <= 0) errors.Add("maximoIntentos debe ser mayor que cero.");
        if (request.VigenciaDesde.HasValue && request.VigenciaHasta.HasValue && request.VigenciaDesde > request.VigenciaHasta) errors.Add("vigenciaDesde no puede ser posterior a vigenciaHasta.");
        return errors;
    }

    public static List<string> ValidateAction(ScenarioActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo) ? ["motivo es requerido."] : request.Motivo.Trim().Length > 1000 ? ["motivo no puede exceder 1000 caracteres."] : [];
}
