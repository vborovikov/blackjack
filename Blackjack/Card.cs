namespace Blackjack;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies the suit of a playing card.
/// </summary>
public enum CardSuit
{
    Unknown,
    Hearts,
    Diamonds,
    Clubs,
    Spades,
    Joker
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
public readonly struct Card : IEquatable<Card>, ISpanFormattable, ISpanParsable<Card>
{
    public const int MinScore = 2;
    public const int MaxScore = 11;
    public static readonly Card Back = new();
    public static readonly Card BlackJocker = new(CardSuit.Joker, CardRank.None);
    public static readonly Card RedJocker = new(CardSuit.Joker, CardRank.Ace);

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

    public char ScoreSymbol => this.Rank switch
    {
        CardRank.Ace => 'A',
        CardRank.Jack or CardRank.Queen or CardRank.King or CardRank.Ten => 'T',
        _ => (char)(0x30 + (((int)this.Rank) % 10))
    };

    public static bool operator ==(Card left, Card right) => left.Equals(right);

    public static bool operator !=(Card left, Card right) => !(left == right);

    public override bool Equals(object? obj) => obj is Card other && Equals(other);

    public bool Equals(Card other) => this.Suit == other.Suit && this.Rank == other.Rank;

    public override int GetHashCode() => HashCode.Combine(this.Rank, this.Suit);

    public override string ToString() => ToString(null, null);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        Span<char> span = stackalloc char[2];
        if (TryFormat(span, out var charsWritten, format, formatProvider))
        {
            return span[..charsWritten].ToString();
        }

        return string.Empty;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, 1);
        charsWritten = 0;

        if (format.IsEmpty || format.Equals("RS", StringComparison.OrdinalIgnoreCase))
        {
            // ## Rank and Suit
            ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, 2);

            destination[charsWritten++] = RankToSymbol(this.Rank);
            destination[charsWritten++] = SuitToSymbol(this.Suit);
            return true;
        }
        else if (format.Equals("R", StringComparison.OrdinalIgnoreCase))
        {
            // # Rank
            destination[charsWritten++] = RankToSymbol(this.Rank);
            return true;
        }
        else if (format.Equals("S", StringComparison.OrdinalIgnoreCase))
        {
            // # Suit
            destination[charsWritten++] = SuitToSymbol(this.Suit);
            return true;
        }

        return false;
    }

    public static Card Parse(string s) =>
        Parse(s.AsSpan(), null);

    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out Card result) =>
        TryParse(s.AsSpan(), null, out result);

    public static Card Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Card result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static Card Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var card))
            return card;

        throw new FormatException();
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Card result)
    {
        if (s.IsEmpty)
        {
            result = default;
            return true;
        }

        if (s.Length != 2 || !char.IsAsciiLetterOrDigit(s[0]) || !char.IsAsciiLetter(s[1]))
        {
            result = default;
            return false;
        }

        result = new(SymbolToSuit(s[1]), SymbolToRank(s[0]));
        return true;
    }

    private static CardRank SymbolToRank(char symbol) => symbol switch
    {
        'A' or 'a' => CardRank.Ace,
        'J' or 'j' => CardRank.Jack,
        'Q' or 'q' => CardRank.Queen,
        'K' or 'k' => CardRank.King,
        'T' or 't' => CardRank.Ten,
        _ => (CardRank)(symbol - 0x30)
    };

    private static CardSuit SymbolToSuit(char symbol) => symbol switch
    {
        'H' or 'h' => CardSuit.Hearts,
        'D' or 'd' => CardSuit.Diamonds,
        'C' or 'c' => CardSuit.Clubs,
        'S' or 's' => CardSuit.Spades,
        'X' or 'x' => CardSuit.Joker,
        _ => CardSuit.Unknown
    };

    private static char RankToSymbol(CardRank rank) => rank switch
    {
        CardRank.Ace => 'A',
        CardRank.Jack => 'J',
        CardRank.Queen => 'Q',
        CardRank.King => 'K',
        CardRank.Ten => 'T',
        _ => (char)(0x30 + (((int)rank) % 10))
    };

    private static char SuitToSymbol(CardSuit suit) => suit switch
    {
        CardSuit.Hearts => 'H',
        CardSuit.Diamonds => 'D',
        CardSuit.Clubs => 'C',
        CardSuit.Spades => 'S',
        CardSuit.Joker => 'X',
        _ => '0'
    };
}