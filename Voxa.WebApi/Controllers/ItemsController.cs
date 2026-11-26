using Microsoft.AspNetCore.Mvc;
using Voxa.Abstractions.Logic;
using Voxa.Abstractions.Models;

namespace Voxa.WebApi.Controllers;

/// <summary>
/// An example resource. Controllers stay thin: they bind the request, call logic and return its model. Rules,
/// visibility and errors (domain exceptions) live in the logic layer.
/// </summary>
[ApiController]
[Route("api/items")]
public class ItemsController(IItemLogic itemLogic) : ControllerBase
{
    /// <summary>Every item, open items first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemModel>>> List(CancellationToken cancellationToken) =>
        Ok(await itemLogic.ListAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemModel>> Get(int id, CancellationToken cancellationToken) =>
        await itemLogic.GetAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ItemModel>>> Create(ItemRequest request, CancellationToken cancellationToken)
    {
        var item = await itemLogic.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, new ApiResponse<ItemModel>(item, "Item created"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ItemModel>>> Update(int id, ItemRequest request, CancellationToken cancellationToken) =>
        new ApiResponse<ItemModel>(await itemLogic.UpdateAsync(id, request, cancellationToken), "Item saved");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await itemLogic.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
