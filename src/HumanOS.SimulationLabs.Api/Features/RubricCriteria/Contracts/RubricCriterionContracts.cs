using System.Text.Json;
using System.Text.Json.Serialization;

namespace HumanOS.SimulationLabs.Api.Features.RubricCriteria.Contracts;

[JsonConverter(typeof(OptionalFieldJsonConverterFactory))]
public readonly struct OptionalField<T> : IEquatable<OptionalField<T>>
{
    public bool IsSpecified { get; }
    public T? Value { get; }

    public OptionalField(T? value)
    {
        IsSpecified = true;
        Value = value;
    }

    public static OptionalField<T> Unspecified => default;

    public static implicit operator OptionalField<T>(T? value) => new(value);

    public bool Equals(OptionalField<T> other) =>
        IsSpecified == other.IsSpecified && EqualityComparer<T?>.Default.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is OptionalField<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(IsSpecified, Value);

    public static bool operator ==(OptionalField<T> left, OptionalField<T> right) => left.Equals(right);

    public static bool operator !=(OptionalField<T> left, OptionalField<T> right) => !left.Equals(right);
}

public sealed class OptionalFieldJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(OptionalField<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalFieldJsonConverter<>).MakeGenericType(itemType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}

public sealed class OptionalFieldJsonConverter<T> : JsonConverter<OptionalField<T>>
{
    public override bool HandleNull => true;

    public override OptionalField<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new OptionalField<T>(default);
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return new OptionalField<T>(value);
    }

    public override void Write(Utf8JsonWriter writer, OptionalField<T> value, JsonSerializerOptions options)
    {
        if (value.IsSpecified)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public sealed class CreateRubricCriterionRequest
{
    public Guid? ObjectiveId { get; set; }
    public Guid? ExpectedMomentId { get; set; }
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoEvidencia { get; set; }
    public decimal? Peso { get; set; }
    public decimal? ScoreMinimoEsperado { get; set; }
    public bool? EsCritico { get; set; }
    public string? IndicadoresPositivos { get; set; }
    public string? IndicadoresNegativos { get; set; }
    public string? ErrorCritico { get; set; }
    public string? RecomendacionBase { get; set; }
}

public sealed class UpdateRubricCriterionRequest
{
    public OptionalField<Guid?> ObjectiveId { get; set; }
    public OptionalField<Guid?> ExpectedMomentId { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? TipoEvidencia { get; set; }
    public decimal? Peso { get; set; }
    public decimal? ScoreMinimoEsperado { get; set; }
    public bool? EsCritico { get; set; }
    public string? IndicadoresPositivos { get; set; }
    public OptionalField<string?> IndicadoresNegativos { get; set; }
    public OptionalField<string?> ErrorCritico { get; set; }
    public OptionalField<string?> RecomendacionBase { get; set; }
}

public sealed class ReorderRubricCriteriaRequest
{
    public List<ReorderRubricCriterionItem>? Criteria { get; set; }
}

public sealed class ReorderRubricCriterionItem
{
    public Guid IdCriterion { get; set; }
    public int Orden { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class RubricCriterionActionRequest
{
    public string? Motivo { get; set; }
}

public sealed class RubricCriterionResponse
{
    public Guid IdCriterion { get; set; }
    public Guid IdRubric { get; set; }
    public Guid IdVersion { get; set; }
    public Guid? ObjectiveId { get; set; }
    public Guid? ExpectedMomentId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string TipoEvidencia { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public decimal ScoreMinimoEsperado { get; set; }
    public bool EsCritico { get; set; }
    public string IndicadoresPositivos { get; set; } = string.Empty;
    public string? IndicadoresNegativos { get; set; }
    public string? ErrorCritico { get; set; }
    public string? RecomendacionBase { get; set; }
    public int Orden { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public string CreadoPor { get; set; } = string.Empty;
    public DateTimeOffset? FechaActualizacion { get; set; }
    public string? ActualizadoPor { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class RubricCriterionListItemResponse
{
    public Guid IdCriterion { get; set; }
    public Guid? ObjectiveId { get; set; }
    public Guid? ExpectedMomentId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string TipoEvidencia { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public decimal ScoreMinimoEsperado { get; set; }
    public bool EsCritico { get; set; }
    public int Orden { get; set; }
    public string Estatus { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class RubricCriterionListResponse
{
    public IReadOnlyList<RubricCriterionListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public sealed class ReorderRubricCriteriaResponse
{
    public IReadOnlyList<ReorderRubricCriterionResponseItem> Items { get; set; } = [];
}

public sealed class ReorderRubricCriterionResponseItem
{
    public Guid IdCriterion { get; set; }
    public int Orden { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
