using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ValuesController(ILogger<ValuesController> logger) : ControllerBase
{
    [HttpGet]
    public IEnumerable<string> Get()
    {
        var userName = User.Identity?.Name!;
        logger.LogInformation("User [{userName}] is viewing values.", userName);
        return new[] { "value1", "value2" };
    }
}