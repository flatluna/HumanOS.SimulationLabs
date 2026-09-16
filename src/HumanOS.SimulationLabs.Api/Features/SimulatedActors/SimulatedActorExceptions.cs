namespace HumanOS.SimulationLabs.Api.Features.SimulatedActors;

public sealed class SimulatedActorNotFoundException : Exception { }
public sealed class SimulatedActorCodeDuplicateException : Exception { }
public sealed class SimulatedActorOrderDuplicateException : Exception { }
public sealed class SimulatedActorScenarioNotFoundException : Exception { }
public sealed class SimulatedActorScenarioNotEditableException : Exception { }
public sealed class SimulatedActorVersionNotEditableException : Exception { }
public sealed class SimulatedActorPrincipalAlreadyExistsException : Exception { }
public sealed class SimulatedActorPreconditionException : Exception { public string Code { get; } public SimulatedActorPreconditionException(string code) : base(code) => Code = code; }
public sealed class SimulatedActorConcurrencyException : Exception { }
