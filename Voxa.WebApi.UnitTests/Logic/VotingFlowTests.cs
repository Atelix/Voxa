using Microsoft.EntityFrameworkCore;
using Voxa.Abstractions;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;

namespace Voxa.WebApi.UnitTests.Logic;

/// <summary>The whole route of a vote: meeting, voters, codes by mail, advance and hall votes, results.</summary>
public sealed class VotingFlowTests : IDisposable
{
    // 1 February 2027, 10:00 in Amsterdam; the meeting is the next evening.
    private static readonly DateTimeOffset s_now = new(2027, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTime s_meetingStart = new(2027, 2, 2, 17, 45, 0, DateTimeKind.Utc);
    private readonly TestServices _services = new(s_now);

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task AVoteIsStoredOnlyAsCountersAndTheCodeIsMarkedUsed()
    {
        var (meeting, proposal, _) = await ReadyMeetingAsync();
        var code = _services.MailedCodes()[0];
        var session = await _services.Ballot.SessionAsync(new BallotCodeRequest { Code = code }, CancellationToken.None);
        var forOption = session.Items.Single(i => i.Id == proposal.Id).Options.Single(o => o.Role == OptionRole.For);

        await _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = proposal.Id, OptionIds = [forOption.Id] }, CancellationToken.None);

