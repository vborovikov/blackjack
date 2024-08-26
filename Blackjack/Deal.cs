namespace Blackjack;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

public readonly record struct Bet(Hand Hand, int Chips);

public class Dealer : HandBase
{
    private const int StandScore = 17;

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

        // third and next deals
        var dealAgain = false;
        do
        {
            dealAgain = DealAgain(hands);
        } while (dealAgain);
    }

    private bool DealAgain(IList<Hand> hands)
    {
        var wouldDealAgain = false;
        var dealerScore = Score();

        for (var i = 0; i < hands.Count; ++i)
        {
            var hand = hands[i];
            if (hand.Play != HandPlay.None)
                continue;

            if (dealerScore >= StandScore)
            {
                // resolve bets

                if (dealerScore > BlackjackScore)
                {
                    hand.Set(HandPlay.Win);
                    //todo: pay bets to all
                    PayBet(hand);
                }
                else if (dealerScore == BlackjackScore)
                {
                    hand.Set(HandPlay.Loss);
                    //todo: collect bets
                    CollectBet(hand);
                }
            }

            if (Play(hand, out var splitHand))
            {
                // insert the split Hand after this Hand but skip both
                hands.Insert(++i, splitHand);
            }

            if (hand.Play == HandPlay.Wrong)
            {
                CollectBet(hand);
                if (splitHand is not null)
                {
                    CollectBet(splitHand);
                }
            }
            else
            {
                PayOut(hand, dealerScore);
                if (splitHand is not null)
                {
                    PayOut(splitHand, dealerScore);
                }
            }

            wouldDealAgain = wouldDealAgain || (hand.Play == HandPlay.None || splitHand?.Play == HandPlay.None);
        }

        // dealer's turn
        if (dealerScore < StandScore)
        {
            Hit(this.shoe.Draw());
        }

        return wouldDealAgain;
    }

    private void PayOut(Hand hand, int dealerScore)
    {
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
        else if (dealerScore >= StandScore)
        {
            if (handScore > dealerScore)
            {
                hand.Set(HandPlay.Win);
                //todo: pay bets
                PayBet(hand);
            }
            else if (handScore < dealerScore)
            {
                hand.Set(HandPlay.Loss);
                //todo: collect bets
                CollectBet(hand);
            }
        }
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

    private bool Play(Hand hand, [MaybeNullWhen(false)] out Hand splitHand)
    {
        splitHand = default;

        try
        {
            var handMove = hand.Move(this.FirstCard);
            if (handMove == HandMove.Stand)
            {
                return false;
            }

            if (handMove == HandMove.Split)
            {
                if (hand.Kind == HandKind.Pair)
                {
                    splitHand = hand.Split();

                    //todo: place new bet
                    PlaceBet(splitHand);
                    hand.Hit(this.shoe.Draw());
                    splitHand.Hit(this.shoe.Draw());

                    return true;
                }
                else
                {
                    hand.Set(HandPlay.Wrong);
                    return false;
                }
            }
            else if (handMove == HandMove.Double)
            {
                if (hand.Score() is 9 or 10 or 11)
                {
                    //todo: place new bet
                    PlaceBet(hand, doubleDown: true);
                    hand.Hit(this.shoe.Draw());
                }
                else
                {
                    hand.Set(HandPlay.Wrong);
                }

                return false;
            }

            hand.Hit(this.shoe.Draw());
        }
        catch (Exception)
        {
            hand.Set(HandPlay.Wrong);
            splitHand?.Set(HandPlay.Wrong);
        }

        return splitHand is not null;
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