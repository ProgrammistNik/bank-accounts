using Microsoft.AspNetCore.Mvc;

namespace bank_accounts.Account.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok("Healthy");
    }
}