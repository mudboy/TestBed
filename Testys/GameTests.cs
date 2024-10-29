using FluentAssertions;
using Monads;
using Xunit;

namespace Testys;

public class GameTests
{
    [Fact]
    public void CheckWorks()
    {
        var result = Game.Check("test", "____", 's');

        result.Item1.Should().BeTrue();
        result.Item2.Should().Be("__s_");
    }

    [Fact]
    public void MakeGuessWorks()
    {
        var env = new TestConsole();
        env.SetInput('t', 'x', 's', 'x', 'x', 'x');
        var comp = Game.Starman<TestConsole>("test", 3);

        var result = comp.Run(env);
        
        result.Match(e => e.Message, _ => "pass").Should().Be("pass");

        env.Log.Last().Should().Be("You Win!!");
    }
}

class TestConsole : ConsoleIO 
{
    public void SetInput(params char[] input)
    {
        _input = input;
        _nextInput = 0;
    }
    public string ReadLine()
    {
        var next = (_nextInput + 1) % _input.Length;
        var val = _input[_nextInput].ToString();
        _nextInput = next;
        return val;
    }

    public string lastLine;
    private char[] _input;
    private int _nextInput;

    public List<string> Log = new List<string>();
    public Unit WriteLine(string value)
    {
        Log.Add(value);
        return Unit.Value;
    }
}