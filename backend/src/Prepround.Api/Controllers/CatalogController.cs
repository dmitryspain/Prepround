using Microsoft.AspNetCore.Mvc;
using Prepround.Api.Extensions;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Catalog;

namespace Prepround.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly IQueryHandler<GetCatalogQuery, CatalogResponse> _handler;

    public CatalogController(IQueryHandler<GetCatalogQuery, CatalogResponse> handler)
    {
        _handler = handler;
    }

    /// <summary>Active catalog for the test setup screen (spec 5.1).</summary>
    [HttpGet]
    public async Task<ActionResult<CatalogResponse>> Get(CancellationToken cancellationToken)
    {
        var result = await _handler.Handle(new GetCatalogQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.BadRequestError(result.Error);
    }
}
