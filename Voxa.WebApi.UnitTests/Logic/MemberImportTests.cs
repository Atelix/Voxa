using Voxa.Abstractions;
using Voxa.Abstractions.Models;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.WebApi.UnitTests.Logic;

/// <summary>The member list a member administration sends through the import API, with the club's rules applied.</summary>
public sealed class MemberImportTests : IDisposable
{
    private static readonly DateTime s_meetingStart = new(2027, 2, 2, 17, 45, 0, DateTimeKind.Utc);
    private readonly TestServices _services = new(new DateTimeOffset(2027, 2, 1, 9, 0, 0, TimeSpan.Zero));

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task TheListReplacesTheVotersAndAppliesTheClubsRules()
    {
        var meetingId = await MeetingAsync();
        _ = await _services.Voters.AddAsync(meetingId, new VoterRequest { Name = "Oud Lid" }, CancellationToken.None);

        var result = await _services.Voters.ImportMembersAsync(meetingId, Request(
            Member("Anna Adult", "anna@example.org", new DateOnly(1990, 1, 1)),
            Member("Kees Kind", null, new DateOnly(2014, 3, 3), new GuardianImportItem { Name = "Petra Ouder", Email = "petra@example.org" }),
            Suspended("Gerda Geschorst", "gerda@example.org"),
            Member("Anna Adult", "ANNA@example.org", new DateOnly(1990, 1, 1)),
            Member("Zonder Adres", null, null)), CancellationToken.None);

        Assert.Equal(VoterImportMode.Replaced, result.Mode);
        Assert.Equal(3, result.Imported);
        Assert.Equal(1, result.ViaGuardian);
        Assert.Equal(1, result.WithoutEmail);
        Assert.Equal(["Gerda Geschorst: suspended", "Anna Adult: duplicate"], result.Skipped);
        var list = await _services.Voters.ListAsync(meetingId, CancellationToken.None);
        Assert.DoesNotContain(list.Voters, static v => v.Name == "Oud Lid");
        var kees = Assert.Single(list.Voters, static v => v.Name == "Kees Kind");
        Assert.Equal("petra@example.org", kees.Email);
        Assert.Equal("Petra Ouder", kees.GuardianName);
    }

    [Fact]
    public async Task TheCodeGoesToTheFirstGuardianWithAnAddress()
    {
        var meetingId = await MeetingAsync();

        _ = await _services.Voters.ImportMembersAsync(meetingId, Request(
            Member("Kees Kind", "kees@example.org", new DateOnly(2014, 3, 3),
                new GuardianImportItem { Name = "Zonder Adres" },
                new GuardianImportItem { Name = "Piet Ouder", Email = "piet@example.org" }),
            Member("Lies Kind", null, new DateOnly(2015, 3, 3), new GuardianImportItem { Email = "ouders@example.org" })), CancellationToken.None);

        var list = await _services.Voters.ListAsync(meetingId, CancellationToken.None);
        var kees = Assert.Single(list.Voters, static v => v.Name == "Kees Kind");
        Assert.Equal("piet@example.org", kees.Email);
        Assert.Equal("Piet Ouder", kees.GuardianName);
        var lies = Assert.Single(list.Voters, static v => v.Name == "Lies Kind");
        Assert.Equal("ouders@example.org", lies.Email);
        Assert.Equal("ouder of verzorger", lies.GuardianName);
    }

    [Fact]
    public async Task MinorsAreLeftOutWhenTheyHaveNoVote()
    {
        var club = ClubSettingsStore.Defaults();
        club.MinorVoting = MinorVoting.None;
        _ = _services.Context.ClubSettings.Add(club);
        _ = await _services.Context.SaveChangesAsync();
        var meetingId = await MeetingAsync();

        var result = await _services.Voters.ImportMembersAsync(meetingId, Request(
            Member("Anna Adult", "anna@example.org", new DateOnly(1990, 1, 1)),
            Member("Kees Kind", "kees@example.org", new DateOnly(2014, 3, 3))), CancellationToken.None);

        Assert.Equal(1, result.Imported);
        Assert.Equal(["Kees Kind: minor"], result.Skipped);
    }

