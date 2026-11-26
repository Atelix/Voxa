using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>Meetings for the board: create, copy, archive, export and wipe.</summary>
public interface IMeetingLogic
{
    Task<IReadOnlyList<MeetingSummaryModel>> ListAsync(CancellationToken cancellationToken);

    /// <exception cref="ObjectNotFoundException">No meeting has this id.</exception>
    Task<MeetingModel> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <exception cref="DomainValidationException">The request breaks a rule, such as advance voting opening too early.</exception>
    Task<MeetingModel> CreateAsync(MeetingRequest request, CancellationToken cancellationToken);

    /// <exception cref="ObjectNotFoundException">No meeting has this id.</exception>
    /// <exception cref="DomainValidationException">The request breaks a rule.</exception>
    /// <exception cref="DomainConflictException">The meeting is archived.</exception>
    Task<MeetingModel> UpdateAsync(Guid id, MeetingRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes a draft meeting. A meeting with codes is archived instead.</summary>
    /// <exception cref="DomainConflictException">Codes have been sent for this meeting.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A new draft with the same agenda, a year later.</summary>
    Task<MeetingModel> CopyAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Closes every item and deletes the codes and the voter list. Only the totals remain.</summary>
    Task<MeetingModel> ArchiveAsync(Guid id, CancellationToken cancellationToken);

    Task<ExportModel> ExportAsync(CancellationToken cancellationToken);

    /// <summary>Deletes every meeting, code, voter and total, for when the tool goes offline for the year.</summary>
    /// <exception cref="DomainValidationException">The confirmation word is wrong.</exception>
    Task WipeAsync(WipeRequest request, CancellationToken cancellationToken);
}
