namespace HumanOS.SimulationLabs.Api.Features.Labs;

public sealed class LabNotFoundException : Exception
{
}

public sealed class LabDuplicateCodeException : Exception
{
}

public sealed class LabConcurrencyException : Exception
{
}

public sealed class LabPreconditionFailedException : Exception
{
}

public sealed class LabNotEditableException : Exception
{
    public LabNotEditableException(string estatus) : base($"El Lab no es editable porque su estatus actual es {estatus}.")
    {
    }
}
