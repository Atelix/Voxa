namespace Voxa.Abstractions;

/// <summary>The requested record does not exist (or the caller may not know it exists). Reported as HTTP 404.</summary>
public class ObjectNotFoundException : Exception
{
    public ObjectNotFoundException(string entityName, object identifier)
        : base($"{entityName} with identifier '{identifier}' was not found.")
    {
        EntityName = entityName;
        Identifier = identifier;
    }

    public ObjectNotFoundException(string message) : base(message)
    {
    }

    public ObjectNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ObjectNotFoundException()
    {
    }

    public string? EntityName { get; }

    public object? Identifier { get; }
}
