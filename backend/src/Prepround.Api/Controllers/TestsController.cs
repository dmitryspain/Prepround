using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Prepround.Api.Extensions;
using Prepround.Api.RateLimiting;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.TestGeneration;

namespace Prepround.Api.Controllers;

[ApiController]
[Route("api/tests")]
public class TestsController : ControllerBase
{
    /// <summary>1 MB: room for 10 000 excluded ids, far below the server default of 30 MB.</summary>
    private const long MaxRequestBodySize = 1024 * 1024;

    private readonly IQueryHandler<GenerateTestQuery, GeneratedTestResponse> _handler;

    public TestsController(IQueryHandler<GenerateTestQuery, GeneratedTestResponse> handler)
    {
        _handler = handler;
    }

    /// <summary>A new test by the user's settings, with correct answers and explanations (spec 5.2).</summary>
    /// <param name="query">The body; its shape does not depend on the language.</param>
    /// <param name="language"><c>?lang=</c>; the handler checks it before anything in the body.</param>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Tests)]
    [RequestSizeLimit(MaxRequestBodySize)]
    public async Task<ActionResult<GeneratedTestResponse>> Generate(
        GenerateTestQuery query,
        [FromQuery(Name = "lang")] string? language,
        CancellationToken cancellationToken)
    {
        var result = await _handler.Handle(query with { Language = language }, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.BadRequestError(result.Error);
    }
}