        var ballots = await _services.Context.BallotTallies.SingleAsync(t => t.AgendaItemId == proposal.Id);
        Assert.Equal(VoteChannel.Advance, ballots.Channel);
        Assert.Equal(1, ballots.Count);
        Assert.Equal(1, (await _services.Context.OptionTallies.SingleAsync(t => t.ItemOptionId == forOption.Id)).Count);
        Assert.Single(await _services.Context.CodeUses.ToListAsync());
        var after = await _services.Ballot.SessionAsync(new BallotCodeRequest { Code = code }, CancellationToken.None);
        Assert.Equal(BallotItemStatus.Voted, after.Items.Single(i => i.Id == proposal.Id).Status);
        Assert.Equal(meeting.Name, after.MeetingName);
    }

    [Fact]
    public async Task TheDatabaseHoldsNoCodeAndNoLinkToAVoter()
    {
        await ReadyMeetingAsync();
        var mailed = _services.MailedCodes();

        var hashes = await _services.Context.VotingCodes.Select(static c => c.Hash).ToListAsync();
        Assert.Equal(mailed.Count, hashes.Count);
        Assert.All(mailed, code => Assert.DoesNotContain(code.Replace("-", string.Empty, StringComparison.Ordinal), hashes));
        Assert.All(mailed, code => Assert.Contains(_services.Codes.Hash(_services.Codes.Normalize(code)!), hashes));
        // VotingCode has no voter column, and Voter has no code column: the entities themselves cannot link them.
        Assert.DoesNotContain(typeof(VotingCode).GetProperties(), static p => p.Name.Contains("Voter", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(Voter).GetProperties(), static p => p.Name.Contains("Hash", StringComparison.Ordinal));
        Assert.All(await _services.Context.Voters.ToListAsync(), static v => Assert.True(v.CodeSent || v.Email is null));
    }

    [Fact]
    public async Task ACodeVotesOncePerItem()
    {
        var (_, proposal, _) = await ReadyMeetingAsync();
        var code = _services.MailedCodes()[0];
        var option = proposal.Options[0].Id;
        await _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = proposal.Id, OptionIds = [option] }, CancellationToken.None);

        await Assert.ThrowsAsync<DomainConflictException>(() =>
            _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = proposal.Id, OptionIds = [option] }, CancellationToken.None));
        Assert.Equal(1, await _services.Context.BallotTallies.SumAsync(static t => t.Count));
    }

    [Fact]
    public async Task AnItemWithoutAdvanceVotingWaitsUntilTheBoardOpensIt()
    {
        var (_, _, hallOnly) = await ReadyMeetingAsync();
        var code = _services.MailedCodes()[0];
        var request = new VoteRequest { Code = code, ItemId = hallOnly.Id, OptionIds = [hallOnly.Options[0].Id] };

        await Assert.ThrowsAsync<DomainForbiddenException>(() => _services.Ballot.VoteAsync(request, CancellationToken.None));

        _services.Time.SetUtcNow(new DateTimeOffset(s_meetingStart.AddMinutes(30)));
        _ = await _services.Agenda.OpenAsync(hallOnly.Id, CancellationToken.None);
        await _services.Ballot.VoteAsync(request, CancellationToken.None);

        var tally = await _services.Context.BallotTallies.SingleAsync(t => t.AgendaItemId == hallOnly.Id);
        Assert.Equal(VoteChannel.Hall, tally.Channel);
    }

    [Fact]
    public async Task TheResultStaysHiddenUntilTheItemCloses()
    {
        var (meeting, proposal, _) = await ReadyMeetingAsync();
        foreach (var code in _services.MailedCodes().Take(3))
        {
            await _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = proposal.Id, OptionIds = [proposal.Options[0].Id] }, CancellationToken.None);
        }

        await Assert.ThrowsAsync<DomainForbiddenException>(() => _services.Results.ResultAsync(proposal.Id, CancellationToken.None));
        var live = await _services.Results.LiveAsync(meeting.Id, CancellationToken.None);
        Assert.Equal(3, live.Items.Single(i => i.Id == proposal.Id).Ballots.Advance);

        _ = await _services.Agenda.CloseAsync(proposal.Id, CancellationToken.None);
        var result = await _services.Results.ResultAsync(proposal.Id, CancellationToken.None);
        Assert.Equal(ResultOutcome.Adopted, result.Outcome);
        Assert.Equal(3, result.ValidVotes);
    }

    [Fact]
    public async Task PaperVotesAddToTheResultAndReplaceAnEarlierCount()
    {
        var (_, proposal, _) = await ReadyMeetingAsync();
        await _services.Ballot.VoteAsync(new VoteRequest { Code = _services.MailedCodes()[0], ItemId = proposal.Id, OptionIds = [proposal.Options[0].Id] }, CancellationToken.None);
        _ = await _services.Agenda.CloseAsync(proposal.Id, CancellationToken.None);
        var against = proposal.Options[1].Id;

        _ = await _services.Results.SetPaperVotesAsync(proposal.Id, new PaperVotesRequest { Options = [new() { OptionId = against, Count = 5 }] }, CancellationToken.None);
        var result = await _services.Results.SetPaperVotesAsync(proposal.Id, new PaperVotesRequest { Options = [new() { OptionId = against, Count = 2 }] }, CancellationToken.None);

        Assert.Equal(2, result.Ballots.Paper);
        Assert.Equal(3, result.Ballots.Total);
        Assert.Equal(ResultOutcome.Rejected, result.Outcome);
    }

    [Fact]
    public async Task AnElectionTakesUpToTheSeatsOrOnlyBlank()
    {
        var meeting = await _services.Meetings.CreateAsync(Request(), CancellationToken.None);
        var election = await _services.Agenda.AddAsync(meeting.Id, new AgendaItemRequest
        {
            Title = "Bestuur",
            Kind = AgendaItemKind.Election,
            Seats = 1,
            Candidates = [new() { Label = "A" }, new() { Label = "B" }],
        }, CancellationToken.None);
        await ImportAndSendAsync(meeting.Id);
        var code = _services.MailedCodes()[0];
        var a = election.Options.Single(o => o.Label == "A").Id;
        var b = election.Options.Single(o => o.Label == "B").Id;
        var blank = election.Options.Single(o => o.Role == OptionRole.Blank).Id;

        await Assert.ThrowsAsync<DomainValidationException>(() => _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = election.Id, OptionIds = [a, b] }, CancellationToken.None));
        await Assert.ThrowsAsync<DomainValidationException>(() => _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = election.Id, OptionIds = [a, blank] }, CancellationToken.None));
        await _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = election.Id, OptionIds = [blank] }, CancellationToken.None);
    }

    [Fact]
    public async Task FiveWrongCodesBlockTheDeviceForTenMinutes()
    {
        await ReadyMeetingAsync();
        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<ObjectNotFoundException>(() => _services.Ballot.SessionAsync(new BallotCodeRequest { Code = "AAAA-AAAA" }, CancellationToken.None));
        }

        var good = new BallotCodeRequest { Code = _services.MailedCodes()[0] };
        await Assert.ThrowsAsync<DomainRateLimitedException>(() => _services.Ballot.SessionAsync(good, CancellationToken.None));

        // Another phone in the same hall is not blocked by this one.
        _services.Client.Device = "device:other";
        _ = await _services.Ballot.SessionAsync(good, CancellationToken.None);

        _services.Client.Device = "device:test";
        _services.Time.Advance(TimeSpan.FromMinutes(10));
        _ = await _services.Ballot.SessionAsync(good, CancellationToken.None);
    }

    [Fact]
    public async Task ArchivingDeletesCodesAndVotersButKeepsTotals()
    {
        var (meeting, proposal, _) = await ReadyMeetingAsync();
        var code = _services.MailedCodes()[0];
        await _services.Ballot.VoteAsync(new VoteRequest { Code = code, ItemId = proposal.Id, OptionIds = [proposal.Options[0].Id] }, CancellationToken.None);

        var archived = await _services.Meetings.ArchiveAsync(meeting.Id, CancellationToken.None);

        Assert.Equal(MeetingStatus.Archived, archived.Status);
        Assert.Equal(1, archived.CodesUsed);
        Assert.Empty(await _services.Context.VotingCodes.ToListAsync());
        Assert.Empty(await _services.Context.Voters.ToListAsync());
        Assert.Equal(1, (await _services.Results.ResultAsync(proposal.Id, CancellationToken.None)).Ballots.Total);
        await Assert.ThrowsAsync<ObjectNotFoundException>(() => _services.Ballot.SessionAsync(new BallotCodeRequest { Code = code }, CancellationToken.None));
    }

    [Fact]
    public async Task AdvanceVotingCannotOpenMoreThanTheAllowedDaysBefore()
    {
        var tooEarly = new MeetingRequest { Name = "ALV", StartsAt = s_meetingStart, AdvanceVoting = true, AdvanceOpensAt = s_meetingStart.AddDays(-31) };

        await Assert.ThrowsAsync<DomainValidationException>(() => _services.Meetings.CreateAsync(tooEarly, CancellationToken.None));
        var defaulted = await _services.Meetings.CreateAsync(new MeetingRequest { Name = "ALV", StartsAt = s_meetingStart, AdvanceVoting = true }, CancellationToken.None);
        // 30 days before 2 February is 3 January, from midnight in Amsterdam (23:00 UTC the day before).
        Assert.Equal(new DateTime(2027, 1, 2, 23, 0, 0, DateTimeKind.Utc), defaulted.AdvanceOpensAt);
    }

    [Fact]
    public async Task CopyStartsNextYearsMeetingWithTheSameAgenda()
    {
        var (meeting, _, _) = await ReadyMeetingAsync();

        var copy = await _services.Meetings.CopyAsync(meeting.Id, CancellationToken.None);

        Assert.Equal("ALV 2028", copy.Name);
        Assert.Equal(MeetingStatus.Draft, copy.Status);
        Assert.Equal(meeting.Items.Select(static i => i.Title), copy.Items.Select(static i => i.Title));
        Assert.All(copy.Items, static i => Assert.Equal(0, i.Ballots));
    }

    private static MeetingRequest Request() => new()
    {
        Name = "ALV 2027",
        StartsAt = s_meetingStart,
        Location = "Kantine",
        AdvanceVoting = true,
        AdvanceOpensAt = s_meetingStart.AddDays(-10),
    };

    private async Task<(MeetingModel Meeting, AgendaItemModel Proposal, AgendaItemModel HallOnly)> ReadyMeetingAsync()
    {
        var meeting = await _services.Meetings.CreateAsync(Request(), CancellationToken.None);
        var proposal = await _services.Agenda.AddAsync(meeting.Id, new AgendaItemRequest { Title = "Begroting 2027" }, CancellationToken.None);
        var hallOnly = await _services.Agenda.AddAsync(meeting.Id, new AgendaItemRequest { Title = "Statutenwijziging", Majority = MajorityRule.TwoThirds, AdvanceVoting = false }, CancellationToken.None);
        await ImportAndSendAsync(meeting.Id);
        return (await _services.Meetings.GetAsync(meeting.Id, CancellationToken.None), proposal, hallOnly);
    }

    private async Task ImportAndSendAsync(Guid meetingId)
    {
        const string csv = "naam;email\nLid Een;een@example.org\nLid Twee;twee@example.org\nLid Drie;drie@example.org\nLid Vier;vier@example.org";
        _ = await _services.Voters.ImportAsync(meetingId, new VoterImportRequest { Csv = csv }, CancellationToken.None);
        var sent = await _services.Voters.SendCodesAsync(meetingId, CancellationToken.None);
        Assert.Equal(4, sent.Sent);
    }
}
