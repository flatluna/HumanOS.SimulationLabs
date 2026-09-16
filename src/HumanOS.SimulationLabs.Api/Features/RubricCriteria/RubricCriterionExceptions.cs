namespace HumanOS.SimulationLabs.Api.Features.RubricCriteria;

public sealed class RubricCriterionNotFoundException : Exception { }
public sealed class RubricCriterionCodeDuplicateException : Exception { }
public sealed class RubricCriterionOrderDuplicateException : Exception { }
public sealed class RubricCriterionRubricNotFoundException : Exception { }
public sealed class RubricCriterionRubricNotEditableException : Exception { }
public sealed class RubricCriterionVersionNotFoundException : Exception { }
public sealed class RubricCriterionVersionNotEditableException : Exception { }
public sealed class RubricCriterionNotEditableException : Exception { }
public sealed class RubricCriterionObjectiveNotFoundException : Exception { }
public sealed class RubricCriterionExpectedMomentNotFoundException : Exception { }
public sealed class RubricCriterionObjectiveMomentMismatchException : Exception
{
    public string Code { get; }
    public RubricCriterionObjectiveMomentMismatchException(string code = "CRITERION_OBJECTIVE_MISMATCH") : base(code) => Code = code;
}
public sealed class RubricCriterionPreconditionException : Exception
{
    public string Code { get; }
    public RubricCriterionPreconditionException(string code) : base(code) => Code = code;
}
public sealed class RubricCriterionConcurrencyException : Exception { }
