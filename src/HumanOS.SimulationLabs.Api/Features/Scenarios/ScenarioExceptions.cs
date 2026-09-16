namespace HumanOS.SimulationLabs.Api.Features.Scenarios;

public sealed class ScenarioNotFoundException : Exception { }
public sealed class ScenarioCodeDuplicateException : Exception { }
public sealed class ScenarioNotEditableException : Exception { }
public sealed class ScenarioVersionNotFoundException : Exception { }
public sealed class ScenarioVersionNotEditableException : Exception { }
public sealed class ScenarioPreconditionException : Exception { public string Code { get; } public ScenarioPreconditionException(string code) : base(code) => Code = code; }
public sealed class ScenarioConcurrencyException : Exception { }
