using FluentAssertions;
using Monads;
using Xunit;

namespace Testys;

public sealed class RandTests
{
    [Theory]
    [InlineData(123456)]
    [InlineData(57)]
    [InlineData(-1)]
    public void Should_Obey_The_First_Functor_Law(int a)
    {
        int Id(int x) => x;
        var m = Rand.Unit(a);
        var rng = Rng.Simple(1234);

        m.Run(rng).Should().Be(m.Select(Id).Run(rng));
    }

    [Theory]
    [InlineData("tests")]
    [InlineData("blub")]
    [InlineData("foo")]
    public void Should_Obey_The_Second_Functor_Law(string a)
    {
        int F(string s) => s.Length;
        bool G(int i) => i % 2 == 0;
        var m = Rand.Unit(a);
        var rng = Rng.Simple(99);

        m.Select(F).Select(G).Run(rng).Should().Be(m.Select(x => G(F(x))).Run(rng));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(53)]
    public void Should_Obey_Monad_Left_Identity_Law(int a)
    {
        Rand<string> F(int i) => Rand.Unit(i.ToString());
        var rng = Rng.Simple(5);

        Rand.Unit(a).SelectMany(F).Run(rng).Should().Be(F(a).Run(rng));
    }

    [Theory]
    [InlineData("one")]
    [InlineData("some")]
    [InlineData("test")]
    public void Should_Obey_Monad_Right_Identity_Law(string a)
    {
        Rand<int> F(string s) => Rand.Unit(s.Length);
        var m = F(a);
        var rng = Rng.Simple(7);

        m.SelectMany(Rand.Unit).Run(rng).Should().Be(m.Run(rng));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Should_Obey_The_Associativity_Law(double a)
    {
        Rand<bool> F(double i) => Rand.Unit(i % 2 == 0);
        Rand<string> G(bool b) => Rand.Unit(b.ToString());
        Rand<int> H(string s) => Rand.Unit(s.Length);
        var m = F(a);
        var rng = Rng.Simple(11);

        m.SelectMany(G).SelectMany(H).Run(rng).Should().Be(m.SelectMany(x => G(x).SelectMany(H)).Run(rng));
    }

    [Fact]
    public void Map2_Combines_Two_Rand_Values()
    {
        var m = Rand.Unit(10).Map2(Rand.Unit(2), (x, y) => x / y);

        m.Run(Rng.Simple(42)).Value.Should().Be(5);
    }

    [Fact]
    public void Eval_Returns_Just_The_Value_That_Run_Would_Have_Returned()
    {
        var rng = Rng.Simple(42);

        Rand.Int.Eval(rng).Should().Be(Rand.Int.Run(rng).Value);
    }

    [Fact]
    public void Exec_Returns_Just_The_Next_Rng_That_Run_Would_Have_Returned()
    {
        var rng = Rng.Simple(42);

        Rand.Int.Exec(rng).Should().Be(Rand.Int.Run(rng).Rng);
    }

    [Fact]
    public void Chains_Built_With_Query_Syntax_Are_Deterministic_For_The_Same_Rng()
    {
        var chain =
            from a in Rand.Int
            from b in Rand.Int
            select a + b;

        var (first, _) = chain.Run(Rng.Simple(2024));
        var (second, _) = chain.Run(Rng.Simple(2024));

        first.Should().Be(second);
    }

    [Fact]
    public void Sequence_Threads_The_Rng_Through_Each_Action_In_Order()
    {
        var actions = new[] { Rand.Int, Rand.Int, Rand.Int };
        var rng = Rng.Simple(2024);

        var (sequenced, _) = actions.Sequence().Run(rng);

        var (first, rng1) = Rand.Int(rng);
        var (second, rng2) = Rand.Int(rng1);
        var (third, _) = Rand.Int(rng2);

        sequenced.Should().Equal(first, second, third);
    }

    [Fact]
    public void Repeat_Runs_The_Same_Action_N_Times_Threading_The_Rng_Through_Each()
    {
        var rng = Rng.Simple(2024);

        var (repeated, _) = Rand.Int.Repeat(3).Run(rng);

        var (first, rng1) = Rand.Int(rng);
        var (second, rng2) = Rand.Int(rng1);
        var (third, _) = Rand.Int(rng2);

        repeated.Should().Equal(first, second, third);
    }
}
