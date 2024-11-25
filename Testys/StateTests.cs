using FluentAssertions;
using Monads;
using Xunit;

namespace Testys;

public sealed class StateTests
{
    private static readonly Guid Monday =
        new Guid("5AB18569-29C7-4041-9719-5255266B808D");

    private static readonly Guid OtherDays =
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
        State<DayOfWeek, Guid> dayIdentifier = 
            s => s == DayOfWeek.Monday ? (Monday, DayOfWeek.Tuesday) : (OtherDays, DayOfWeek.Monday);
        
        Assert.Equal(dayIdentifier.Run(day), dayIdentifier.Select(Id).Run(day));
        return;
        Guid Id(Guid g) => g;
    }
    
    [Theory]
    [InlineData( "foo", 0)]
    [InlineData( "bar", 1)]
    [InlineData( "baz", 2)]
    [InlineData("quux", 3)]
    public void SecondFunctorLaw(string txt, int i)
    {
        bool Func(int x) => x % 2 == 0;

        var s = VowelExpander(txt);
        Assert.Equal(
            s.Select((Func<string, int>)Func1).Select(Func).Run(i),
            s.Select(x => Func(Func1(x))).Run(i));
        int Func1(string x) => x.Length;
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
    
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(53)]
    public void Should_Obey_Monad_Left_Identity_Law(int a)
    {
        State<int, string> F(int i) => State.Return<int, string>($"{i}");

        // binding on a value wrapped via unit is the same as calling the thing directly
        // unit is on the left of bind
        State.Return<int, int>(a).SelectMany(F)(1).Should().Be(F(a)(1));
    }
    
    [Theory]
    [InlineData("one")]
    [InlineData("some")]
    [InlineData("test")]
    public void Should_Obey_Monad_Right_Identity_Law(string a)
    {
        State<int, int> F(string s) => State.Return<int, int>(s.Length);
        var m = F(a);

        // binding unit on the result of calling f is the same as calling the thing directly
        // unit is on the right of bind - unit is basically the id func for the monad
        m.SelectMany(State.Return<int,int>)(1).Should().Be(m(1));
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Should_Obey_The_Associativity_Law(double a)
    {
        State<string, bool> F(double i) => State.Return<string, bool>(i % 2 == 0);
        State<string, string> G(bool b) => State.Return<string,string>(b.ToString());
        State<string, int> H(string s) => State.Return<string,int>(s.Length);
        var m = F(a);
 
        // the order of grouping binds together is like brackets for adding (a + b) + c == a + (b + c)
        m.SelectMany(G).SelectMany(H)("").Should().Be(m.SelectMany(x => G(x).SelectMany(H))(""));
    }

    [Fact]
    public void BiMap()
    {
        var a = State.Return<string, int>(10);
        var b = State.Return<string, int>(2);

        var s = a.BiMap(b, (x, y) => x / y);
        var ss = s.BiMap(b, (x, y) => x * y)("");
        ss.Value.Should().Be(10);
    }
    
    [Fact]
    public void ModifyExample()
    {
        var x = State.Modify((int i) => i + 1);
        var actual = x.Run(1);
        Assert.Equal(2, actual.State);
    }

    [Fact]
    public void ModifyExample2()
    {
        var initialState = new Machine(true, 10, 0);
        Input[] input = [Input.Coin, Input.Turn, Input.Turn, Input.Turn];
        var stateMachine = Candy.SimulateMachine(input);

        var result = stateMachine.Run(initialState);
        
        result.Value.Should().Be((9, 1));
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

    [Fact]
    public void DoEither()
    {
        var fail = Result.Failure<int>(new Error("Bang!"));

        var xx = fail.Select(x => x.ToString());

        xx.Should().BeOfType<Try<string>>();

        var result = Result.Success(1);
        var result2 = Result.Success(2);
        var f = (int x) => result2;

        var x = result >> Add5 >> Add5;

        x.Should().Be(Result.Success(11));

    }

    private static Result<int> Add5(int x) => x + 5;
}