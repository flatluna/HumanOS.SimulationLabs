namespace HumanOS.SimulationLabs.Api.Features.ConversationTurns;

public sealed class ConversationTurnNotFoundException : Exception { }
public sealed class ConversationTurnAttemptNotFoundException : Exception { }
public sealed class ConversationTurnAttemptNotEditableException : Exception { }
public sealed class ActorScenarioMismatchException : Exception { }
public sealed class ExpectedMomentVersionMismatchException : Exception { }
public sealed class TranscriptNotEditableException : Exception { }
public sealed class ConversationTurnPreconditionException : Exception { public string Code { get; } public ConversationTurnPreconditionException(string code) : base(code) => Code = code; }
public sealed class ConversationTurnConcurrencyException : Exception { }
