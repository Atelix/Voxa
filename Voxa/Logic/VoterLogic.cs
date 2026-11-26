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
    IEnumerable<IMemberSource> sources,
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
        return await ReplaceAsync(meetingId, MemberCsv.Parse(request.Csv), cancellationToken);
    }

    public async Task<VoterImportResultModel> ImportFromSourceAsync(int meetingId, CancellationToken cancellationToken)
    {
        var settings = await club.GetAsync(cancellationToken);
        var source = (settings.MemberSource == MemberSourceKind.Keeva ? sources.OfType<KeevaMemberSource>().FirstOrDefault() : null)
                     ?? throw new DomainValidationException("No member administration is set up; upload a CSV instead.");
        await EnsureMeetingAsync(meetingId, cancellationToken);
        return await ReplaceAsync(meetingId, await source.FetchAsync(cancellationToken), cancellationToken);
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
        RandomNumberGenerator.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(pending));

        int sent = 0, failed = 0;
        foreach (var voter in pending)
        {
            var code = codes.Generate();
            var message = CodeMail.Build(meeting, settings, voter, voter.Email!, code, app.Value);
            if (!await mail.SendAsync(message, cancellationToken))
            {
                failed++;
                continue;
            }

            Issue(meeting, code);
            voter.CodeSent = true;
            if (++sent % SaveEvery == 0)
            {
                _ = await context.SaveChangesAsync(cancellationToken);
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
        var message = CodeMail.Build(meeting, settings, example, request.Email, codes.Generate(), app.Value);
        if (!await mail.SendAsync(message with { Subject = "[Test] " + message.Subject }, cancellationToken))
        {
            throw new DomainValidationException("The mail provider refused the test mail.");
        }
    }

    /// <summary>Applies the club's rules (suspension, minors) to a member list and makes it the meeting's voter list.</summary>
    private async Task<VoterImportResultModel> ReplaceAsync(int meetingId, IReadOnlyList<ImportedMember> members, CancellationToken cancellationToken)
    {
        var meeting = await EnsureMeetingAsync(meetingId, cancellationToken);
        var existing = await context.Voters.Where(v => v.MeetingId == meetingId).ToListAsync(cancellationToken);
        if (existing.Any(static v => v.CodeSent))
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
            if (voters.Any(v => string.Equals(v.Name, voter.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(v.Email, voter.Email, StringComparison.OrdinalIgnoreCase)))
            {
                skipped.Add($"{member.Name}: dubbel");
                continue;
            }

            voters.Add(voter);
        }

        if (voters.Count == 0)
        {
            throw new DomainValidationException("The list has no members entitled to vote.");
        }

        context.Voters.RemoveRange(existing);
        context.Voters.AddRange(voters);
        _ = await context.SaveChangesAsync(cancellationToken);
        return new VoterImportResultModel
        {
            Imported = voters.Count,
            ViaGuardian = voters.Count(static v => v.GuardianName is not null),
            WithoutEmail = voters.Count(static v => v.Email is null),
            Skipped = skipped,
        };
    }

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
