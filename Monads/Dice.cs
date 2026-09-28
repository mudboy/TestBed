namespace Monads;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

/// <summary>
/// e.g. Dice.Of(2, 6) is 2d6 - two six-sided dice.
/// </summary>
public sealed record Dice(int NumberOfDice, int Faces)
{
    public static Dice Of(int numberOfDice, int faces) => new(numberOfDice, faces);

    public Rand<IReadOnlyList<int>> Roll() => RollOne.Repeat(NumberOfDice);

    public Rand<int> Total => Roll().Select(rolls => rolls.Sum());

    public Rand<int> Highest => Roll().Select(rolls => rolls.Max());

    public Rand<int> Lowest => Roll().Select(rolls => rolls.Min());

    public Rand<int> CountOf(int value) => Roll().Select(rolls => rolls.Count(r => r == value));

    /// <summary>Rolls this dice twice and keeps the higher total.</summary>
    public Rand<int> WithAdvantage => Total.WithAdvantage();

    /// <summary>Rolls this dice twice and keeps the lower total.</summary>
    public Rand<int> WithDisadvantage => Total.WithDisadvantage();

    private Rand<int> RollOne =>
        rng =>
        {
            var (n, next) = rng.NonNegativeInt();
            return (1 + n % Faces, next);
        };
}
