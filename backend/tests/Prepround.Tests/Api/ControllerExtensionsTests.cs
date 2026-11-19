using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Prepround.Api.Extensions;
using Prepround.Domain.Shared;

namespace Prepround.Tests.Api;

public class ControllerExtensionsTests
{
    [Fact]
    public void Bad_request_error_is_400_with_exactly_code_and_message()
    {
        var controller = new ProbeController();

        var result = controller.BadRequestError(new Error("Tests.InvalidCount", "Кількість питань має бути від 10 до 100."));

        Assert.Equal(400, result.StatusCode);
        var json = JsonSerializer.SerializeToElement(result.Value, JsonSerializerOptions.Web);
        Assert.Equal(new[] { "code", "message" }, json.EnumerateObject().Select(property => property.Name));
        Assert.Equal("Tests.InvalidCount", json.GetProperty("code").GetString());
        Assert.Equal("Кількість питань має бути від 10 до 100.", json.GetProperty("message").GetString());
    }

    private sealed class ProbeController : ControllerBase;
}
