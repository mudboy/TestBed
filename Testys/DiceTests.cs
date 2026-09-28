using FluentAssertions;
using Monads;
using Xunit;

namespace Testys;

public sealed class DiceTests
{
    [Fact]
    public void Roll_Returns_One_Value_Per_Die_Within_The_Face_Range()
    {
        var dice = Dice.Of(numberOfDice: 5, faces: 6);

        var (rolls, _) = dice.Roll().Run(Rng.Simple(1));

        rolls.Should().HaveCount(5);
        rolls.Should().OnlyContain(r => r >= 1 && r <= 6);
    }

    [Fact]
    public void Total_Is_The_Sum_Of_The_Individual_Rolls()
    {
        var dice = Dice.Of(2, 6);
        var rng = Rng.Simple(2024);

        var (rolls, _) = dice.Roll().Run(rng);
        var (total, _) = dice.Total.Run(rng);

        total.Should().Be(rolls.Sum());
    }

    [Fact]
    public void Highest_Is_The_Biggest_Individual_Roll()
    {
        var dice = Dice.Of(4, 6);
        var rng = Rng.Simple(2024);

        var (rolls, _) = dice.Roll().Run(rng);
        var (highest, _) = dice.Highest.Run(rng);

        highest.Should().Be(rolls.Max());
    }

    [Fact]
    public void Lowest_Is_The_Smallest_Individual_Roll()
    {
        var dice = Dice.Of(4, 6);
        var rng = Rng.Simple(2024);

        var (rolls, _) = dice.Roll().Run(rng);
        var (lowest, _) = dice.Lowest.Run(rng);

        lowest.Should().Be(rolls.Min());
    }

    [Fact]
    public void CountOf_Counts_How_Many_Dice_Landed_On_A_Given_Value()
    {
        var dice = Dice.Of(10, 6);
        var rng = Rng.Simple(2024);

        var (rolls, _) = dice.Roll().Run(rng);
        var (sixes, _) = dice.CountOf(6).Run(rng);

        sixes.Should().Be(rolls.Count(r => r == 6));
    }

    [Fact]
    public void The_Same_Seed_Always_Produces_The_Same_Roll()
    {
        var dice = Dice.Of(3, 20);

        var (first, _) = dice.Roll().Run(Rng.Simple(42));
        var (second, _) = dice.Roll().Run(Rng.Simple(42));

        first.Should().Equal(second);
    }

    [Fact]
    public void WithAdvantage_Rolls_Twice_And_Keeps_The_Higher_Total()
    {
        var dice = Dice.Of(1, 20);
        var rng = Rng.Simple(2024);

        var (advantage, _) = dice.WithAdvantage.Run(rng);

        var (firstRoll, rngAfterFirst) = dice.Total.Run(rng);
        var (secondRoll, _) = dice.Total.Run(rngAfterFirst);

        advantage.Should().Be(Math.Max(firstRoll, secondRoll));
    }

    [Fact]
    public void WithDisadvantage_Rolls_Twice_And_Keeps_The_Lower_Total()
    {
        var dice = Dice.Of(1, 20);
        var rng = Rng.Simple(2024);

        var (disadvantage, _) = dice.WithDisadvantage.Run(rng);

        var (firstRoll, rngAfterFirst) = dice.Total.Run(rng);
        var (secondRoll, _) = dice.Total.Run(rngAfterFirst);

        disadvantage.Should().Be(Math.Min(firstRoll, secondRoll));
    }

    [Fact]
    public void Advantage_Tends_To_Roll_Higher_Than_Disadvantage_Over_Many_Rolls()
    {
        var dice = Dice.Of(1, 20);

        var averageAdvantage = Enumerable.Range(0, 500)
            .Select(seed => dice.WithAdvantage.Eval(Rng.Simple(seed)))
            .Average();

        var averageDisadvantage = Enumerable.Range(0, 500)
            .Select(seed => dice.WithDisadvantage.Eval(Rng.Simple(seed)))
            .Average();

        averageAdvantage.Should().BeGreaterThan(averageDisadvantage);
    }

    [Fact]
    public void Dice_Can_Be_Composed_Into_A_Larger_Chain()
    {
        // attack roll: a d20 plus a flat +3 modifier
        var attackRoll =
            from d20 in Dice.Of(1, 20).Total
            from modifier in Rand.Unit(3)
            select d20 + modifier;

        var (result, _) = attackRoll.Run(Rng.Simple(7));
        var (baseRoll, _) = Dice.Of(1, 20).Total.Run(Rng.Simple(7));

        result.Should().Be(baseRoll + 3);
    }
}
