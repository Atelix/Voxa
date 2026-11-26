using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.WebApi.UnitTests.Logic;

/// <summary>A parent with several youth members gets one mail with a code per child, and every code is its own vote.</summary>
public sealed class SharedAddressTests : IDisposable
{
    private static readonly DateTime s_meetingStart = new(2027, 2, 2, 17, 45, 0, DateTimeKind.Utc);
    private readonly TestServices _services = new(new DateTimeOffset(2027, 2, 1, 9, 0, 0, TimeSpan.Zero));

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task OneAddressGetsOneMailWithACodePerVoter()
    {
        var meetingId = await ReadyMeetingAsync();

        var mails = _services.Outbox.Messages();
        Assert.Equal(2, mails.Count);
        var family = Assert.Single(mails, static m => m.To == "ouder@example.org");
        Assert.Contains("stemcodes", family.Subject, StringComparison.Ordinal);
        var codes = TestServices.CodesIn(family.Text);
        Assert.Equal(3, codes.Count);
        Assert.Equal(3, codes.Distinct().Count());
        Assert.Contains("Stemcode voor Jeugd Een: ", family.Text, StringComparison.Ordinal);
        Assert.Contains("Stemcode voor Jeugd Twee: ", family.Text, StringComparison.Ordinal);
        Assert.Contains("Stemcode voor Ouder Lid: ", family.Text, StringComparison.Ordinal);
        Assert.Contains("#code=" + string.Join(',', codes.Select(static c => c.Replace("-", string.Empty, StringComparison.Ordinal))), family.Text, StringComparison.Ordinal);
        var single = Assert.Single(mails, static m => m.To == "los@example.org");
        Assert.Single(TestServices.CodesIn(single.Text));
        var list = await _services.Voters.ListAsync(meetingId, CancellationToken.None);
        Assert.All(list.Voters, static v => Assert.True(v.CodeSent));
    }

    [Fact]
    public async Task EveryCodeInTheMailIsASeparateVote()
    {
        var meetingId = await ReadyMeetingAsync();
        var item = (await _services.Meetings.GetAsync(meetingId, CancellationToken.None)).Items.Single();
        var forId = item.Options.Single(static o => o.Role == OptionRole.For).Id;
        var againstId = item.Options.Single(static o => o.Role == OptionRole.Against).Id;
        var codes = TestServices.CodesIn(_services.Outbox.Messages().Single(static m => m.To == "ouder@example.org").Text);

        await _services.Ballot.VoteAsync(new VoteRequest { Code = codes[0], ItemId = item.Id, OptionIds = [forId] }, CancellationToken.None);
        await _services.Ballot.VoteAsync(new VoteRequest { Code = codes[1], ItemId = item.Id, OptionIds = [forId] }, CancellationToken.None);
        await _services.Ballot.VoteAsync(new VoteRequest { Code = codes[2], ItemId = item.Id, OptionIds = [againstId] }, CancellationToken.None);

        var counted = (await _services.Meetings.GetAsync(meetingId, CancellationToken.None)).Items.Single();
        Assert.Equal(3, counted.Ballots);
        var session = await _services.Ballot.SessionAsync(new BallotCodeRequest { Code = codes[0] }, CancellationToken.None);
        Assert.Equal(BallotItemStatus.Voted, session.Items.Single().Status);
    }

    [Fact]
    public void TheHtmlMailShowsEveryCodeWithItsMember()
    {
        var meeting = new Meeting { Name = "ALV 2027", StartsAt = s_meetingStart };
        var club = new ClubSettings { ClubName = "Club", PrimaryColor = "#231E5B", SecondaryColor = "#62CDF6", HeadingFont = "Oswald", BodyFont = "Hind", MailFromName = "Club" };
        var codes = new[] { new MailedCode(new Voter { Name = "Jeugd <Een>", GuardianName = "Ouder" }, "AAAA-BBBB"), new MailedCode(new Voter { Name = "Jeugd Twee", GuardianName = "Ouder" }, "CCCC-DDDD") };

        var mail = CodeMail.Build(meeting, club, codes, "ouder@example.org", new AppOptions { PublicUrl = "https://stem.example.org/" });

        Assert.Contains("Hoi Ouder,", mail.Text, StringComparison.Ordinal);
        Assert.Contains("https://stem.example.org/#code=AAAABBBB,CCCCDDDD", mail.Html, StringComparison.Ordinal);
        Assert.Contains("Stemcode voor Jeugd &lt;Een&gt;</p>", mail.Html, StringComparison.Ordinal);
        Assert.Contains(">AAAA-BBBB</p>", mail.Html, StringComparison.Ordinal);
        Assert.Contains(">CCCC-DDDD</p>", mail.Html, StringComparison.Ordinal);
    }

    private async Task<Guid> ReadyMeetingAsync()
    {
        var meeting = await _services.Meetings.CreateAsync(new MeetingRequest
        {
            Name = "ALV 2027",
            StartsAt = s_meetingStart,
            AdvanceVoting = true,
            AdvanceOpensAt = s_meetingStart.AddDays(-10),
        }, CancellationToken.None);
        _ = await _services.Agenda.AddAsync(meeting.Id, new AgendaItemRequest { Title = "Begroting 2027" }, CancellationToken.None);
        // Two children and the parent, who is a member too, share an address; the address is written differently once.
        const string csv = "naam;email;geboortedatum;ouder naam;ouder email\n"
                         + "Ouder Lid;ouder@example.org;1980-01-01;;\n"
                         + "Jeugd Een;;2014-03-03;Ouder Lid;ouder@example.org\n"
                         + "Jeugd Twee;;2016-04-04;Ouder Lid; Ouder@Example.org \n"
                         + "Los Lid;los@example.org;1990-01-01;;";
        _ = await _services.Voters.ImportAsync(meeting.Id, new VoterImportRequest { Csv = csv }, CancellationToken.None);
        var sent = await _services.Voters.SendCodesAsync(meeting.Id, CancellationToken.None);
        Assert.Equal(4, sent.Sent);
        return meeting.Id;
    }
}
