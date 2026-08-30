// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Monads;

// converted from
// https://github.com/fpinscala/fpinscala/blob/second-edition/src/main/scala/fpinscala/answers/state/State.scala
public delegate (A Value, S State) State<S, A>(S s);

public static class State
{

    extension<A, S>(State<S, A> underlying)
    {
        public (A value, S State) Run(S s) => underlying(s);

        public State<S, B> SelectMany<B>(Func<A, State<S, B>> f) =>
            s =>
            {
                var (a, s1) = underlying(s);
                return f(a)(s1);
            };
        
        public State<S, C> SelectMany<B, C>(Func<A, State<S, B>> f,
            Func<A, B, C> project) =>
            underlying.SelectMany(a => f(a).Select(b => project(a, b)));

        public State<S, C> Map2<B, C>(State<S, B> sb, Func<A, B, C> f) =>
            from a in underlying
            from b in sb
            select f(a, b);
    }
    
    // public static (A Value, S State) Run<S, A>(this State<S, A> underlying, S s) => 
    //     underlying(s);

    public static State<S, A> Return<S, A>(A a) => 
        s => (a, s);

    public static State<S, S> Get<S>() => 
        s => (s, s);
    
    public static State<S, Unit> Set<S>(S s) => 
        _ => (Unit.Value, s);

    public static State<S, Unit> Modify<S>(Func<S, S> f) =>
        from s in Get<S>()
        from _ in Set(f(s))
        select Unit.Value;


    public static State<S, B> Select<S, A, B>(this State<S, A> underlying, Func<A, B> f) =>
        underlying.SelectMany(a => Return<S,B>(f(a)));

    
    //def apply[S, A](f: S => (A, S)): State[S, A] = f
    public static State<S, A> Apply<S, A>(State<S, A> f) => f;

    public static State<S, B> Apply<S, A, B>(this State<S, Func<A, B>> fs, State<S, A> parameter) =>
        fs.Map2(parameter, (f, a) => f(a));

    public static State<S, B> ApplyM<S, A, B>(this State<S, Func<A, B>> fs, State<S, A> parameter) =>
        fs.SelectMany(parameter.Select);

    public static State<S, IEnumerable<B>> Traverse<S, A, B>(this IEnumerable<A> input, Func<A, State<S, B>> f) =>
        input.Aggregate(Return<S, IEnumerable<B>>([]),
            (acc, a) => acc.Map2(f(a), (bs, b) => bs.Append(b)));

    public static State<S, IEnumerable<A>> Sequence<S, A>(IEnumerable<State<S, A>> actions) =>
        actions.Traverse(x => x);
}

public static class Candy
{
    public static State<Machine, (int Coins, int Candies)> SimulateMachine(params Input[] inputs) =>
        from _ in inputs.Traverse(i => State.Modify(Update(i)))
        from s in State.Get<Machine>()
        select (s.Coins, s.Candies);

    public static Func<Machine, Machine> Update(Input i) => s =>
        (i, s) switch
        {
            (_, (_, 0, _)) => s, // no candy left
            (Input.Coin, (false, _, _)) => s, // coin in unlocked machine
            (Input.Turn, (true, _, _)) => s, // turn locked machine
            (Input.Coin, (true, var candy, var coin)) // coin in locked machine
                => new Machine(false, candy, coin + 1),
            (Input.Turn, (false, var candy, var coin)) // turn unlocked machine
                => new Machine(true, candy - 1, coin),
            _ => throw new ArgumentOutOfRangeException()
        };
}

public record struct Machine(bool Locked, int Candies, int Coins);
public enum Input {Coin, Turn}

