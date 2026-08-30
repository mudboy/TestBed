using FluentAssertions;
using Monads;
using Xunit;

namespace Testys;

public class EitherTests
{
    [Fact]
    public void Should_Equal()
    {
        var right = Either.Right<int, string>("test");
        var left = Either.Left<int, string>(99);

        right.Should().NotBe(left);
        right.Should().Be(Either.Right<int, string>("test"));
        right.Should().NotBe(Either.Right<string, int>(99));
        right.Should().Be("test");
        right.Should().NotBe("xxx");
        right.Should().NotBe(99);
        left.Should().Be(Either.Left<int, string>(99));
        left.Should().NotBe(Either.Left<string, int>("test"));
        left.Should().Be(99);
        left.Should().NotBe(1);
        left.Should().NotBe("xxx");
    }

    [Fact]
    public void Should_Allow_Casting()
    {
        var right = Either.Right<int, string>("test");

        var validCast = () => (string)right;
        var badCast = () => (double)right;

        validCast.Should().NotThrow();
        badCast.Should().Throw<InvalidCastException>().WithMessage("bob");

    }
}