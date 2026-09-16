namespace HumanOS.SimulationLabs.Api.Features.Stages;

public sealed class StageNotFoundException : Exception { }
public sealed class StageCodeDuplicateException : Exception { }
public sealed class StageOrderDuplicateException : Exception { }
public sealed class StageNotEditableException : Exception { public StageNotEditableException(string status) : base(status) { } }
public sealed class StageVersionNotFoundException : Exception { }
public sealed class StageVersionNotEditableException : Exception { }
public sealed class StagePreconditionException : Exception { public string Code { get; } public StagePreconditionException(string code) : base(code) => Code = code; }
public sealed class StageConcurrencyException : Exception { }
