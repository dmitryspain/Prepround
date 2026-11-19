using Microsoft.AspNetCore.Mvc;

namespace Prepround.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>Liveness only: deliberately does not touch the database, so it answers even without one (spec 5.3).</summary>
    [HttpGet]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("ok"));
}

public sealed record HealthResponse(string Status);
