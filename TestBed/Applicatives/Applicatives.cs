using TestBed.HigherKinds;
using Utils;
using static TestBed.Applicatives.Suit;
using static TestBed.Applicatives.Face;

namespace TestBed.Applicatives;

public delegate (Deck, IEnumerable<Card>) Deal(Deck d);

// from https://blog.ploeh.dk/2018/10/08/full-deck/
public static class Applicatives
{
    public static List<Suit> allSuits = new() {Hearts, Clubs, Spades, Diamonds};
    public static List<Face> allFaces = new() {Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace};

    public static void Main()
    {
        var allCards = allFaces.Aggregate(Enumerable.Empty<Card>(), (acc, s) 
            => acc.Concat(allSuits.Select(f => new Card(f, s))));

        var shuffledCards = allCards.Shuffle();
        var shuffledDeck = new Deck(shuffledCards);

        var (d1, h1) = Deck.DealHand(shuffledDeck, 2);
        
        Console.WriteLine($"{h1} :: {h1.Value}");

        if (h1.Value.fullValue < 17)
        {
            Console.WriteLine("Hit!");
            var (_, cards) = Deck.Deal(d1, 1);
            var h2 = Hand.AddCards(h1, cards);
            Console.WriteLine($"{h2} :: {h2.Value}");
        }

        var xh = Hand.From(new Card(Hearts, Ace), new Card(Hearts, Two));
        Console.WriteLine($"{xh} :: {xh.Value}");
        xh = Hand.AddCards(xh, new Card(Hearts, Three));
        Console.WriteLine($"{xh} :: {xh.Value}");
    }
}

public readonly record struct Player(Hand Hand)
{
    public static Player FromHand(Hand hand) => new Player(hand);
}

public readonly record struct Hand(IEnumerable<Card> Cards)
{
    public static Hand From(params Card[] cards) => new Hand(cards);
    
    public (int fullValue, int lowValue) Value => Cards.Aggregate((0,0),(acc, c) => c.Face switch
    {
        Ace => ((int)c.Face + acc.Item1, acc.Item1 + 1),
        _ => ((int)c.Face % 11 + acc.Item1, 0)
    });

    public override string ToString() => string.Join(", ", Cards.Select(x => x.ToString()));
    
    public static Hand AddCards(Hand hand, IEnumerable<Card> cards) => new(hand.Cards.Concat(cards));
    public static Hand AddCards(Hand hand, params Card[] cards) => new(hand.Cards.Concat(cards));
}

public readonly record struct Deck(IEnumerable<Card> Cards)
{
    public static (Deck newDeck, Card[] cards) Deal(Deck deck, int count)
    {
        var x = deck.Cards.Take(count).ToArray();
        var d = deck.Cards.Skip(count);
        return (new Deck(d), x);
    }

    public static (Deck newDeck, Hand hand) DealHand(Deck deck, int count)
    {
        var (d, c) = Deal(deck, count);
        return (d, new Hand(c));
    }
}

public readonly record struct Card(Suit Suit, Face Face)
{
    public override string ToString()
    {
        var s = Suit switch
        {
            Hearts => "\u2665",
            Diamonds => "\u2666",
            Clubs => "\u2663",
            Spades => "\u2660",
            _ => throw new ArgumentOutOfRangeException()
        };

        var f = Face switch
        {
            Ace => "A",
            King => "K",
            Queen => "Q",
            Jack => "J",
            _ => ((int)Face).ToString()
        };

        return f + s;
    }
}

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum Face
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 21,
    Queen = 32,
    King = 43,
    Ace = 11
}

