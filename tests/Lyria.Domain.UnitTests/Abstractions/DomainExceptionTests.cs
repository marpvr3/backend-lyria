using Lyria.Domain.Exceptions;
using Xunit;

namespace Lyria.Domain.UnitTests.Abstractions;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var exception = new TestDomainException("Error de dominio");

        Assert.Equal("Error de dominio", exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new TestDomainException("Error de dominio", inner);

        Assert.Equal("Error de dominio", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void DomainException_IsAbstract()
    {
        Assert.True(typeof(DomainException).IsAbstract);
    }

    [Fact]
    public void DomainException_InheritsFromException()
    {
        var exception = new TestDomainException("test");

        Assert.IsAssignableFrom<Exception>(exception);
    }

    private sealed class TestDomainException : DomainException
    {
        public TestDomainException(string message)
            : base(message)
        {
        }

        public TestDomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
