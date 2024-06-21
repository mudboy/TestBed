using FluentAssertions;
using TestBed;
using Xunit;

namespace Testys;

public sealed class NonEmptyListTests
{
    [Fact]
    public void Must_Have_One_Element()
    {
        var result = new NonEmptyList<int>(1);

        result.Count.Should().Be(1);
    }

    [Fact]
    public void Should_Be_Indexed()
    {
        var vals = new[] { 1, 2, 3 };
        var result = new NonEmptyList<int>(1, 2, 3);

        for (int i = 0; i < result.Count; i++)
        {
            result[i].Should().Be(vals[i]);
        }
    }

    [Fact]
    public void Should_Have_Tail()
    {
        var result = new NonEmptyList<int>(1, 2, 3);

        result.Count.Should().Be(3);
    }

    [Fact]
    public void Should_Be_Selectable()
    {
        var result = new NonEmptyList<int>(1, 2, 3);

        result.Select(x => x.ToString()).Should().BeEquivalentTo("1", "2", "3");
    }

    [Fact]
    public void Should_Allow_AsNonEmpty()
    {
        var result = new[] { 1, 2, 3 }.AsNonEmptyList();
        var x = new HashSet<int>() { 1, 2, 3 }.AsNonEmptyList();

        result.Count.Should().Be(3);
        x.Count.Should().Be(3);
    }
}