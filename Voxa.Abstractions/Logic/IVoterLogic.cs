using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>The voters of a meeting and their codes.</summary>
public interface IVoterLogic
{
    Task<VoterListModel> ListAsync(int meetingId, CancellationToken cancellationToken);

    /// <summary>Replaces the voter list with the members in a CSV export (from Keeva or a spreadsheet).</summary>
    /// <exception cref="DomainValidationException">The CSV has no name column, or no rows.</exception>
    /// <exception cref="DomainConflictException">Codes have already been sent; add members one by one instead.</exception>
    Task<VoterImportResultModel> ImportAsync(int meetingId, VoterImportRequest request, CancellationToken cancellationToken);

    /// <summary>Replaces the voter list with the current members from the club's member administration (Keeva).</summary>
    /// <exception cref="DomainValidationException">No member administration is set up, or it refused the request.</exception>
    /// <exception cref="DomainConflictException">Codes have already been sent.</exception>
    Task<VoterImportResultModel> ImportFromSourceAsync(int meetingId, CancellationToken cancellationToken);

    Task<VoterModel> AddAsync(int meetingId, VoterRequest request, CancellationToken cancellationToken);

    /// <exception cref="DomainConflictException">This voter already received a code.</exception>
    Task DeleteAsync(int voterId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a code for every voter with an address who has none yet and mails it. Only the hash is stored, and
    /// nothing records which code went to whom.
    /// </summary>
    Task<SendCodesResultModel> SendCodesAsync(int meetingId, CancellationToken cancellationToken);

    /// <summary>Creates codes for paper slips, marking that many voters without an address as served.</summary>
    Task<PaperCodesModel> CreatePaperCodesAsync(int meetingId, PaperCodesRequest request, CancellationToken cancellationToken);

    /// <summary>Sends an example code mail with a code that does not work.</summary>
    Task SendTestMailAsync(int meetingId, TestMailRequest request, CancellationToken cancellationToken);
}
