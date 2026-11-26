namespace Starter.Abstractions;

/// <summary>
/// The change conflicts with the current state, for example a duplicate. Reported as HTTP 409 with the message as detail.
/// </summary>
public class DomainConflictException : Exception
{
    public DomainConflictException()
    {
    }

    public DomainConflictException(string message) : base(message)
    {
    }

    public DomainConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
