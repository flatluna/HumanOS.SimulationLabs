namespace HumanOS.SimulationLabs.Api.Features.ArtifactSubmissions;

public sealed class ArtifactSubmissionNotFoundException : Exception { }
public sealed class ArtifactSubmissionAttemptNotFoundException : Exception { }
public sealed class ArtifactSubmissionAttemptNotEditableException : Exception { }
public sealed class ArtifactSubmissionNotEditableException : Exception { }
public sealed class ArtifactSubmissionVersionDuplicateException : Exception { }
public sealed class ArtifactSubmissionStageMismatchException : Exception { }
public sealed class ArtifactSubmissionObjectiveMismatchException : Exception { }
public sealed class InvalidArtifactJsonException : Exception { }
public sealed class ArtifactSubmissionPreconditionException : Exception { public string Code { get; } public ArtifactSubmissionPreconditionException(string code) : base(code) => Code = code; }
public sealed class ArtifactSubmissionConcurrencyException : Exception { }
