using Prepround.Domain.Shared;

namespace Prepround.Tests.Domain;

public class ResultTests
{
    private static readonly Error SomeError = new("Area.Code", "Повідомлення.");

    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_carries_its_error()
    {
        var result = Result.Failure(SomeError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void Failure_without_error_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));
        Assert.Throws<InvalidOperationException>(() => Result.Failure<int>(Error.None));
    }

    [Fact]
    public void Success_with_error_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new ProbeResult(isSuccess: true, SomeError));
    }

    [Fact]
    public void Value_of_success_is_returned()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Value_of_failure_throws_instead_of_returning_default()
    {
        var result = Result.Failure<int>(SomeError);

        Assert.True(result.IsFailure);
        Assert.Equal(SomeError, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Value_converts_implicitly_to_successful_result()
    {
        Result<string> result = "value";

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void Errors_with_same_code_and_message_are_equal()
    {
        Assert.Equal(new Error("Area.Code", "Повідомлення."), SomeError);
        Assert.Equal(string.Empty, Error.None.Code);
        Assert.Equal(string.Empty, Error.None.Message);
    }

    /// <summary>Reaches the protected constructor to check the success-without-error invariant.</summary>
    private sealed class ProbeResult(bool isSuccess, Error error) : Result(isSuccess, error);
}
