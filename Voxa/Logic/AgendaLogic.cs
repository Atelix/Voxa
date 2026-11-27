using Microsoft.EntityFrameworkCore;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

public sealed class AgendaLogic(VoxaContext context) : IAgendaLogic
{
    /// <summary>
    /// The fixed options of a proposal, in ballot order. The stored labels are Dutch; the app shows these options by
    /// their role, in the reader's language.
    /// </summary>
    public static readonly IReadOnlyList<(OptionRole Role, string Label)> ProposalOptions =
        [(OptionRole.For, "Voor"), (OptionRole.Against, "Tegen"), (OptionRole.Abstain, "Onthouding")];

    public const string BlankLabel = "Blanco";

    public async Task<AgendaItemModel> AddAsync(Guid meetingId, AgendaItemRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meeting = await context.Meetings.Include(static m => m.Items).FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken)
                      ?? throw new ObjectNotFoundException(nameof(Meeting), meetingId);
        MeetingLogic.EnsureNotArchived(meeting);
        var item = new AgendaItem { Title = string.Empty, MeetingId = meetingId, Position = meeting.Items.Count == 0 ? 1 : meeting.Items.Max(static i => i.Position) + 1 };
        Apply(item, request);
        _ = context.AgendaItems.Add(item);
        _ = await context.SaveChangesAsync(cancellationToken);
        return Mapping.Item(item);
    }

    public async Task<AgendaItemModel> UpdateAsync(Guid itemId, AgendaItemRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await LoadAsync(itemId, cancellationToken);
        EnsureEditable(item);
        context.ItemOptions.RemoveRange(item.Options);
        item.Options.Clear();
        Apply(item, request);
        _ = await context.SaveChangesAsync(cancellationToken);
        return Mapping.Item(item);
    }

    public async Task DeleteAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await LoadAsync(itemId, cancellationToken);
        EnsureEditable(item);
        _ = context.AgendaItems.Remove(item);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgendaItemModel>> ReorderAsync(Guid meetingId, ItemOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var items = await context.AgendaItems.Include(static i => i.Options).Include(static i => i.Ballots)
                                 .Where(i => i.MeetingId == meetingId).ToListAsync(cancellationToken);
        if (items.Count != request.ItemIds.Count || items.Any(i => !request.ItemIds.Contains(i.Id)) || request.ItemIds.Distinct().Count() != items.Count)
        {
            throw new DomainValidationException(Texts.Get("Error.ItemOrder"));
        }

        for (var index = 0; index < request.ItemIds.Count; index++)
        {
            items.Single(i => i.Id == request.ItemIds[index]).Position = index + 1;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return [.. items.OrderBy(static i => i.Position).Select(Mapping.Item)];
    }

    public async Task<AgendaItemModel> OpenAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await LoadAsync(itemId, cancellationToken);
        if (item.Meeting.Status != MeetingStatus.Ready)
        {
            throw new DomainConflictException(Texts.Get("Error.SendCodesFirst"));
        }

        if (item.State == AgendaItemState.Closed)
        {
            throw new DomainConflictException(Texts.Get("Error.ItemClosed"));
        }

        item.State = AgendaItemState.Open;
        _ = await context.SaveChangesAsync(cancellationToken);
        return Mapping.Item(item);
    }

    public async Task<AgendaItemModel> CloseAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await LoadAsync(itemId, cancellationToken);
        if (item.Meeting.Status == MeetingStatus.Draft)
        {
            throw new DomainConflictException(Texts.Get("Error.VotingNotStarted"));
        }

        item.State = AgendaItemState.Closed;
        _ = await context.SaveChangesAsync(cancellationToken);
        return Mapping.Item(item);
    }

    private async Task<AgendaItem> LoadAsync(Guid itemId, CancellationToken cancellationToken) =>
        await context.AgendaItems.Include(static i => i.Meeting).Include(static i => i.Options).Include(static i => i.Ballots)
                     .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
        ?? throw new ObjectNotFoundException(nameof(AgendaItem), itemId);

    private static void EnsureEditable(AgendaItem item)
    {
        MeetingLogic.EnsureNotArchived(item.Meeting);
        if (Mapping.IsLocked(item))
        {
            throw new DomainConflictException(Texts.Get("Error.ItemLocked"));
        }
    }

    private static void Apply(AgendaItem item, AgendaItemRequest request)
    {
        item.Title = Mapping.Clean(request.Title) ?? throw new DomainValidationException(Texts.Get("Error.ItemTitleRequired"));
        item.Description = Mapping.Clean(request.Description);
        item.Kind = request.Kind;
        item.AdvanceVoting = request.AdvanceVoting;
        if (request.Kind == AgendaItemKind.Proposal)
        {
            item.Majority = request.Majority;
            item.Seats = 0;
            item.Options = [.. ProposalOptions.Select(static (o, i) => new ItemOption { Position = i + 1, Role = o.Role, Label = o.Label })];
            return;
        }

        var candidates = request.Candidates.Select(static c => (Label: Mapping.Clean(c.Label), Note: Mapping.Clean(c.Note)))
                                .Where(static c => c.Label is not null).ToList();
        if (candidates.Count == 0)
        {
            throw new DomainValidationException(Texts.Get("Error.ElectionNoCandidates"));
        }

        if (request.Seats < 1)
        {
            throw new DomainValidationException(Texts.Get("Error.ElectionNoSeats"));
        }

        if (candidates.Select(static c => c.Label!.ToUpperInvariant()).Distinct().Count() != candidates.Count)
        {
            throw new DomainValidationException(Texts.Get("Error.CandidatesUnique"));
        }

        item.Majority = MajorityRule.Absolute;
        item.Seats = request.Seats;
        item.Options = [.. candidates.Select(static (c, i) => new ItemOption { Position = i + 1, Role = OptionRole.Candidate, Label = c.Label!, Note = c.Note })];
        item.Options.Add(new ItemOption { Position = candidates.Count + 1, Role = OptionRole.Blank, Label = BlankLabel });
    }
}
