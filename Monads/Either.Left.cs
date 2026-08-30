using System.Diagnostics.CodeAnalysis;
using Monads.monoids;

namespace Monads;

public abstract partial class Either<A, B>
{
    internal class Left<Ax, Bx>(Ax value) : Either<Ax, Bx>
    {
        [NotNull]
        private readonly Ax _value = value;
        public override bool IsLeft => true;
        public override bool IsRight => false;
        public override C Match<C>(Func<Ax, C> left, Func<Bx, C> right) => left(_value);
        public override void Match(Action<Ax> left, Action<Bx> right) => left(_value);
        public override Either<Ax, C> Select<C>(Func<Bx, C> f) => new Left<Ax, C>(_value);
        public override Either<Ax, C> SelectMany<C>(Func<Bx, Either<Ax, C>> f) => new Left<Ax, C>(_value);
        public override bool Contains(Bx elem) => false;
        public override bool Exists(Func<Bx, bool> predicate) => false;
        public override Either<Ax, Bx> FilterOrElse(Func<Bx, bool> predicate, Func<Ax> zero) => this;
        public override Bx GetOrElse(Bx alternative) => alternative;
        public override Bx GetOrElse(Func<Bx> alternative) => alternative();
        public override Either<Ax, D> Map2<C, D>(Either<Ax, C> other, Func<Bx, C, D> func) =>
            other.Match(
                left: l =>
                {
                    return l switch
                    {
                        Monoid<Ax> x => x.Combine(_value, l),
                        _ => Either.Left<Ax, D>(_value)
                    };
                },
                right: r => Either.Left(_value));

        internal override Ax GetLeftValue => _value;
        internal override Bx GetRightValue => throw new InvalidCastException("Either in is the Left state");

        public override bool Equals(Either<Ax, Bx>? other) =>
            other switch
            {
                Left<Ax, Bx> l => _value.Equals(l._value),
                _ => false
            };


        public override bool Equals(object? obj) =>
            obj switch
            {
                Left<Ax, Bx> l => _value.Equals(l._value),
                Ax a => a.Equals(_value),
                _ => false
            };


        public override int GetHashCode() => _value.GetHashCode();
    }    
}