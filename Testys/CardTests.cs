using FluentAssertions;
using TestBed.Applicatives;
using Xunit;

namespace Testys;

public sealed class CardTests
{
    [Fact]
    public void nameValues()
    {
        var ten = Hand.From(new Card(Suit.Clubs, Face.Ten));
        var king = Hand.From(new Card(Suit.Clubs, Face.King));
        var queen = Hand.From(new Card(Suit.Clubs, Face.Queen));
        var jack = Hand.From(new Card(Suit.Clubs, Face.Jack));
        var hh = Hand.From(new Card(Suit.Clubs, Face.Six), new Card(Suit.Clubs, Face.Five),
            new Card(Suit.Clubs, Face.Seven));

        ten.Value.fullValue.Should().Be(10);
        king.Value.fullValue.Should().Be(10);
        queen.Value.fullValue.Should().Be(10);
        jack.Value.fullValue.Should().Be(10);
        hh.Value.fullValue.Should().Be(18);



    }
}