using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;

namespace Voxa.WebApi.Controllers;

/// <summary>The club's name and brand, so the app can theme itself before anyone signs in.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/club")]
public class ClubController(IClubLogic clubLogic) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ClubBrandModel>> Brand(CancellationToken cancellationToken) =>
        await clubLogic.BrandAsync(cancellationToken);
}

/// <summary>
/// Voting with a code. The code travels in the request body, never in the address, so it does not end up in proxy or
/// browser logs.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/ballot")]
public class BallotController(IBallotLogic ballotLogic) : ControllerBase
{
    /// <summary>The meeting and its items as this code sees them: open, voted, waiting or closed.</summary>
    [HttpPost("session")]
    public async Task<ActionResult<BallotSessionModel>> Session(BallotCodeRequest request, CancellationToken cancellationToken) =>
        await ballotLogic.SessionAsync(request, cancellationToken);

    [HttpPost("votes")]
    public async Task<IActionResult> Vote(VoteRequest request, CancellationToken cancellationToken)
    {
        await ballotLogic.VoteAsync(request, cancellationToken);
        return NoContent();
    }
}

/// <summary>The hall screen: the open item and its count, or a result the board put on screen.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/screen")]
public class ScreenController(IResultLogic resultLogic) : ControllerBase
{
    [HttpGet("{meetingId:int}")]
    public async Task<ActionResult<ScreenModel>> Get(int meetingId, CancellationToken cancellationToken) =>
        await resultLogic.ScreenAsync(meetingId, cancellationToken);
}

[ApiController]
[AllowAnonymous]
[Route("api/admin/session")]
public class AdminSessionController(IAdminAuthLogic authLogic) : ControllerBase
{
    [HttpGet]
    public ActionResult<AdminSessionModel> Status() => authLogic.Status();

    [HttpPost]
    public async Task<ActionResult<AdminSessionModel>> SignIn(SignInRequest request) => await authLogic.SignInAsync(request);

    [HttpDelete]
    public async Task<ActionResult<AdminSessionModel>> SignOut() => await authLogic.SignOutAsync();
}
