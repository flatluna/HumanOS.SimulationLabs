namespace HumanOS.SimulationLabs.Api.Features.Rubrics;

public sealed class RubricNotFoundException : Exception { }
public sealed class RubricAlreadyExistsException : Exception { }
public sealed class RubricCodeDuplicateException : Exception { }
public sealed class RubricVersionNotFoundException : Exception { }
public sealed class RubricVersionNotEditableException : Exception { }
public sealed class RubricNotEditableException : Exception { }
public sealed class RubricPreconditionException : Exception
{
    public string Code { get; }
    public RubricPreconditionException(string code) : base(code) => Code = code;
}
public sealed class RubricConcurrencyException : Exception { }
