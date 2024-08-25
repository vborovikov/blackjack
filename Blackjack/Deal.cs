namespace Blackjack;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public readonly record struct Bet(Hand hand, int Chips);

public class Dealer : HandBase
{
    public const int BlackjackScore = 21;

    public static readonly Card[] Upcards;

    private readonly Shoe shoe;
    private readonly Dictionary<Hand, Bet> bets;

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
        this.bets = [];
    }

    public override string ToString() => this.FirstCard.ToString();

    public void Play(IList<Hand> hands)
    {
        PlaceBets(hands);

        // first deal
        Deal(hands);
        // second deal
        Deal(hands);

        // check naturals, including the dealer
        var dealerIsNatural = this.FirstCard.Score is 1 or 10 && this.IsNatural;
        foreach (var hand in hands)
        {
            if (hand.IsNatural)
            {
                if (dealerIsNatural)
                {
                    hand.Set(HandPlay.Tie);
                    //todo: return the bets
                    ReturnBet(hand);
                }
                else
                {
                    hand.Set(HandPlay.Blackjack);
                    //todo: pay 1.5 of their bets
                    PayBet(hand, multiplier: 1.5f);
                }
            }
            else if (dealerIsNatural)
            {
                hand.Set(HandPlay.Loss);
                //todo: collect the bets
                CollectBet(hand);
            }
        }

        // third deal
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
            if (handScore == BlackjackScore)
            {
                hand.Set(HandPlay.Blackjack);
                //todo: pay bets
                PayBet(hand);
            }
            else if (handScore > BlackjackScore)
            {
                hand.Set(HandPlay.Bust);
                //todo: collect bets
                CollectBet(hand);
            }
        }

        // dealer's turn
        while (Score() < 17)
        {
            Hit(this.shoe.Draw());
        }

        var dealerScore = this.Score();
        // resolve bets
        foreach (var hand in hands)
        {
            if (hand.Play != HandPlay.None)
                continue;

            if (dealerScore > 21)
            {
                //todo: pay bets to all
                PayBet(hand);
            }
            else if (dealerScore == 21)
            {
                hand.Set(HandPlay.Loss);
                //todo: collect bets
                CollectBet(hand);
            }
            else
            {
                if (hand.Score() > dealerScore)
                {
                    hand.Set(HandPlay.Win);
                    //todo: pay bets
                    PayBet(hand);
                }
                else
                {
                    hand.Set(HandPlay.Loss);
                    //todo: collect bets
                    CollectBet(hand);
                }
            }
        }
    }

    private void PlaceBets(IEnumerable<Hand> hands)
    {
        foreach (var hand in hands)
        {
            PlaceBet(hand);
        }
    }

    private void PlaceBet(Hand hand, bool doubleDown = false)
    {
        if (doubleDown)
        {
            this.bets.Remove(hand);
        }

        this.bets.Add(hand, hand.MakeBet(doubleDown));
    }

    private void PayBet(Hand hand, float multiplier = 1.0f)
    {
        if (this.bets.Remove(hand, out var bet))
        {
            var reward = bet with { Chips = bet.Chips + Pop((int)MathF.Ceiling(bet.Chips * multiplier)) };
            hand.ResolveBet(reward);
        }
    }

    private void CollectBet(Hand hand)
    {
        if (this.bets.Remove(hand, out var bet))
        {
            Push(bet.Chips);
        }
    }

    private void ReturnBet(Hand hand)
    {
        if (this.bets.Remove(hand, out var bet))
        {
            hand.ResolveBet(bet);
        }
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
                    var anotherHand = hand.Split();
                    otherHands.Add(anotherHand);
                    //todo: place new bet
                    PlaceBet(anotherHand);
                }
                else if (handMove == HandMove.Double)
                {
                    var handScore = hand.Score();
                    if (handScore >= 9 && handScore <= 11)
                    {
                        //todo: place new bet
                        PlaceBet(hand, doubleDown: true);
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