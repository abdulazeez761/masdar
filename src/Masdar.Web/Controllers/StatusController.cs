using Microsoft.AspNetCore.Authorization;

namespace Masdar.Web.Controllers;

[AllowAnonymous]
public sealed class StatusController : Controller
{
    [HttpGet("/Status/{code:int}")]
    public IActionResult Show(int code)
    {
        if (code is < 400 or > 599) return BadRequest();
        Response.StatusCode = code;
        return View("~/Views/Shared/Status.cshtml", code);
    }
}
