using System.Diagnostics.CodeAnalysis;

namespace Monads;

public abstract partial class Either<A, B>
{
    internal class Right<Ax, Bx>(Bx value) : Either<Ax, Bx>
    {
        [NotNull]
        private readonly Bx _value = value;
        public override bool IsLeft => false;
        public override bool IsRight => true;
        public override C Match<C>(Func<Ax, C> left, Func<Bx, C> right) => right(_value);
        public override void Match(Action<Ax> left, Action<Bx> right) => right(_value);
        public override Either<Ax, C> Select<C>(Func<Bx, C> f) => new Right<Ax, C>(f(_value));
        public override Either<Ax, C> SelectMany<C>(Func<Bx, Either<Ax, C>> f) => f(_value);
        public override bool Contains(Bx elem) => elem?.Equals(_value) ?? false;
        public override bool Exists(Func<Bx, bool> predicate) => predicate(_value);
        public override Either<Ax, Bx> FilterOrElse(Func<Bx, bool> predicate, Func<Ax> zero) =>
            !predicate(_value) ? new Left<Ax, Bx>(zero()) : this;
        public override Bx GetOrElse(Bx alternative) => _value;
        public override Bx GetOrElse(Func<Bx> alternative) => _value;
        public override Either<Ax, D> Map2<C, D>(Either<Ax, C> other, Func<Bx, C, D> func) =>
            other.Match(
                left: l => Either.Left(l),
                right: r => Either.Right<Ax, D>(func(_value, r)));

        internal override Ax GetLeftValue => throw new InvalidCastException($"Either is type Right<{typeof(B).Name}>");
        internal override Bx GetRightValue => _value;

        public override bool Equals(Either<Ax, Bx>? other) =>
            other switch
            {
                Right<Ax, Bx> l => _value.Equals(l._value),
                _ => false
            };


        public override bool Equals(object? obj) =>
            obj switch
            {
                Right<Ax, Bx> l => _value.Equals(l._value),
                Bx a => a.Equals(_value),
                _ => false
            };
        
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}