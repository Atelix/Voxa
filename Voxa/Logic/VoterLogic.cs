using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Abstractions.Services;
using Voxa.Persistence.Entity;
using Voxa.Services;

namespace Voxa.Logic;

/// <summary>
/// The voter list and the codes. A code exists in readable form only in memory while its mail is sent: the database
/// gets its hash, and nothing records which voter it went to. Voters are sent in random order, so the order of the
/// stored hashes says nothing about the order of the list either.
/// </summary>
public sealed class VoterLogic(
    VoxaContext context,
    ClubSettingsStore club,
    ICodeService codes,
    IMailSender mail,
    IOptions<AppOptions> app) : IVoterLogic
{
    private const int SaveEvery = 25;

    public async Task<VoterListModel> ListAsync(int meetingId, CancellationToken cancellationToken)
    {
        await EnsureMeetingAsync(meetingId, cancellationToken);
        var voters = await context.Voters.AsNoTracking().Where(v => v.MeetingId == meetingId).OrderBy(static v => v.Name).ToListAsync(cancellationToken);
        return new VoterListModel
        {
            Total = voters.Count,
            ViaGuardian = voters.Count(static v => v.GuardianName is not null),
            WithoutEmail = voters.Count(static v => v.Email is null),
            CodesSent = voters.Count(static v => v.CodeSent),
            Voters = [.. voters.Select(ToModel)],
        };
    }

    public async Task<VoterImportResultModel> ImportAsync(int meetingId, VoterImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await ApplyAsync(meetingId, MemberCsv.Parse(request.Csv), addAfterCodes: false, cancellationToken);
    }

    public async Task<IReadOnlyList<ImportMeetingModel>> ImportTargetsAsync(CancellationToken cancellationToken)
    {
        var meetings = await context.Meetings.AsNoTracking()
                                    .Where(static m => m.Status != MeetingStatus.Archived)
                                    .OrderBy(static m => m.StartsAt)
                                    .Select(m => new
                                    {
                                        m.Id,
                                        m.Name,
                                        m.StartsAt,
                                        m.Status,
                                        Voters = context.Voters.Count(v => v.MeetingId == m.Id),
                                        CodesSent = context.Voters.Any(v => v.MeetingId == m.Id && v.CodeSent),
                                    })
                                    .ToListAsync(cancellationToken);
        return [.. meetings.Select(static m => new ImportMeetingModel
        {
            Id = m.Id,
            Name = m.Name,
            StartsAt = m.StartsAt,
            Status = m.Status,
            VoterCount = m.Voters,
            ImportMode = m.CodesSent ? VoterImportMode.Added : VoterImportMode.Replaced,
        })];
    }

    public async Task<VoterImportResultModel> ImportMembersAsync(int meetingId, MemberImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var members = request.Members.Select(static m =>
        {
            // The code goes to the first parent or guardian with an address, and the mail greets that person.
            var guardian = m.Guardians?.FirstOrDefault(static g => Mapping.Clean(g.Email) is not null) ?? m.Guardians?.FirstOrDefault();
            return new ImportedMember
            {
                Name = m.Name,
                Email = m.Email,
                BirthDate = m.BirthDate,
                Suspended = m.Suspended,
                GuardianName = guardian?.Name,
                GuardianEmail = guardian?.Email,
            };
        }).ToList();
        return await ApplyAsync(meetingId, members, addAfterCodes: true, cancellationToken);
    }

    public async Task<VoterModel> AddAsync(int meetingId, VoterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meeting = await EnsureMeetingAsync(meetingId, cancellationToken);
        var voter = new Voter
        {
            MeetingId = meeting.Id,
            Name = Mapping.Clean(request.Name) ?? throw new DomainValidationException("A voter needs a name."),
            Email = Mapping.Clean(request.Email),
            GuardianName = Mapping.Clean(request.GuardianName),
        };
        _ = context.Voters.Add(voter);
        _ = await context.SaveChangesAsync(cancellationToken);
        return ToModel(voter);
    }

    public async Task DeleteAsync(int voterId, CancellationToken cancellationToken)
    {
        var voter = await context.Voters.FirstOrDefaultAsync(v => v.Id == voterId, cancellationToken)
                    ?? throw new ObjectNotFoundException(nameof(Voter), voterId);
        if (voter.CodeSent)
        {
            throw new DomainConflictException("This voter already received a code.");
        }

        _ = context.Voters.Remove(voter);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SendCodesResultModel> SendCodesAsync(int meetingId, CancellationToken cancellationToken)
    {
        var meeting = await EnsureMeetingAsync(meetingId, cancellationToken);
        var settings = await club.GetAsync(cancellationToken);
        EnsureSender(settings);
        var pending = await context.Voters.Where(v => v.MeetingId == meetingId && !v.CodeSent && v.Email != null).ToListAsync(cancellationToken);
        var withoutEmail = await context.Voters.CountAsync(v => v.MeetingId == meetingId && !v.CodeSent && v.Email == null, cancellationToken);
        // One mail per address: a parent of several youth members gets all their codes together. The mails go out in
        // random order, and so do the codes within one mail's batch, so no order in the database follows the list.
        var batches = pending.GroupBy(static v => v.Email!.Trim().ToUpperInvariant()).Select(static g => g.OrderBy(static v => v.Name).ToList()).ToArray();
        RandomNumberGenerator.Shuffle(batches.AsSpan());

        int sent = 0, failed = 0, unsaved = 0;
        foreach (var batch in batches)
        {
            var mailed = batch.Select(v => new MailedCode(v, codes.Generate())).ToArray();
            var message = CodeMail.Build(meeting, settings, mailed, batch[0].Email!, app.Value);
            if (!await mail.SendAsync(message, cancellationToken))
            {
                failed += batch.Count;
                continue;
            }

            RandomNumberGenerator.Shuffle(mailed.AsSpan());
            foreach (var (voter, code) in mailed)
            {
                Issue(meeting, code);
                voter!.CodeSent = true;
            }

            sent += batch.Count;
            unsaved += batch.Count;
            if (unsaved >= SaveEvery)
            {
                _ = await context.SaveChangesAsync(cancellationToken);
                unsaved = 0;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return new SendCodesResultModel { Sent = sent, Failed = failed, WithoutEmail = withoutEmail };
    }

    public async Task<PaperCodesModel> CreatePaperCodesAsync(int meetingId, PaperCodesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meeting = await EnsureMeetingAsync(meetingId, cancellationToken);
        var created = new List<string>(request.Count);
        for (var i = 0; i < request.Count; i++)
        {
            var code = codes.Generate();
            Issue(meeting, code);
            created.Add(code);
        }

        // The slips are for voters without an address; mark that many as served, so they are not counted twice.
        var served = await context.Voters.Where(v => v.MeetingId == meetingId && !v.CodeSent && v.Email == null)
                                  .OrderBy(static v => v.Name).Take(request.Count).ToListAsync(cancellationToken);
        foreach (var voter in served)
        {
            voter.CodeSent = true;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return new PaperCodesModel { Codes = created };
    }

    public async Task SendTestMailAsync(int meetingId, TestMailRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var meeting = await EnsureMeetingAsync(meetingId, cancellationToken);
        var settings = await club.GetAsync(cancellationToken);
        EnsureSender(settings);
        var example = new Voter { Name = "[naam lid]", GuardianName = null };
        // A fresh code that is never stored, so the example cannot be used to vote.
        var message = CodeMail.Build(meeting, settings, [new MailedCode(example, codes.Generate())], request.Email, app.Value);
        if (!await mail.SendAsync(message with { Subject = "[Test] " + message.Subject }, cancellationToken))
        {
            throw new DomainValidationException("The mail provider refused the test mail.");
        }
    }

    /// <summary>
    /// Applies the club's rules (suspension, minors) to a member list and makes it the meeting's voter list. Once codes
    /// have been sent the list is never replaced: with <paramref name="addAfterCodes"/> the members who are not on it yet
    /// are added, otherwise the import is refused.
    /// </summary>
    private async Task<VoterImportResultModel> ApplyAsync(int meetingId, IReadOnlyList<ImportedMember> members, bool addAfterCodes, CancellationToken cancellationToken)
    {
        var meeting = await EnsureMeetingAsync(meetingId, cancellationToken);
        var existing = await context.Voters.Where(v => v.MeetingId == meetingId).ToListAsync(cancellationToken);
        var adding = existing.Any(static v => v.CodeSent);
        if (adding && !addAfterCodes)
        {
            throw new DomainConflictException("Codes have been sent from this list. Add new members one by one.");
        }

        var settings = await club.GetAsync(cancellationToken);
        var meetingDay = DateOnly.FromDateTime(ClubTime.ToLocal(meeting.StartsAt));
        var skipped = new List<string>();
        var voters = new List<Voter>();
        foreach (var member in members)
        {
            if (member.Suspended)
            {
                skipped.Add($"{member.Name}: geschorst");
                continue;
            }

            var minor = member.BirthDate is { } birth && birth.AddYears(settings.AdultAge) > meetingDay;
            if (minor && settings.MinorVoting == MinorVoting.None)
            {
                skipped.Add($"{member.Name}: minderjarig");
                continue;
            }

            var viaGuardian = minor && settings.MinorVoting == MinorVoting.Guardian;
            var voter = new Voter
            {
                MeetingId = meetingId,
                Name = member.Name.Trim(),
                Email = Mapping.Clean(viaGuardian ? member.GuardianEmail : member.Email),
                GuardianName = viaGuardian ? Mapping.Clean(member.GuardianName) ?? "ouder of verzorger" : null,
            };
            if (voters.Any(v => SameVoter(v, voter)))
            {
                skipped.Add($"{member.Name}: dubbel");
                continue;
            }

            voters.Add(voter);
        }

        var alreadyListed = 0;
        if (adding)
        {
            // By name only: a member whose address changed after the codes went out must not get a second code. Two
            // members with the same name are rare; the board adds the second one by hand.
            alreadyListed = voters.RemoveAll(v => existing.Any(e => string.Equals(e.Name, v.Name, StringComparison.OrdinalIgnoreCase)));
        }
        else
        {
            if (voters.Count == 0)
            {
                throw new DomainValidationException("The list has no members entitled to vote.");
            }

            context.Voters.RemoveRange(existing);
        }

        context.Voters.AddRange(voters);
        _ = await context.SaveChangesAsync(cancellationToken);
        return new VoterImportResultModel
        {
            Mode = adding ? VoterImportMode.Added : VoterImportMode.Replaced,
            Imported = voters.Count,
            AlreadyListed = alreadyListed,
            ViaGuardian = voters.Count(static v => v.GuardianName is not null),
            WithoutEmail = voters.Count(static v => v.Email is null),
            Skipped = skipped,
        };
    }

    /// <summary>One member is one voter: the same name with the same address (or both without one) is a duplicate.</summary>
    private static bool SameVoter(Voter a, Voter b) =>
        string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Email, b.Email, StringComparison.OrdinalIgnoreCase);

    private void Issue(Meeting meeting, string code)
    {
        _ = context.VotingCodes.Add(new VotingCode { Id = Guid.NewGuid(), MeetingId = meeting.Id, Hash = codes.Hash(codes.Normalize(code)!) });
        meeting.CodesIssued++;
        meeting.Status = MeetingStatus.Ready;
    }

    private void EnsureSender(ClubSettings settings)
    {
        if (!mail.IsTrial && string.IsNullOrWhiteSpace(settings.MailFromAddress))
        {
            throw new DomainValidationException("Set the club's sending address in the settings first.");
        }
    }

    private async Task<Meeting> EnsureMeetingAsync(int meetingId, CancellationToken cancellationToken)
    {
        var meeting = await context.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken)
                      ?? throw new ObjectNotFoundException(nameof(Meeting), meetingId);
        MeetingLogic.EnsureNotArchived(meeting);
        return meeting;
    }

    private static VoterModel ToModel(Voter voter) => new()
    {
        Id = voter.Id,
        Name = voter.Name,
        Email = voter.Email,
        GuardianName = voter.GuardianName,
        CodeSent = voter.CodeSent,
    };
}
