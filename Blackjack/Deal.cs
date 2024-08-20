namespace Blackjack;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public readonly struct Bet
{
    public Hand Hand { get; }

    public int Chips { get; }
}

public class Dealer : HandBase
{
    public static readonly Card[] Upcards;

    private readonly Shoe shoe;

    static Dealer()
    {
        // Upcards are for information only, not real cards
        Upcards =
        [
            new Card(CardSuit.Joker, CardRank.Two),
            new Card(CardSuit.Joker, CardRank.Three),
            new Card(CardSuit.Joker, CardRank.Four),
            new Card(CardSuit.Joker, CardRank.Five),
            new Card(CardSuit.Joker, CardRank.Six),
            new Card(CardSuit.Joker, CardRank.Seven),
            new Card(CardSuit.Joker, CardRank.Eight),
            new Card(CardSuit.Joker, CardRank.Nine),
            new Card(CardSuit.Joker, CardRank.Ten),
            new Card(CardSuit.Joker, CardRank.Ace),
        ];
    }

    public Dealer() : base(bank: 50000)
    {
        this.shoe = Shoe.Shuffle();
    }

    public override string ToString() => this.FirstCard.ToString();

    public void Play(IList<Hand> hands)
    {
        PlaceBets(hands);
        Deal(hands);
        Deal(hands);

        // check naturals, including the dealer
        var isNatural = this.FirstCard.Score is 1 or 10 && this.IsNatural;
        foreach (var hand in hands)
        {
            if (hand.IsNatural)
            {
                //todo: for natural pay 1.5 of their bets, for tie return the bets
                hand.Set(isNatural ? HandPlay.Tie : HandPlay.Blackjack);
            }
            else if (isNatural)
            {
                //todo: collect the bets
                hand.Set(HandPlay.Loss);
            }
        }

        for (var i = 0; i != hands.Count; ++i)
        {
            var hand = hands[i];
            if (hand.Play != HandPlay.None)
                continue;

            var otherHands = Play(hand);
            if (hand.Play != HandPlay.Wrong)
            {
                foreach (var otherHand in otherHands)
                {
                    hands.Insert(++i, otherHand);
                }
            }

            var handScore = hand.Score();
            if (handScore == 21)
            {
                //todo: pay bets
                hand.Set(HandPlay.Blackjack);
            }
            else if (handScore > 21)
            {
                //todo: collect bets
                hand.Set(HandPlay.Bust);
            }
        }

        // dealer's turn
        while (Score() < 17)
        {
            Hit(this.shoe.Draw());
        }

        var dealerScore = this.Score();
        //todo: resolve bets
        foreach (var hand in hands)
        {
            if (hand.Play != HandPlay.None)
                continue;

            if (dealerScore > 21)
            {
                // pay bets
            }
            else if (dealerScore == 21)
            {
                // collect bets
                hand.Set(HandPlay.Loss);
            }
            else
            {
                // pay or collect bets
                hand.Set(hand.Score() > dealerScore ? HandPlay.Win : HandPlay.Loss);
            }
        }
    }

    private void PlaceBets(IEnumerable<Hand> hands)
    {
    }

    private Hand[] Play(Hand hand)
    {
        var otherHands = new List<Hand>();

        try
        {
            var handMove = HandMove.Stand;
            do
            {
                handMove = hand.Move(this.FirstCard);

                if (handMove == HandMove.Stand)
                    break;

                if (handMove == HandMove.Split)
                {
                    otherHands.Add(hand.Split());
                }
                else if (handMove == HandMove.Double)
                {
                    //todo: place new bet
                    var handScore = hand.Score();
                    if (handScore >= 9 && handScore <= 11)
                    {
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                hand.Hit(this.shoe.Draw());
            }
            while (handMove != HandMove.Stand);
        }
        catch (Exception)
        {
            hand.Set(HandPlay.Wrong);
            //foreach (var otherHand in otherHands)
            //{
            //    otherHand.Set(HandPlay.Wrong);
            //}
        }

        return otherHands.ToArray();
    }

    private void Deal(IEnumerable<Hand> hands)
    {
        if (this.cards.Count >= 2)
            throw new InvalidOperationException();

        foreach (var hand in hands)
        {
            hand.Hit(this.shoe.Draw());
        }
        Hit(this.shoe.Draw());
    }
}

public class Shoe
{
    private readonly Stack<Card> cards;

    private Shoe(IEnumerable<Card> cards)
    {
        this.cards = new Stack<Card>(cards);
    }

    public static Shoe Shuffle(int deckCount = 4)
    {
        var cards =
            Shuffle(
                Enumerable
                    .Repeat(Card.StandardDeck, deckCount)
                    .SelectMany(deck => deck))
            .ToArray();

        return new Shoe(cards);
    }

    public static IEnumerable<T> Shuffle<T>(IEnumerable<T> source)
    {
        return source.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue));
    }

    public static T[] Shuffle<T>(params T[] source)
    {
        RandomNumberGenerator.Shuffle(source.AsSpan());
        return source;
    }

    public Card Draw()
    {
        return this.cards.Pop();
    }
}