namespace Blackjack;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static System.String;

public enum HandKind
{
    Hard,
    Soft,
    Pair
}

public enum HandMove
{
    Stand,
    Hit,
    Double,
    Split
}

public enum HandPlay
{
    None,
    Loss,
    Win,
    Tie,
    Blackjack,
    Bust,
    Wrong,
}

public abstract class HandBase
{
    public const int BlackjackScore = 21;

    protected readonly List<Card> cards;

    protected HandBase(int bank)
    {
        this.cards = new List<Card>(capacity: 4);
        this.Bank = bank;
    }

    public int Bank { get; private set; }

    public Card FirstCard => this.cards.Count > 0 ? this.cards[0] : Card.Joker;
    public bool IsNatural => this.cards.Count == 2 && Score() == BlackjackScore;
    protected bool HasAce => this.cards.Any(card => card.Rank == CardRank.Ace);

    public override string ToString() =>
        Join("-", this.cards.OrderByDescending(card => card.Order));

    public int Score()
    {
        // A+A+X will always be valued as 12+X, unless this would bust, in which case it must be valued as 12
        // (since the only such X is a ten-value card, and therefore the only non-bust score is for both aces to be one-value).

        var score = this.cards.Sum(card => card.Score);
        if (score <= 11 && this.HasAce)
            return score + 10;

        return score;
    }

    public void Hit(Card card)
    {
        this.cards.Add(card);
    }

    protected int Pop(int chips) 
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chips);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chips, this.Bank);

        this.Bank -= chips;
        return chips;
    }

    protected void Push(int chips)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chips);

        this.Bank += chips;
    }
}

public class Hand : HandBase, IReadOnlyCollection<Card>
{
    public const int DefaultBank = 1000;
    private const int DefaultBet = 250;

    private sealed class HandLayoutEqualityComparer : IEqualityComparer<Hand>
    {
        public bool Equals(Hand x, Hand y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            return
                x.Kind == y.Kind &&
                x.HasAce == y.HasAce &&
                x.Score() == y.Score();
        }

        public int GetHashCode(Hand obj)
        {
            return HashCode.Combine(obj.Kind, obj.HasAce, obj.Score());
        }
    }

    public static readonly IEqualityComparer<Hand> LayoutEqualityComparer = new HandLayoutEqualityComparer();

    public static readonly Hand[] AllDeals;
    public static readonly Hand[] Deals;

    private readonly Player player;

    static Hand()
    {
        var hands = new List<Hand>(capacity: Card.StandardDeck.Length * (Card.StandardDeck.Length - 1) / 2);

        for (var i = 0; i != Card.StandardDeck.Length; ++i)
        {
            for (var j = i + 1; j != Card.StandardDeck.Length; ++j)
            {
                hands.Add(new Hand { Card.StandardDeck[i], Card.StandardDeck[j] });
            }
        }

        AllDeals = hands
            .ToArray();

        Deals = AllDeals
            .Where(h => h.Score() < BlackjackScore)
            .Distinct(LayoutEqualityComparer)
            .OrderBy(hand => hand.Kind)
            .ThenByDescending(hand => hand.Sum(c => c.Order))
            .ToArray();
    }

    public Hand(Player player) : this(player, DefaultBank) { }

    public Hand(Player player, int bank) : base(bank)
    {
        this.player = player;
    }

    private Hand() : this(Player.None)
    {
    }

    protected Hand(Player player, Card card, int bank) : this(player, bank)
    {
        this.IsSplit = true;
        Hit(card);
    }

    public int Count => this.cards.Count;

    public Card SecondCard => this.cards.Count > 1 ? this.cards[1] : Card.Joker;

    public bool IsSplit { get; }

    public HandKind Kind
    {
        get
        {
            if (this.cards.Count == 2 && this.FirstCard.Rank == this.SecondCard.Rank)
                return HandKind.Pair;
            if (this.HasAce)
                return HandKind.Soft;

            return HandKind.Hard;
        }
    }

    public HandPlay Play { get; private set; }

    public override string ToString() => this.Kind switch
    {
        HandKind.Hard => Score().ToString(CultureInfo.InvariantCulture),
        _ => base.ToString()
    };

    public virtual HandMove Move(Card upcard, int dealerScore)
    {
        return this.player.Move(this, upcard, dealerScore);
    }

    public Hand Split()
    {
        if (this.Kind != HandKind.Pair)
            throw new InvalidOperationException();

        var secondCard = this.cards[1];
        this.cards.RemoveAt(1);
        return Split(secondCard);
    }

    protected virtual Hand Split(Card card)
    {
        return new Hand(this.player, card, Pop(DefaultBet));
    }

    public void Set(HandPlay play)
    {
        this.Play = play;
        this.player.EndPlay(this);
    }

    public Bet MakeBet(bool doubleDown = false)
    {
        //todo: develop a betting strategy
        return new(this, Pop(DefaultBet * (doubleDown ? 2 : 1)));
    }

    public void ResolveBet(Bet bet)
    {
        Push(bet.Chips);
    }

    public IEnumerator<Card> GetEnumerator() => this.cards.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public void Add(Card card) => Hit(card);
}