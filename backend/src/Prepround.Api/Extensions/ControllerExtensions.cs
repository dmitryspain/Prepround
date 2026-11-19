using Microsoft.AspNetCore.Mvc;
using Prepround.Domain.Shared;

namespace Prepround.Api.Extensions;

public static class ControllerExtensions
{
    /// <summary>One error shape for every failed handler, so clients parse a single format: { code, message }.</summary>
    public static BadRequestObjectResult BadRequestError(this ControllerBase controller, Error error) =>
        controller.BadRequest(new { code = error.Code, message = error.Message });
}
