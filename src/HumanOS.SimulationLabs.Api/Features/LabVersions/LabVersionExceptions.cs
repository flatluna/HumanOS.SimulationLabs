namespace HumanOS.SimulationLabs.Api.Features.LabVersions;

public sealed class LabNotFoundException : Exception
{
    public LabNotFoundException(string message = "Lab no encontrado.") : base(message)
    {
    }
}

public sealed class LabRetiredException : Exception
{
    public LabRetiredException(string message = "El Lab está retirado y no admite nuevas versiones.") : base(message)
    {
    }
}

public sealed class LabVersionNotFoundException : Exception
{
    public LabVersionNotFoundException(string message = "Versión de Lab no encontrada.") : base(message)
    {
    }
}

public sealed class LabVersionDuplicateException : Exception
{
    public LabVersionDuplicateException(string message = "Conflicto al generar el número de versión.") : base(message)
    {
    }
}

public sealed class LabVersionNotEditableException : Exception
{
    public LabVersionNotEditableException(string estatus)
        : base($"La versión no es editable porque su estatus actual es {estatus}.")
    {
    }
}

public sealed class InvalidStatusTransitionException : Exception
{
    public InvalidStatusTransitionException(string currentStatus, string targetStatus)
        : base($"No se permite la transición de estado de {currentStatus} a {targetStatus}.")
    {
    }

    public InvalidStatusTransitionException(string message) : base(message)
    {
    }
}

public sealed class LabVersionPreconditionFailedException : Exception
{
    public string ReasonCode { get; }

    public LabVersionPreconditionFailedException(string reasonCode, string message) : base(message)
    {
        ReasonCode = reasonCode;
    }
}

public sealed class LabVersionConcurrencyException : Exception
{
    public LabVersionConcurrencyException(string message = "Conflicto de concurrencia al actualizar la versión de Lab.") : base(message)
    {
    }
}

public sealed class ConfigurationHashException : Exception
{
    public ConfigurationHashException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}
