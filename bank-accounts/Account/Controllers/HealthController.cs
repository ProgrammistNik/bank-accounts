using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace bank_accounts.Account.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>
    /// Проверка состояния сервиса
    /// </summary>
    /// <response code="200"/>
    [HttpGet("live")]
    public IActionResult GetHealth()
    {
        return Ok("Healthy");
    }
    /// <summary>
    /// Проверка состояния RabbitMQ и работоспособности Outbox сервиса
    /// </summary>
    /// <response code="200"/>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(_ => true, cancellationToken);

        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };

        return Ok(result);
    }
}