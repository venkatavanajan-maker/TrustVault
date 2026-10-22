using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TrustVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestAuthController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "JWT authentication successful.",
            userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(
                System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value
        });
    }
}
