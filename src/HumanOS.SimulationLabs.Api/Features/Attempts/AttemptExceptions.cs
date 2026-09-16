namespace HumanOS.SimulationLabs.Api.Features.Attempts;

public sealed class AttemptNotFoundException : Exception { }
public sealed class AttemptScenarioNotFoundException : Exception { }
public sealed class ScenarioNotPublishedException : Exception { }
public sealed class AttemptLimitReachedException : Exception { }
public sealed class AttemptNotEditableException : Exception { }
public sealed class AttemptForbiddenException : Exception { }
public sealed class InvalidAttemptTransitionException : Exception { }
public sealed class AttemptPreconditionException : Exception { public string Code { get; } public AttemptPreconditionException(string code) : base(code) => Code = code; }
public sealed class AttemptConcurrencyException : Exception { }
