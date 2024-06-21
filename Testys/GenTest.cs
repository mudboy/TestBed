using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using TestBed;
using Xunit;
using static TestBed.Gen;

namespace Testys;

public sealed class GenTest
{
    [Theory]
    [InlineData(123456)]
    [InlineData(57)]
    [InlineData(-1)]
    public void Should_Obey_The_First_Functor_Law(int a)
    {
        int Id(int x) => x;
        var m = Unit(a);
        
        // mapping the id func is the same as calling the thing directly
        m.Should().BeSameAs(m.Select(Id));
    }
    
    [Theory]
    [InlineData("tests")]
    [InlineData("blub")]
    [InlineData("foo")]
    public void Should_Obey_The_Second_Functor_Law(string a)
    {
        int F(string s) => s.Length;
        bool G(int i) => i % 2 == 0;
        var m = Unit(a);

        // the order of grouping maps together is like brackets for adding (a + b) + c == a + (b + c)
        m.Select(F).Select(G).Should().BeSameAs(m.Select(x => G(F(x))));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(53)]
    public void Should_Obey_Monad_Left_Identity_Law(int a)
    {
        Gen<string> F(int i) => Unit(i.ToString());

        // binding on a value wrapped via unit is the same as calling the thing directly
        // unit is on the left of bind
        Unit(a).SelectMany(F).Should().BeSameAs(F(a));
    }    
    
    [Theory]
    [InlineData("one")]
    [InlineData("some")]
    [InlineData("test")]
    public void Should_Obey_Monad_Right_Identity_Law(string a)
    {
        Gen<int> F(string s) => Unit(s.Length);
        var m = F(a);

        // binding unit on the result of calling f is the same as calling the thing directly
        // unit is on the right of bind - unit is basically the id func for the monad
        m.SelectMany(Unit).Should().BeSameAs(m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Should_Obey_The_Associativity_Law(double a)
    {
        Gen<bool> F(double i) => Unit(i % 2 == 0);
        Gen<string> G(bool b) => Unit(b.ToString());
        Gen<int> H(string s) => Unit(s.Length);
        var m = F(a);
 
        // the order of grouping binds together is like brackets for adding (a + b) + c == a + (b + c)
        m.SelectMany(G).SelectMany(H).Should().BeSameAs(m.SelectMany(x => G(x).SelectMany(H)));
    }
}

public static class GenExtensions 
{
    public static GenAssertions<A> Should<A>(this Gen<A> instance)
    {
        return new GenAssertions<A>(instance); 
    } 
}

public sealed class GenAssertions<A> : 
    ReferenceTypeAssertions<Gen<A>, GenAssertions<A>>
{
    public GenAssertions(Gen<A> instance)
        : base(instance)
    {
    }

    protected override string Identifier => "Gen";

    public new AndConstraint<GenAssertions<A>> BeSameAs(
        Gen<A> other, long seed = 12345, string because = "", params object[] becauseArgs)
    {
        var r = Rng.Simple(seed);
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Equals(Subject(r).Item1, other(r).Item1))
            .FailWith("Expected {context:Gen} to produce {0}{reason}, but found {1}.", 
                Subject(r).Item1, other(r).Item1);

        return new AndConstraint<GenAssertions<A>>(this);
    }
}