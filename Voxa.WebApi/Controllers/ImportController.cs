using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;

namespace Voxa.WebApi.Controllers;

/// <summary>
/// The member import API: a member administration, or an automation such as n8n, sends the club's member list here
/// with the club's import key. See docs/import-api.md; the OpenAPI document is at <c>/api/import/openapi.json</c>.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = ImportApiKeyHandler.SchemeName, Roles = ImportApiKeyHandler.Role)]
[ApiExplorerSettings(GroupName = ImportApi.DocumentName)]
[Route("api/import")]
[Produces("application/json")]
public class ImportController(IVoterLogic voterLogic) : ControllerBase
{
    /// <summary>The meetings that can receive a member list (every meeting that is not archived), soonest first.</summary>
    [HttpGet("meetings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ImportMeetingModel>>> Meetings(CancellationToken cancellationToken) =>
        Ok(await voterLogic.ImportTargetsAsync(cancellationToken));

    /// <summary>
    /// Sends the club's current members to a meeting. Until the first code is sent this replaces the voter list; after
    /// that it only adds members whose name is not on the list yet, and nobody is removed. Voxa
    /// applies the club's rules itself: suspended members and, depending on the settings, minors are left out, and a
    /// minor's code can go to the parent or guardian.
    /// </summary>
    /// <response code="200">What the import did; <c>skipped</c> lists who was left out and why.</response>
    /// <response code="400">The body is invalid, or no member in it is entitled to vote.</response>
    /// <response code="401">The key is missing or wrong.</response>
    /// <response code="404">The meeting does not exist.</response>
    /// <response code="409">The meeting is archived.</response>
    /// <response code="429">Too many wrong keys from this address; try again after Retry-After seconds.</response>
    [HttpPost("meetings/{meetingId:int}/members")]
    [RequestSizeLimit(8_000_000)]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<VoterImportResultModel>> Members(int meetingId, MemberImportRequest request, CancellationToken cancellationToken) =>
        await voterLogic.ImportMembersAsync(meetingId, request, cancellationToken);
}