    [Fact]
    public async Task AfterTheCodesAreSentOnlyNewMembersAreAdded()
    {
        var meetingId = await MeetingAsync();
        _ = await _services.Voters.ImportMembersAsync(meetingId, Request(
            Member("Anna Adult", "anna@example.org", null),
            Member("Bert Adult", "bert@example.org", null)), CancellationToken.None);
        _ = await _services.Voters.SendCodesAsync(meetingId, CancellationToken.None);

        // Bert left the club, Anna's address changed and Carla joined: Bert keeps his code, Anna gets no second one and
        // Carla is added without one.
        var result = await _services.Voters.ImportMembersAsync(meetingId, Request(
            Member("Anna Adult", "anna.nieuw@example.org", null),
            Member("Carla Nieuw", "carla@example.org", null)), CancellationToken.None);

        Assert.Equal(VoterImportMode.Added, result.Mode);
        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.AlreadyListed);
        var list = await _services.Voters.ListAsync(meetingId, CancellationToken.None);
        Assert.Equal(["Anna Adult", "Bert Adult", "Carla Nieuw"], list.Voters.Select(static v => v.Name));
        Assert.False(list.Voters.Single(static v => v.Name == "Carla Nieuw").CodeSent);
        Assert.Equal(1, (await _services.Voters.SendCodesAsync(meetingId, CancellationToken.None)).Sent);
    }

    [Fact]
    public async Task AListWithoutVotersIsRefusedButAddingNothingIsFine()
    {
        var meetingId = await MeetingAsync();

        _ = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _services.Voters.ImportMembersAsync(meetingId, Request(Suspended("Gerda Geschorst", "gerda@example.org")), CancellationToken.None));

        _ = await _services.Voters.ImportMembersAsync(meetingId, Request(Member("Anna Adult", "anna@example.org", null)), CancellationToken.None);
        _ = await _services.Voters.SendCodesAsync(meetingId, CancellationToken.None);
        var again = await _services.Voters.ImportMembersAsync(meetingId, Request(Member("Anna Adult", "anna@example.org", null)), CancellationToken.None);
        Assert.Equal(0, again.Imported);
        Assert.Equal(1, again.AlreadyListed);
    }

    [Fact]
    public async Task TheCsvImportStillRefusesAReplaceAfterTheCodesAreSent()
    {
        var meetingId = await MeetingAsync();
        _ = await _services.Voters.ImportMembersAsync(meetingId, Request(Member("Anna Adult", "anna@example.org", null)), CancellationToken.None);
        _ = await _services.Voters.SendCodesAsync(meetingId, CancellationToken.None);

        _ = await Assert.ThrowsAsync<DomainConflictException>(() =>
            _services.Voters.ImportAsync(meetingId, new VoterImportRequest { Csv = "naam;email\nCarla Nieuw;carla@example.org" }, CancellationToken.None));
    }

    [Fact]
    public async Task TheTargetsAreTheMeetingsThatAreNotArchivedWithTheirImportMode()
    {
        var later = await MeetingAsync(s_meetingStart.AddDays(30));
        var sooner = await MeetingAsync();
        var archived = await MeetingAsync(s_meetingStart.AddDays(60));
        _ = await _services.Voters.ImportMembersAsync(sooner, Request(Member("Anna Adult", "anna@example.org", null)), CancellationToken.None);
        _ = await _services.Voters.SendCodesAsync(sooner, CancellationToken.None);
        _ = await _services.Meetings.ArchiveAsync(archived, CancellationToken.None);

        var targets = await _services.Voters.ImportTargetsAsync(CancellationToken.None);

        Assert.Equal([sooner, later], targets.Select(static t => t.Id));
        Assert.Equal(VoterImportMode.Added, targets[0].ImportMode);
        Assert.Equal(1, targets[0].VoterCount);
        Assert.Equal(VoterImportMode.Replaced, targets[1].ImportMode);
        _ = await Assert.ThrowsAsync<DomainConflictException>(() =>
            _services.Voters.ImportMembersAsync(archived, Request(Member("Anna Adult", "anna@example.org", null)), CancellationToken.None));
    }

    private static MemberImportRequest Request(params MemberImportItem[] members) => new() { Members = members };

    private static MemberImportItem Member(string name, string? email, DateOnly? birthDate, params GuardianImportItem[] guardians) => new()
    {
        Name = name,
        Email = email,
        BirthDate = birthDate,
        Guardians = guardians,
    };

    private static MemberImportItem Suspended(string name, string email) => new() { Name = name, Email = email, Suspended = true };

    private async Task<int> MeetingAsync(DateTime? startsAt = null)
    {
        var meeting = await _services.Meetings.CreateAsync(new MeetingRequest { Name = "ALV 2027", StartsAt = startsAt ?? s_meetingStart }, CancellationToken.None);
        return meeting.Id;
    }
}
