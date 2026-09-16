namespace HumanOS.SimulationLabs.Api.Features.ExpectedMoments;

public sealed class ExpectedMomentNotFoundException : Exception { }
public sealed class ExpectedMomentCodeDuplicateException : Exception { }
public sealed class ExpectedMomentOrderDuplicateException : Exception { }
public sealed class ExpectedMomentVersionNotFoundException : Exception { }
public sealed class ExpectedMomentVersionNotEditableException : Exception { }
public sealed class ExpectedMomentStageNotFoundException : Exception { }
public sealed class ExpectedMomentObjectiveNotFoundException : Exception { }
public sealed class ExpectedMomentObjectiveStageMismatchException : Exception { }
public sealed class ExpectedMomentPreconditionException : Exception
{
    public string Code { get; }
    public ExpectedMomentPreconditionException(string code) : base(code) => Code = code;
}
public sealed class ExpectedMomentConcurrencyException : Exception { }
