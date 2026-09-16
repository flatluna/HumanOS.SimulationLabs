using HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Contracts;
using HumanOS.SimulationLabs.Entities;

namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder.Validators;

public static class AiLabBuilderValidators
{
    public static List<string> ValidateRequest(GenerateLabDraftRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.TargetRole)) errors.Add("targetRole es requerido.");
        if (string.IsNullOrWhiteSpace(request.LabObjective)) errors.Add("labObjective es requerido.");
        if (string.IsNullOrWhiteSpace(request.ProcessName)) errors.Add("processName es requerido.");

        var hasTypedProcessContent = !string.IsNullOrWhiteSpace(request.ProcessContent);
        var hasProcessPdf = !string.IsNullOrWhiteSpace(request.ProcessFileName) && !string.IsNullOrWhiteSpace(request.ProcessContentBase64);
        if (!hasTypedProcessContent && !hasProcessPdf)
        {
            errors.Add("processContent (o processFileName + processContentBase64) es requerido.");
        }
        if (request.SkillsToEvaluate is null || request.SkillsToEvaluate.Count == 0) errors.Add("skillsToEvaluate debe tener al menos una habilidad.");
        else if (request.SkillsToEvaluate.Any(s => string.IsNullOrWhiteSpace(s.SkillName)))
            errors.Add("Cada skillToEvaluate requiere skillName (whatGoodLooksLike es opcional — el agente lo define si se omite).");
        if (string.IsNullOrWhiteSpace(request.LabLanguage)) errors.Add("labLanguage es requerido.");
        if (string.IsNullOrWhiteSpace(request.Difficulty) || !ScenarioDificultad.Allowed.Contains(request.Difficulty))
            errors.Add($"difficulty debe ser uno de: {string.Join(", ", ScenarioDificultad.Allowed)}.");
        if (request.DurationMinutes <= 0) errors.Add("durationMinutes debe ser mayor que cero.");
        if (request.DialogueCount < 1 || request.DialogueCount > 20) errors.Add("dialogueCount debe estar entre 1 y 20.");

        return errors;
    }

    public static decimal MinScoreFor(string difficulty) => difficulty switch
    {
        ScenarioDificultad.Beginner => 6.0m,
        ScenarioDificultad.Intermediate => 7.0m,
        ScenarioDificultad.Advanced => 8.0m,
        ScenarioDificultad.Expert => 8.0m,
        _ => 7.0m,
    };
}
