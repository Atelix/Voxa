using System.Diagnostics.CodeAnalysis;

namespace Starter.WebApi;

/// <summary>
/// The envelope for successful responses that carry a message next to the data, such as "Item saved".
/// Failures never use it: they are problem details written by <see cref="DomainExceptionHandler"/>.
/// </summary>
[method: SetsRequiredMembers]
public class ApiResponse<TModel>(TModel data, string? message = null)
{
    public required TModel Data { get; set; } = data;

    public required string? Message { get; set; } = message;
}
