using System.ComponentModel.Design;

namespace Monads;


public delegate Either<Error, A>IO<in Env, A>(Env e);

public static class IO
{
    public static IO<Env, A> Pure<Env, A>(A value) => env => value;

    public static Either<Error, A> Run<Env, A>(this IO<Env, A> ma, Env env)
    {
        try
        {
            return ma(env);
        }
        catch (Exception e)
        {
            return new Left<Error, A>(new Error(e.Message));
        }
    }

    public static IO<Env, B> Select<Env, A, B>(this IO<Env, A> ma, Func<A, B> f) =>
        env => ma(env).Match(
            left: Either.Left<Error,B>,
            right: a => f(a));

    public static IO<Env, B> SelectMany<Env, A, B>(this IO<Env, A> ma, Func<A, IO<Env, B>> bind) =>
        env => ma(env).Match(
            left: Either.Left<Error, B>,
            right: a => bind(a)(env));

    public static IO<Env, C> SelectMany<Env, A, B, C>(this IO<Env, A> ma, Func<A, IO<Env, B>> bind,
        Func<A, B, C> project) =>
        ma.SelectMany(a => bind(a).Select(b => project(a, b)));
}

public interface ConsoleIO
{
    string ReadLine();
    Unit WriteLine(string value);

    public static IO<Env, string> ReadLine<Env>()
        where Env : ConsoleIO =>
        env => env.ReadLine();

    public static IO<Env, Unit> WriteLine<Env>(string value)
        where Env : ConsoleIO =>
        env => env.WriteLine(value);
}

public interface FileIO
{
    string ReadAllText(string path);

    public static IO<Env, string> ReadAllText<Env>(string path)
        where Env : FileIO =>
        env => env.ReadAllText(path);
}

public interface IntIO
{
    Option<int> TryParse(string input);

    public static IO<Env, Option<int>> TryParse<Env>(string input)
        where Env : IntIO =>
        env => env.TryParse(input);
}

public static class Example
{
    public static void Main()
    {
        var comp = from text in FileIO.ReadAllText<TestEnv>("test")
            from val in IntIO.TryParse<TestEnv>(text)
            select val;

        var result = comp.Run(new TestEnv());
    }
}

public class LiveEnv : FileIO, ConsoleIO
{
    public string ReadAllText(string path) => File.ReadAllText(path);
    public string ReadLine() => Console.ReadLine();
    public Unit WriteLine(string value) { Console.WriteLine(value); return Unit.Value; }
}

public partial class TestEnv : FileIO
{
    public string ReadAllText(string path) => "this is a test";
}

public partial class TestEnv : IntIO
{
    public Option<int> TryParse(string input) => 
        int.TryParse(input, out var result) ? 
            Option.Some(result) : Option.None;
}

public static class Game
{
    public static IO<Env, Unit> Turn<Env>(string word, string display, int n)
        where Env : ConsoleIO
    {
            if (n == 0)
                return ConsoleIO.WriteLine<Env>("You Lose!");
            if (word == display)
                return ConsoleIO.WriteLine<Env>("You Win!!");

            return MakeGuess<Env>(word, display, n);
    }

    public static IO<Env, Unit> MakeGuess<Env>(string word, string display, int n)
        where Env : ConsoleIO
    {
        return from _ in ConsoleIO.WriteLine<Env>(display + " " + string.Concat(Enumerable.Repeat('*', n)))
            from q in ConsoleIO.ReadLine<Env>()
            let x = Check(word, display, q?.First() ?? '0')
            let n1 = x.Item1 ? n : n - 1
            from _1 in Turn<Env>(word, x.Item2, n1)
            select Unit.Value;
    }

    public static (bool, string) Check(string word, string display, char c)
    {
        var contains = word.Contains(c);
        return (contains, string.Concat(word.Zip(display).Select(x => c == x.First ? c : x.Second)));
    }

    public static IO<Env, Unit> Starman<Env>(string word, int n)
        where Env : ConsoleIO
    {
        return Turn<Env>(word, string.Concat(Enumerable.Repeat('_', word.Length)), n);
    }
}