namespace Voxa.Persistence.Entity;

/// <summary>
/// An example entity. Replace it with your own domain; keep entities in this project and out of the API contract.
/// </summary>
public class Item
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool Done { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
