// ...existing code...

using Microsoft.AspNetCore.Mvc;

namespace KunstButikken.UserService.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
  [HttpGet("claims")]
  public IActionResult GetClaims()
  {
    return Ok(new
    {
      IsAuthenticated = false,
      AuthenticationType = "None",
      Name = "Anonymous",
      Claims = Array.Empty<object>()
    });
  }
}

// ...existing code...
