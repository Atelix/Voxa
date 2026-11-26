namespace Voxa.Abstractions.Models;

/// <summary>
/// An item as the API returns it. <c>required</c> members are required in the Swagger document, so the generated
/// client types them as always present.
/// </summary>
public sealed record ItemModel
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string? Description { get; init; }

    public required bool Done { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }
}
