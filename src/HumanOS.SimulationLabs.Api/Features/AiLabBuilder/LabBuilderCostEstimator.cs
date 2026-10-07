using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace HumanOS.SimulationLabs.Api.Features.AiLabBuilder;

/// <summary>Estimated USD cost of one AI Lab Builder generation call, shown to the human reviewer.</summary>
public sealed class LabBuilderCostEstimate
{
    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int CachedInputTokens { get; set; }

    public string? ModelName { get; set; }

    public long ElapsedMilliseconds { get; set; }

    public decimal EstimatedCostUsd { get; set; }

    /// <summary>True when rates came from built-in defaults rather than configured "AgentPricing" values.</summary>
    public bool IsEstimate { get; set; }
}

/// <summary>
/// Converts a <see cref="LabBuilderTokenUsage"/> into an estimated USD cost, same per-million-token
/// rate scheme as HumanOS/Services/TokenCostEstimator.cs (Capabilities/Structured Learning cost
/// dashboards) so Lab generation costs are comparable. Reads optional overrides from the
/// "AgentPricing" config section; falls back to built-in default rates otherwise.
/// </summary>
public static class LabBuilderCostEstimator
{
    private const decimal DefaultInputPerMillion = 1.25m;
    private const decimal DefaultCachedInputPerMillion = 0.625m;
    private const decimal DefaultOutputPerMillion = 10.00m;

    public static LabBuilderCostEstimate Estimate(LabBuilderTokenUsage usage, IConfiguration configuration)
    {
        var modelKey = string.IsNullOrWhiteSpace(usage.ModelName) ? null : $"AgentPricing:Models:{usage.ModelName}";
        var isEstimate = modelKey is null || configuration.GetSection(modelKey).Exists() == false;

        var inputRate = ReadDecimal(configuration, modelKey is null ? null : $"{modelKey}:InputPerMillionTokens", DefaultInputPerMillion);
        var cachedRate = ReadDecimal(configuration, modelKey is null ? null : $"{modelKey}:CachedInputPerMillionTokens", DefaultCachedInputPerMillion);
        var outputRate = ReadDecimal(configuration, modelKey is null ? null : $"{modelKey}:OutputPerMillionTokens", DefaultOutputPerMillion);

        var cachedInput = (long)usage.CachedInputTokens;
        var billableInput = Math.Max(0, (long)usage.InputTokens - cachedInput);
        var output = (long)usage.OutputTokens;

        var cost = (billableInput / 1_000_000m * inputRate)
            + (cachedInput / 1_000_000m * cachedRate)
            + (output / 1_000_000m * outputRate);

        return new LabBuilderCostEstimate
        {
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            CachedInputTokens = usage.CachedInputTokens,
            ModelName = usage.ModelName,
            ElapsedMilliseconds = usage.ElapsedMilliseconds,
            EstimatedCostUsd = Math.Round(cost, 4),
            IsEstimate = isEstimate,
        };
    }

    private static decimal ReadDecimal(IConfiguration configuration, string? key, decimal fallback)
    {
        if (key is null)
        {
            return fallback;
        }

        var raw = configuration[key];
        return !string.IsNullOrWhiteSpace(raw) && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }
}
