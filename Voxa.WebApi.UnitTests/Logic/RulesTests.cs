using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Models;
using Voxa.Logic;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.WebApi.UnitTests.Logic;

public sealed class RulesTests
{
    private static int s_ids;

    [Fact]
    public void CodesUseTheUnambiguousAlphabetAndNormalise()
    {
        var service = new CodeService(Options.Create(new CodeOptions { Pepper = "p" }));
        var code = service.Generate();

        Assert.Matches("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{4}$", code);
        Assert.Equal(code.Replace("-", string.Empty, StringComparison.Ordinal), service.Normalize(" " + code.ToLowerInvariant() + " "));
        Assert.Null(service.Normalize("K7Q2-M9X0"));
        Assert.Null(service.Normalize("K7Q2"));
    }

    [Fact]
    public void TheHashDependsOnThePepper()
    {
        var one = new CodeService(Options.Create(new CodeOptions { Pepper = "one" }));
        var two = new CodeService(Options.Create(new CodeOptions { Pepper = "two" }));

        Assert.Equal(one.Hash("K7Q2M9XD"), one.Hash("K7Q2M9XD"));
        Assert.NotEqual(one.Hash("K7Q2M9XD"), two.Hash("K7Q2M9XD"));
    }

    [Theory]
    [InlineData(10, 9, 5, MajorityRule.Absolute, ResultOutcome.Adopted)]
    [InlineData(9, 9, 5, MajorityRule.Absolute, ResultOutcome.Rejected)]
    [InlineData(20, 10, 0, MajorityRule.TwoThirds, ResultOutcome.Adopted)]
    [InlineData(19, 10, 0, MajorityRule.TwoThirds, ResultOutcome.Rejected)]
    [InlineData(0, 0, 4, MajorityRule.Absolute, ResultOutcome.NoVotes)]
    public void AProposalCountsForAndAgainstOnly(int forVotes, int against, int abstain, MajorityRule rule, ResultOutcome expected)
    {
        var item = Proposal(rule, forVotes, against, abstain);

        var result = ResultCalculator.Calculate(item);

        Assert.Equal(expected, result.Outcome);
        Assert.Equal(forVotes + against, result.ValidVotes);
    }

    [Fact]
    public void AnElectionNeedsAnAbsoluteMajorityOfTheBallotsThatAreNotBlank()
    {
        // 10 ballots, 2 blank: 8 valid, 5 needed. A has 6, B 5, C 3; two seats.
        var item = Election(seats: 2, ballots: 10, blank: 2, ("A", 6), ("B", 5), ("C", 3));

        var result = ResultCalculator.Calculate(item);

        Assert.Equal(8, result.ValidVotes);
        Assert.Equal(5, result.VotesNeeded);
        Assert.Equal(ResultOutcome.Elected, result.Outcome);
        Assert.Equal(["A", "B"], result.Options.Where(static o => o.Elected).Select(static o => o.Label));
    }

    [Fact]
    public void AnElectionWithoutEnoughMajoritiesNeedsAnotherRound()
    {
        var item = Election(seats: 2, ballots: 10, blank: 0, ("A", 7), ("B", 4), ("C", 4));

        var result = ResultCalculator.Calculate(item);

        Assert.Equal(ResultOutcome.NoMajority, result.Outcome);
        Assert.Equal(["A"], result.Options.Where(static o => o.Elected).Select(static o => o.Label));
    }

    [Fact]
    public void ATieOnTheLastSeatElectsNoneOfTheTied()
    {
        var item = Election(seats: 1, ballots: 10, blank: 0, ("A", 6), ("B", 6));

        var result = ResultCalculator.Calculate(item);

        Assert.Equal(ResultOutcome.Tie, result.Outcome);
        Assert.DoesNotContain(result.Options, static o => o.Elected);
    }

