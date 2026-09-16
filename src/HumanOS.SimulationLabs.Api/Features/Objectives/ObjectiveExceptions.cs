namespace HumanOS.SimulationLabs.Api.Features.Objectives;

public sealed class ObjectiveNotFoundException : Exception { }
public sealed class ObjectiveCodeDuplicateException : Exception { }
public sealed class ObjectiveOrderDuplicateException : Exception { }
public sealed class ObjectiveNotEditableException : Exception { public ObjectiveNotEditableException(string status) : base(status) { } }
public sealed class ObjectiveVersionNotFoundException : Exception { }
public sealed class ObjectiveStageNotFoundException : Exception { }
public sealed class ObjectiveVersionNotEditableException : Exception { }
public sealed class ObjectivePreconditionException : Exception
{
    public string Code { get; }
    public ObjectivePreconditionException(string code) : base(code) => Code = code;
}
public sealed class ObjectiveConcurrencyException : Exception { }
