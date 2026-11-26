using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;
using Voxa.Logic;

namespace Voxa.WebApi.Controllers;

[ApiController]
[Authorize(Roles = AdminAuthLogic.Role)]
[Route("api/admin/club")]
public class AdminClubController(IClubLogic clubLogic) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ClubSettingsModel>> Get(CancellationToken cancellationToken) => await clubLogic.GetAsync(cancellationToken);

    [HttpPut]
    [RequestSizeLimit(2_000_000)]
    public async Task<ActionResult<ApiResponse<ClubSettingsModel>>> Update(ClubSettingsRequest request, CancellationToken cancellationToken) =>
        new ApiResponse<ClubSettingsModel>(await clubLogic.UpdateAsync(request, cancellationToken), "Settings saved");
}

[ApiController]
[Authorize(Roles = AdminAuthLogic.Role)]
[Route("api/admin/meetings")]
public class AdminMeetingsController(IMeetingLogic meetingLogic, IAgendaLogic agendaLogic, IResultLogic resultLogic) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MeetingSummaryModel>>> List(CancellationToken cancellationToken) =>
        Ok(await meetingLogic.ListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MeetingModel>> Get(int id, CancellationToken cancellationToken) => await meetingLogic.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<MeetingModel>> Create(MeetingRequest request, CancellationToken cancellationToken)
    {
        var meeting = await meetingLogic.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = meeting.Id }, meeting);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MeetingModel>> Update(int id, MeetingRequest request, CancellationToken cancellationToken) =>
        await meetingLogic.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await meetingLogic.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>A new draft with the same agenda, a year later.</summary>
    [HttpPost("{id:int}/copy")]
    public async Task<ActionResult<MeetingModel>> Copy(int id, CancellationToken cancellationToken) => await meetingLogic.CopyAsync(id, cancellationToken);

    /// <summary>Closes every item and deletes the codes and the voter list; only the totals remain.</summary>
    [HttpPost("{id:int}/archive")]
    public async Task<ActionResult<MeetingModel>> Archive(int id, CancellationToken cancellationToken) => await meetingLogic.ArchiveAsync(id, cancellationToken);

    [HttpPost("{id:int}/items")]
    public async Task<ActionResult<AgendaItemModel>> AddItem(int id, AgendaItemRequest request, CancellationToken cancellationToken) =>
        await agendaLogic.AddAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/items/order")]
    public async Task<ActionResult<IReadOnlyList<AgendaItemModel>>> Reorder(int id, ItemOrderRequest request, CancellationToken cancellationToken) =>
        Ok(await agendaLogic.ReorderAsync(id, request, cancellationToken));

    /// <summary>Turnout per item; results only for closed items.</summary>
    [HttpGet("{id:int}/live")]
    public async Task<ActionResult<LiveModel>> Live(int id, CancellationToken cancellationToken) => await resultLogic.LiveAsync(id, cancellationToken);

    [HttpPut("{id:int}/screen")]
    public async Task<IActionResult> Screen(int id, ScreenRequest request, CancellationToken cancellationToken)
    {
        await resultLogic.SetScreenAsync(id, request, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = AdminAuthLogic.Role)]
[Route("api/admin/items")]
public class AdminItemsController(IAgendaLogic agendaLogic, IResultLogic resultLogic) : ControllerBase
{
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AgendaItemModel>> Update(int id, AgendaItemRequest request, CancellationToken cancellationToken) =>
        await agendaLogic.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await agendaLogic.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/open")]
    public async Task<ActionResult<AgendaItemModel>> Open(int id, CancellationToken cancellationToken) => await agendaLogic.OpenAsync(id, cancellationToken);

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<AgendaItemModel>> Close(int id, CancellationToken cancellationToken) => await agendaLogic.CloseAsync(id, cancellationToken);

    /// <summary>The result of a closed item. 403 while the item is still open or waiting.</summary>
    [HttpGet("{id:int}/result")]
    public async Task<ActionResult<ResultModel>> Result(int id, CancellationToken cancellationToken) => await resultLogic.ResultAsync(id, cancellationToken);

    [HttpPut("{id:int}/paper")]
    public async Task<ActionResult<ResultModel>> Paper(int id, PaperVotesRequest request, CancellationToken cancellationToken) =>
        await resultLogic.SetPaperVotesAsync(id, request, cancellationToken);
}

[ApiController]
[Authorize(Roles = AdminAuthLogic.Role)]
[Route("api/admin/meetings/{meetingId:int}/voters")]
public class AdminVotersController(IVoterLogic voterLogic) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VoterListModel>> List(int meetingId, CancellationToken cancellationToken) => await voterLogic.ListAsync(meetingId, cancellationToken);

    [HttpPost("import")]
    [RequestSizeLimit(4_000_000)]
    public async Task<ActionResult<VoterImportResultModel>> Import(int meetingId, VoterImportRequest request, CancellationToken cancellationToken) =>
        await voterLogic.ImportAsync(meetingId, request, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<VoterModel>> Add(int meetingId, VoterRequest request, CancellationToken cancellationToken) =>
        await voterLogic.AddAsync(meetingId, request, cancellationToken);

    [HttpDelete("{voterId:int}")]
    public async Task<IActionResult> Delete(int voterId, CancellationToken cancellationToken)
    {
        await voterLogic.DeleteAsync(voterId, cancellationToken);
        return NoContent();
    }

    [HttpPost("send")]
    public async Task<ActionResult<SendCodesResultModel>> Send(int meetingId, CancellationToken cancellationToken) =>
        await voterLogic.SendCodesAsync(meetingId, cancellationToken);

    /// <summary>Codes for paper slips, shown once.</summary>
    [HttpPost("paper")]
    public async Task<ActionResult<PaperCodesModel>> Paper(int meetingId, PaperCodesRequest request, CancellationToken cancellationToken) =>
        await voterLogic.CreatePaperCodesAsync(meetingId, request, cancellationToken);

    [HttpPost("test")]
    public async Task<IActionResult> Test(int meetingId, TestMailRequest request, CancellationToken cancellationToken)
    {
        await voterLogic.SendTestMailAsync(meetingId, request, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize(Roles = AdminAuthLogic.Role)]
[Route("api/admin/data")]
public class AdminDataController(IMeetingLogic meetingLogic, ITrialLogic trialLogic) : ControllerBase
{
    /// <summary>The totals of every meeting, to keep before the data is wiped.</summary>
    [HttpGet("export")]
    public async Task<ActionResult<ExportModel>> Export(CancellationToken cancellationToken) => await meetingLogic.ExportAsync(cancellationToken);

    /// <summary>Deletes every meeting, code, voter and total. The club settings stay.</summary>
    [HttpPost("wipe")]
    public async Task<IActionResult> Wipe(WipeRequest request, CancellationToken cancellationToken)
    {
        await meetingLogic.WipeAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Trial setup only: a demo meeting whose codes go to the test mailbox.</summary>
    [HttpPost("demo")]
    public async Task<ActionResult<MeetingModel>> Demo(CancellationToken cancellationToken) => await trialLogic.CreateDemoAsync(cancellationToken);

    /// <summary>Trial setup only: the mails in the test mailbox, newest first.</summary>
    [HttpGet("outbox")]
    public ActionResult<IReadOnlyList<OutboxMailModel>> Outbox() => Ok(trialLogic.Outbox());
}
