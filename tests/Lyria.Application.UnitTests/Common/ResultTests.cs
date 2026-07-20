using Lyria.Application.Common.Errors;
using Lyria.Application.Common.Results;
using Xunit;

namespace Lyria.Application.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_IsSuccessTrue()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Success_HasNoError()
    {
        Result result = Result.Success();

        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_IsFailureTrue()
    {
        Result result = Result.Failure(Error.NotFound("test", "desc"));

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Failure_HasError()
    {
        var error = Error.NotFound("test", "desc");
        Result result = Result.Failure(error);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void SuccessWithError_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new TestResult(true, Error.Failure("code", "desc")));
    }

    [Fact]
    public void FailureWithNoError_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new TestResult(false, Error.None));
    }

    [Fact]
    public void GenericSuccess_HasValue()
    {
        Result<int> result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ValueThrows()
    {
        Result<int> result = Result.Failure<int>(Error.NotFound("test", "desc"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericSuccess_IsFailureFalse()
    {
        Result<string> result = Result.Success("hello");

        Assert.False(result.IsFailure);
    }

    [Fact]
    public void GenericFailure_IsSuccessFalse()
    {
        Result<string> result = Result.Failure<string>(Error.Conflict("code", "desc"));

        Assert.False(result.IsSuccess);
    }

    private sealed class TestResult(bool isSuccess, Error error)
        : Result(isSuccess, error);
}
