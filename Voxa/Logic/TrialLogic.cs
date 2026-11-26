using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Abstractions.Services;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

/// <summary>
/// The trial setup. It only works while mail goes to the in-memory test mailbox, so demo codes can never reach a
/// member: the test voters have example.org addresses, and the mailbox shows their codes to the board.
/// </summary>
public sealed class TrialLogic(IMeetingLogic meetings, IAgendaLogic agenda, IVoterLogic voters, IMailSender mail, IOutbox? outbox, TimeProvider time) : ITrialLogic
{
    public async Task<MeetingModel> CreateDemoAsync(CancellationToken cancellationToken)
    {
        EnsureTrial();
        var today = ClubTime.ToLocal(time.GetUtcNow().UtcDateTime).Date;
        var startsAt = ClubTime.ToUtc(today.AddDays(3).AddHours(19).AddMinutes(30));
        var meeting = await meetings.CreateAsync(new MeetingRequest
        {
            Name = Texts.Format("Trial.MeetingName", today.Year),
            Kind = MeetingKind.Annual,
            StartsAt = startsAt,
            Location = Texts.Get("Trial.Location"),
            AdvanceVoting = true,
            AdvanceOpensAt = ClubTime.ToUtc(today),
        }, cancellationToken);

        static AgendaItemRequest Proposal(string title, string description, MajorityRule majority = MajorityRule.Absolute, bool advance = true) =>
            new() { Title = title, Description = description, Kind = AgendaItemKind.Proposal, Majority = majority, AdvanceVoting = advance };

        // The demo is written in the board's language: the language of this request.
        _ = await agenda.AddAsync(meeting.Id, Proposal(Texts.Get("Trial.MinutesTitle"), Texts.Get("Trial.MinutesText")), cancellationToken);
        _ = await agenda.AddAsync(meeting.Id, Proposal(Texts.Get("Trial.BudgetTitle"), Texts.Get("Trial.BudgetText")), cancellationToken);
        _ = await agenda.AddAsync(meeting.Id, Proposal(Texts.Get("Trial.StatutesTitle"), Texts.Get("Trial.StatutesText"), MajorityRule.TwoThirds, advance: false), cancellationToken);
        _ = await agenda.AddAsync(meeting.Id, new AgendaItemRequest
        {
            Title = Texts.Get("Trial.ElectionTitle"),
            Description = Texts.Get("Trial.ElectionText"),
            Kind = AgendaItemKind.Election,
            Seats = 2,
            AdvanceVoting = true,
            Candidates =
            [
                new() { Label = Texts.Format("Trial.Candidate", "A"), Note = Texts.Get("Trial.Reelection") },
                new() { Label = Texts.Format("Trial.Candidate", "B"), Note = Texts.Get("Trial.NewCandidate") },
                new() { Label = Texts.Format("Trial.Candidate", "C"), Note = Texts.Get("Trial.NewCandidate") },
            ],
        }, cancellationToken);

        var csv = string.Join("\n",
            "naam;email;geboortedatum;ouder naam;ouder email;status",
            "Test Lid 1;lid1@example.org;1980-04-01;;;",
            "Test Lid 2;lid2@example.org;1975-09-12;;;",
            "Test Lid 3;lid3@example.org;1990-01-30;;;",
            "Test Lid 4;lid4@example.org;1988-06-15;;;",
            "Test Lid 5;lid5@example.org;2001-11-03;;;",
            $"Test Jeugdlid;;{today.Year - 12}-05-05;Test Ouder;ouder@example.org;",
            $"Test Jeugdlid 2;;{today.Year - 9}-08-08;Test Ouder;ouder@example.org;",
            "Test Lid zonder e-mail;;1970-02-02;;;",
            "Test Geschorst Lid;geschorst@example.org;1985-03-03;;;geschorst");
        _ = await voters.ImportAsync(meeting.Id, new VoterImportRequest { Csv = csv }, cancellationToken);
        _ = await voters.SendCodesAsync(meeting.Id, cancellationToken);
        return await meetings.GetAsync(meeting.Id, cancellationToken);
    }

    public IReadOnlyList<OutboxMailModel> Outbox()
    {
        EnsureTrial();
        return outbox?.Messages() ?? [];
    }

    private void EnsureTrial()
    {
        if (!mail.IsTrial)
        {
            throw new DomainForbiddenException(Texts.Get("Error.TrialOnly"));
        }
    }
}