    [Fact]
    public void TheCsvReaderKnowsDutchColumnsSeparatorsAndQuotes()
    {
        const string csv = "Voornaam;Tussenvoegsel;Achternaam;E-mail;Geboortedatum;Ouder e-mail;Status\n"
                         + "Jan;van der;Berg;jan@example.org;01-02-1980;;\n"
                         + "\"Piet, jr\";;Smit;;2015-06-01;ouder@example.org;\n"
                         + "Kees;;Geschorst;kees@example.org;;;geschorst";

        var members = MemberCsv.Parse(csv);

        Assert.Equal(["Jan van der Berg", "Piet, jr Smit", "Kees Geschorst"], members.Select(static m => m.Name));
        Assert.Equal(new DateOnly(1980, 2, 1), members[0].BirthDate);
        Assert.Equal("ouder@example.org", members[1].GuardianEmail);
        Assert.True(members[2].Suspended);
    }

    [Fact]
    public void TheCsvReaderNeedsANameColumn() =>
        Assert.Throws<DomainValidationException>(() => MemberCsv.Parse("email\na@example.org"));

    [Fact]
    public async Task MinorsGoThroughTheirGuardianAndSuspendedMembersAreLeftOut()
    {
        using var services = new TestServices(new DateTimeOffset(2027, 1, 10, 9, 0, 0, TimeSpan.Zero));
        var meeting = await services.Meetings.CreateAsync(new MeetingRequest { Name = "ALV", StartsAt = new DateTime(2027, 2, 2, 17, 45, 0, DateTimeKind.Utc) }, CancellationToken.None);
        const string csv = "naam;email;geboortedatum;ouder naam;ouder email;status\n"
                         + "Volwassen;v@example.org;1980-01-01;;;\n"
                         + "Jeugd;j@example.org;2015-01-01;Ouder Jeugd;o@example.org;\n"
                         + "Bijna 18;b@example.org;2009-02-02;;ob@example.org;\n"
                         + "Geschorst;g@example.org;1980-01-01;;;geschorst";

        var result = await services.Voters.ImportAsync(meeting.Id, new VoterImportRequest { Csv = csv }, CancellationToken.None);
        var list = await services.Voters.ListAsync(meeting.Id, CancellationToken.None);

        Assert.Equal(3, result.Imported);
        Assert.Single(result.Skipped);
        Assert.Equal("o@example.org", list.Voters.Single(static v => v.Name == "Jeugd").Email);
        Assert.Equal("Ouder Jeugd", list.Voters.Single(static v => v.Name == "Jeugd").GuardianName);
        // 18 on the day of the meeting: votes for themselves.
        Assert.Equal("b@example.org", list.Voters.Single(static v => v.Name == "Bijna 18").Email);
    }

    private static AgendaItem Proposal(MajorityRule rule, int forVotes, int against, int abstain)
    {
        var item = new AgendaItem { Id = Guid.NewGuid(), Title = "Voorstel", Kind = AgendaItemKind.Proposal, Majority = rule };
        foreach (var (role, count) in new[] { (OptionRole.For, forVotes), (OptionRole.Against, against), (OptionRole.Abstain, abstain) })
        {
            item.Options.Add(Option(role, role.ToString(), count));
        }

        item.Ballots.Add(new BallotTally { Channel = VoteChannel.Hall, Count = forVotes + against + abstain });
        return item;
    }

    private static AgendaItem Election(int seats, int ballots, int blank, params (string Name, int Votes)[] candidates)
    {
        var item = new AgendaItem { Id = Guid.NewGuid(), Title = "Verkiezing", Kind = AgendaItemKind.Election, Seats = seats };
        foreach (var (name, votes) in candidates)
        {
            item.Options.Add(Option(OptionRole.Candidate, name, votes));
        }

        item.Options.Add(Option(OptionRole.Blank, "Blanco", blank));
        item.Ballots.Add(new BallotTally { Channel = VoteChannel.Hall, Count = ballots });
        return item;
    }

    private static ItemOption Option(OptionRole role, string label, int count)
    {
        var option = new ItemOption { Id = Guid.NewGuid(), Role = role, Label = label, Position = ++s_ids };
        option.Tallies.Add(new OptionTally { Channel = VoteChannel.Hall, Count = count });
        return option;
    }
}
