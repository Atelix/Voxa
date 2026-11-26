using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>The voters of a meeting and their codes.</summary>
public interface IVoterLogic
{
    Task<VoterListModel> ListAsync(Guid meetingId, CancellationToken cancellationToken);

    /// <summary>Replaces the voter list with the members in a CSV export (from a member administration or a spreadsheet).</summary>
    /// <exception cref="DomainValidationException">The CSV has no name column, or no rows.</exception>
    /// <exception cref="DomainConflictException">Codes have already been sent; add members one by one instead.</exception>
    Task<VoterImportResultModel> ImportAsync(Guid meetingId, VoterImportRequest request, CancellationToken cancellationToken);

    /// <summary>The meetings the import API can send a member list to, soonest first.</summary>
    Task<IReadOnlyList<ImportMeetingModel>> ImportTargetsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Takes the member list a member administration sends. Until the first code is sent it replaces the voter list;
    /// after that it only adds members whose name is not on the list yet, so a later push never takes away a code's voter.
    /// </summary>
    /// <exception cref="ObjectNotFoundException">The meeting does not exist.</exception>
    /// <exception cref="DomainConflictException">The meeting is archived.</exception>
    /// <exception cref="DomainValidationException">Replacing, and no member is entitled to vote.</exception>
    Task<VoterImportResultModel> ImportMembersAsync(Guid meetingId, MemberImportRequest request, CancellationToken cancellationToken);

    Task<VoterModel> AddAsync(Guid meetingId, VoterRequest request, CancellationToken cancellationToken);

    /// <exception cref="DomainConflictException">This voter already received a code.</exception>
    Task DeleteAsync(Guid voterId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a code for every voter with an address who has none yet and mails it. Only the hash is stored, and
    /// nothing records which code went to whom.
    /// </summary>
    Task<SendCodesResultModel> SendCodesAsync(Guid meetingId, CancellationToken cancellationToken);

    /// <summary>Creates codes for paper slips, marking that many voters without an address as served.</summary>
    Task<PaperCodesModel> CreatePaperCodesAsync(Guid meetingId, PaperCodesRequest request, CancellationToken cancellationToken);

    /// <summary>Sends an example code mail with a code that does not work.</summary>
    Task SendTestMailAsync(Guid meetingId, TestMailRequest request, CancellationToken cancellationToken);
}
