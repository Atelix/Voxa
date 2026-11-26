namespace Voxa.Abstractions;

/// <summary>
/// The caller may not perform this action. Reported as HTTP 403 with the message as detail.
/// </summary>
public class DomainForbiddenException : Exception
{
    public DomainForbiddenException()
    {
    }

    public DomainForbiddenException(string message) : base(message)
    {
    }

    public DomainForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
