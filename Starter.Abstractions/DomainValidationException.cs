namespace Starter.Abstractions;

/// <summary>
/// The request breaks a business rule or carries invalid input. Reported as HTTP 400 with the message as detail.
/// </summary>
public class DomainValidationException : Exception
{
    public DomainValidationException()
    {
    }

    public DomainValidationException(string message) : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
