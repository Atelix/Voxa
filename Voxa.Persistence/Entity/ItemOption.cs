namespace Voxa.Persistence.Entity;

/// <summary>A choice on an agenda item: for, against or abstain, or a candidate or the blank vote.</summary>
public class ItemOption
{
    public int Id { get; set; }

    public int AgendaItemId { get; set; }

    public AgendaItem AgendaItem { get; set; } = null!;

    public int Position { get; set; }

    public required string Label { get; set; }

    public string? Note { get; set; }

    public OptionRole Role { get; set; }

    public ICollection<OptionTally> Tallies { get; set; } = [];
}
