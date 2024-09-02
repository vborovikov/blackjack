namespace Blackjack;

using System;

/// <summary>
/// Specifies the suit of a playing card.
/// </summary>
public enum CardSuit
{
    Joker,
    Hearts,
    Diamonds,
    Clubs,
    Spades,
}

/// <summary>
/// Specifies the rank of a playing card.
/// </summary>
public enum CardRank
{
    None,
    Ace,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
}

/// <summary>
/// Represents a playing card.
/// </summary>
public readonly struct Card : IEquatable<Card>
{
    public const int MinScore = 2;
    public const int MaxScore = 11;
    public static readonly Card Joker = new();

    public static readonly Card[] StandardDeck;

    static Card()
    {
        StandardDeck = new Card[52];
        for (var suit = CardSuit.Hearts; suit <= CardSuit.Spades; ++suit)
            for (var rank = CardRank.Ace; rank <= CardRank.King; ++rank)
                StandardDeck[(((int)suit - 1) * 13) + (int)rank - 1] = new Card(suit, rank);
    }

    public Card(CardSuit suit, CardRank rank)
    {
        this.Suit = suit;
        this.Rank = rank;
    }

    public CardSuit Suit { get; }

    public CardRank Rank { get; }

    public bool IsFaceCard => this.Rank >= CardRank.Jack && this.Rank <= CardRank.King;

    public int Score => this.Rank switch
    {
        CardRank.Jack or CardRank.Queen or CardRank.King => 10,
        _ => (int)this.Rank
    };

    public int Order => this.Rank switch
    {
        CardRank.Ace => MaxScore,
        CardRank.Jack or CardRank.Queen or CardRank.King => 10,
        _ => (int)this.Rank
    };

    public char Symbol => RankToSymbol(this.Rank);

    public static bool operator ==(Card left, Card right) => left.Equals(right);

    public static bool operator !=(Card left, Card right) => !(left == right);

    public static char RankToSymbol(CardRank rank) => rank switch
    {
        CardRank.Ace => 'A',
        CardRank.Jack or CardRank.Queen or CardRank.King or CardRank.Ten => 'T',
        _ => (char)(0x30 + (((int)rank) % 10))
    };

    public override bool Equals(object obj) => obj is Card other && Equals(other);

    public bool Equals(Card other) => this.Suit == other.Suit && this.Rank == other.Rank;

    public override int GetHashCode() => HashCode.Combine(this.Suit, this.Rank);

    public override string ToString() => RankToSymbol(this.Rank).ToString();
}