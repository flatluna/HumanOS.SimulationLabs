using HumanOS.SimulationLabs.Api.Features.ConversationTurns.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.ConversationTurns.Validators;

public static class ConversationTurnValidators
{
    public static List<string> ValidateCreate(CreateConversationTurnRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.SpeakerType) || !TurnSpeakerType.Allowed.Contains(request.SpeakerType.Trim().ToUpperInvariant())) errors.Add("speakerType no es válido.");
        if (string.IsNullOrWhiteSpace(request.Texto)) errors.Add("texto es requerido.");
        if (string.IsNullOrWhiteSpace(request.TipoEntrada) || !TurnTipoEntrada.Allowed.Contains(request.TipoEntrada.Trim().ToUpperInvariant())) errors.Add("tipoEntrada no es válido.");
        if (string.Equals(request.SpeakerType?.Trim().ToUpperInvariant(), TurnSpeakerType.SimulatedActor, StringComparison.Ordinal) && request.ActorId is null) errors.Add("actorId es requerido cuando speakerType es SIMULATED_ACTOR.");
        return errors;
    }

    public static List<string> ValidateCorrect(CorrectConversationTurnRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CorrectedText)) errors.Add("correctedText es requerido.");
        if (string.IsNullOrWhiteSpace(request.Reason)) errors.Add("reason es requerido.");
        return errors;
    }

    public static List<string> ValidateAction(ConversationTurnActionRequest request) =>
        string.IsNullOrWhiteSpace(request.Motivo) ? [] : request.Motivo.Trim().Length > 1000 ? ["motivo no puede exceder 1000 caracteres."] : [];
}
