namespace HumanOS.SimulationLabs.Api.Features.UserActions;

public sealed class UserActionNotFoundException : Exception { }
public sealed class UserActionAttemptNotFoundException : Exception { }
public sealed class UserActionAttemptNotEditableException : Exception { }
public sealed class UserActionNotEditableException : Exception { }
public sealed class InvalidActionJsonException : Exception { }
public sealed class ActionExpectedMomentMismatchException : Exception { }
public sealed class UserActionPreconditionException : Exception { public string Code { get; } public UserActionPreconditionException(string code) : base(code) => Code = code; }
public sealed class UserActionConcurrencyException : Exception { }
