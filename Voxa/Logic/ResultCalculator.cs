using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;

namespace Voxa.Logic;

/// <summary>
/// Turns the counters of an item into its result, following the usual statutes: blank votes and abstentions do not
/// count, a proposal needs more than half of the valid votes (or two thirds where the statutes ask for it), and a person
/// is elected with an absolute majority of the valid ballots; otherwise a new round is needed.
/// </summary>
public static class ResultCalculator
{
    public static ChannelCountModel Count(IEnumerable<(VoteChannel Channel, int Count)> tallies)
    {
        var list = tallies.ToList();
        int Sum(VoteChannel channel) => list.Where(t => t.Channel == channel).Sum(static t => t.Count);
        var advance = Sum(VoteChannel.Advance);
        var hall = Sum(VoteChannel.Hall);
        var paper = Sum(VoteChannel.Paper);
        return new ChannelCountModel { Advance = advance, Hall = hall, Paper = paper, Total = advance + hall + paper };
    }

    public static ResultModel Calculate(AgendaItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var ballots = Count(item.Ballots.Select(static b => (b.Channel, b.Count)));
        var options = item.Options.OrderBy(static o => o.Position)
                          .Select(static o => (Option: o, Votes: Count(o.Tallies.Select(static t => (t.Channel, t.Count)))))
                          .ToList();

        return item.Kind == AgendaItemKind.Proposal ? Proposal(item, ballots, options) : Election(item, ballots, options);
    }

    /// <summary>More than half: the smallest whole number above half. Two thirds: at least two thirds.</summary>
    public static int VotesNeeded(int valid, MajorityRule rule) =>
        rule == MajorityRule.TwoThirds ? (int)Math.Ceiling(valid * 2 / 3.0) : (valid / 2) + 1;

    private static ResultModel Proposal(AgendaItem item, ChannelCountModel ballots, List<(ItemOption Option, ChannelCountModel Votes)> options)
    {
        int Total(OptionRole role) => options.Where(o => o.Option.Role == role).Sum(static o => o.Votes.Total);
        var forVotes = Total(OptionRole.For);
        var valid = forVotes + Total(OptionRole.Against);
        var needed = VotesNeeded(valid, item.Majority);
        var outcome = valid == 0 ? ResultOutcome.NoVotes : forVotes >= needed ? ResultOutcome.Adopted : ResultOutcome.Rejected;
        return Build(item, ballots, options, valid, needed, outcome, static _ => false);
    }

    private static ResultModel Election(AgendaItem item, ChannelCountModel ballots, List<(ItemOption Option, ChannelCountModel Votes)> options)
    {
        var blank = options.Where(static o => o.Option.Role == OptionRole.Blank).Sum(static o => o.Votes.Total);
        var valid = Math.Max(ballots.Total - blank, 0);
        var needed = VotesNeeded(valid, MajorityRule.Absolute);
        var seats = Math.Max(1, item.Seats);
        var withMajority = options.Where(o => o.Option.Role == OptionRole.Candidate && o.Votes.Total >= needed)
                                  .OrderByDescending(static o => o.Votes.Total)
                                  .ToList();

        var elected = new HashSet<int>();
        ResultOutcome outcome;
        if (valid == 0)
        {
            outcome = ResultOutcome.NoVotes;
        }
        else
        {
            // When more candidates have a majority than there are seats, a tie on the last seat elects none of the tied.
            var tieAtCut = withMajority.Count > seats && withMajority[seats - 1].Votes.Total == withMajority[seats].Votes.Total;
            var cutVotes = tieAtCut ? withMajority[seats - 1].Votes.Total : int.MinValue;
            foreach (var (option, _) in withMajority.Take(seats).Where(c => c.Votes.Total > cutVotes))
            {
                _ = elected.Add(option.Id);
            }

            outcome = tieAtCut ? ResultOutcome.Tie : elected.Count < seats ? ResultOutcome.NoMajority : ResultOutcome.Elected;
        }

        return Build(item, ballots, options, valid, needed, outcome, elected.Contains);
    }

    private static ResultModel Build(AgendaItem item, ChannelCountModel ballots, List<(ItemOption Option, ChannelCountModel Votes)> options,
                                     int valid, int needed, ResultOutcome outcome, Func<int, bool> elected) => new()
                                     {
                                         ItemId = item.Id,
                                         MeetingId = item.MeetingId,
                                         Position = item.Position,
                                         Title = item.Title,
                                         Kind = item.Kind,
                                         Majority = item.Majority,
                                         Seats = item.Seats,
                                         Ballots = ballots,
                                         Options = [.. options.Select(o => new OptionResultModel
                                         {
                                             OptionId = o.Option.Id,
                                             Label = o.Option.Label,
                                             Role = o.Option.Role,
                                             Votes = o.Votes,
                                             Elected = elected(o.Option.Id),
                                         })],
                                         ValidVotes = valid,
                                         VotesNeeded = needed,
                                         Outcome = outcome,
                                     };
}
