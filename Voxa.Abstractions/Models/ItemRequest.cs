using System.ComponentModel.DataAnnotations;

namespace Voxa.Abstractions.Models;

/// <summary>The fields a client sends to create or update an item.</summary>
public sealed class ItemRequest
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    [Required]
    [StringLength(NameMaxLength)]
    public required string Name { get; init; }

    [StringLength(DescriptionMaxLength)]
    public string? Description { get; init; }

    public bool Done { get; init; }
}
