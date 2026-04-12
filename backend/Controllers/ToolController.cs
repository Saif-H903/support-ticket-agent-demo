using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
public sealed class ToolController : ControllerBase
{
    private readonly BestelStatusService _bestelStatusService;

    public ToolController(BestelStatusService bestelStatusService)
    {
        _bestelStatusService = bestelStatusService;
    }

    [HttpGet("api/tools/bestelstatus/{bestellingId}")]
    [HttpGet("api/tool/order-status/{bestellingId}")]
    public ActionResult<BestelStatusResultaat> HaalBestelStatus(string bestellingId)
    {
        return Ok(_bestelStatusService.HaalStatusOp(bestellingId));
    }
}
