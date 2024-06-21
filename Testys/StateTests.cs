using TestBed;
using Xunit;

namespace Testys;

public sealed class StateTests
{
    public static readonly Guid Monday =
        new Guid("5AB18569-29C7-4041-9719-5255266B808D");

    public static readonly Guid OtherDays =
        new Guid("00553FC8-82C9-40B2-9FAA-F9ADFFD4EE66");
    
    [Theory]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void FirstFunctorLaw(DayOfWeek day)
    {
        Func<Guid, Guid> id = g => g;
        State<DayOfWeek, Guid> dayIdentifier = 
            s => s == DayOfWeek.Monday ? (Monday, DayOfWeek.Tuesday) : (OtherDays, DayOfWeek.Monday);
        
        Assert.Equal(dayIdentifier.Run(day), dayIdentifier.Select(id).Run(day));
    }
    
    [Theory]
    [InlineData( "foo", 0)]
    [InlineData( "bar", 1)]
    [InlineData( "baz", 2)]
    [InlineData("quux", 3)]
    public void SecondFunctorLaw(string txt, int i)
    {
        Func<string, int> g = x => x.Length;
        Func<int, bool> f = x => x % 2 == 0;

        var s = VowelExpander(txt);
        Assert.Equal(
            s.Select(g).Select(f).Run(i),
            s.Select(x => f(g(x))).Run(i));
    }
    
    [Theory]
    [InlineData("foo", 0, "f")]
    [InlineData("foo", 1, "foo")]
    [InlineData("foo", 2, "foooo")]
    [InlineData("bar", 0, "br")]
    [InlineData("bar", 1, "bar")]
    [InlineData("bar", 2, "baar")]
    public void BasicUsageExample(string txt, int count, string expected)
    {
        var s = VowelExpander(txt);
        var t = s.Run(count);
        Assert.Equal((expected, count + 1), t);
    }

    private State<int, string> VowelExpander(string text) =>
        state =>
        {
            const string vowels = "aeiouy";
            var expanded = text.SelectMany(c =>
                vowels.Contains(c) ? Enumerable.Repeat(c, state) : new[] { c });

            var newState = state + 1;

            return (new string(expanded.ToArray()), newState);
        };
}