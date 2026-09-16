using HumanOS.SimulationLabs.Api.Features.SimulatedActors.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.SimulatedActors.Validators;

public static class SimulatedActorValidators
{
    public static List<string> ValidateCreate(CreateSimulatedActorRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > 50) errors.Add("codigo es requerido y no puede exceder 50 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 200) errors.Add("nombre es requerido y no puede exceder 200 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Rol)) errors.Add("rol es requerido.");
        if (string.IsNullOrWhiteSpace(request.Tipo) || !ActorTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (string.IsNullOrWhiteSpace(request.Descripcion)) errors.Add("descripcion es requerida.");
        if (string.IsNullOrWhiteSpace(request.Objetivo)) errors.Add("objetivo es requerido.");
        if (string.IsNullOrWhiteSpace(request.ContextoConocido)) errors.Add("contextoConocido es requerido.");
        if (string.IsNullOrWhiteSpace(request.BriefOculto)) errors.Add("briefOculto es requerido.");
        if (string.IsNullOrWhiteSpace(request.EstiloComunicacion) || !ActorEstiloComunicacion.Allowed.Contains(request.EstiloComunicacion.Trim().ToUpperInvariant())) errors.Add("estiloComunicacion no es válido.");
        if (string.IsNullOrWhiteSpace(request.NivelConocimiento) || !ActorNivelConocimiento.Allowed.Contains(request.NivelConocimiento.Trim().ToUpperInvariant())) errors.Add("nivelConocimiento no es válido.");
        if (string.IsNullOrWhiteSpace(request.Idioma)) errors.Add("idioma es requerido.");
        return errors;
    }

    public static List<string> ValidateUpdate(UpdateSimulatedActorRequest request)
    {
        var errors = new List<string>();
        if (request.Nombre is null && request.Rol is null && request.Tipo is null && request.Descripcion is null && request.Objetivo is null &&
            request.ContextoConocido is null && request.BriefOculto is null && request.InformacionPuedeRevelar is null && request.InformacionNoRevelarAutomaticamente is null &&
            request.Restricciones is null && request.Objeciones is null && request.Contradicciones is null && request.EstiloComunicacion is null &&
            request.NivelConocimiento is null && request.Idioma is null && request.VoiceName is null && request.MensajeInicial is null &&
            request.PuedeIniciarConversacion is null && request.EsPrincipal is null)
        {
            errors.Add("Debe proporcionar al menos un campo para actualizar.");
        }
        if (request.Nombre is not null && string.IsNullOrWhiteSpace(request.Nombre)) errors.Add("nombre no puede quedar vacío.");
        if (request.Nombre?.Trim().Length > 200) errors.Add("nombre no puede exceder 200 caracteres.");
        if (request.Tipo is not null && !ActorTipo.Allowed.Contains(request.Tipo.Trim().ToUpperInvariant())) errors.Add("tipo no es válido.");
        if (request.EstiloComunicacion is not null && !ActorEstiloComunicacion.Allowed.Contains(request.EstiloComunicacion.Trim().ToUpperInvariant())) errors.Add("estiloComunicacion no es válido.");
        if (request.NivelConocimiento is not null && !ActorNivelConocimiento.Allowed.Contains(request.NivelConocimiento.Trim().ToUpperInvariant())) errors.Add("nivelConocimiento no es válido.");
        return errors;
    }

    public static List<string> ValidateAction(SimulatedActorActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo) ? ["motivo es requerido."] : request.Motivo.Trim().Length > 1000 ? ["motivo no puede exceder 1000 caracteres."] : [];
}
