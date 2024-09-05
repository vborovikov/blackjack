namespace Blackjack;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public readonly record struct Bet(Hand Hand, int Chips);

public class Dealer : HandBase
{
    private const int StandScore = 17;
    private static int globalGameId = 0;

    // Upcards are for information only, not real cards
    public static readonly Card[] Upcards =
    [
        new(CardSuit.Unknown, CardRank.Two),
        new(CardSuit.Unknown, CardRank.Three),
        new(CardSuit.Unknown, CardRank.Four),
        new(CardSuit.Unknown, CardRank.Five),
        new(CardSuit.Unknown, CardRank.Six),
        new(CardSuit.Unknown, CardRank.Seven),
        new(CardSuit.Unknown, CardRank.Eight),
        new(CardSuit.Unknown, CardRank.Nine),
        new(CardSuit.Unknown, CardRank.Ten),
        new(CardSuit.Unknown, CardRank.Ace),
    ];

    private readonly EventId gameId;
    private readonly ILogger log;
    private readonly Shoe shoe;
    private readonly Dictionary<Hand, Bet> bets;

    public Dealer() : this(NullLogger.Instance)
    {
        this.shoe = Shoe.Shuffle();
        this.bets = [];
    }

    public Dealer(ILogger logger) : base(bank: 50000)
    {
        this.gameId = new EventId(Interlocked.Increment(ref globalGameId));
        this.log = logger;
        this.shoe = Shoe.Shuffle();
        this.bets = [];
    }

    public void Play(IList<Hand> hands)
    {
        this.log.LogInformation(this.gameId, "Game started");

        PlaceBets(hands);

        // first deal
        Deal(hands);
        // second deal
        Deal(hands);

        this.log.LogDebug(this.gameId, "Dealer: {Dealer}", this);
        this.log.LogDebug(this.gameId, "Bets: {Bets}", string.Join(", ", this.bets.Select(bet => bet.Value.ToString())));

        // check naturals, including the dealer
        this.log.LogInformation(this.gameId, "Checking naturals");
        var dealerIsNatural = this.FirstCard.Score is 1 or 10 && this.IsNatural;
        this.log.LogDebug(this.gameId, "Dealer is natural: {DealerIsNatural}", dealerIsNatural);
        foreach (var hand in hands)
        {
            if (hand.IsNatural)
            {
                if (dealerIsNatural)
                {
                    hand.Set(HandPlay.Tie);
                    ReturnBet(hand);

                    this.log.LogDebug(this.gameId, "Hand {Hand} is a tie", hand.ToString());
                }
                else
                {
                    hand.Set(HandPlay.Blackjack);
                    PayBet(hand, multiplier: 1.5f);

                    this.log.LogDebug(this.gameId, "Hand {Hand} is blackjack", hand.ToString());
                }
            }
            else if (dealerIsNatural)
            {
                hand.Set(HandPlay.Loss);
                CollectBet(hand);

                this.log.LogDebug(this.gameId, "Hand {Hand} is a loss", hand.ToString());
            }
        }

        // third and next deals
        var dealAgain = false;
        do
        {
            dealAgain = DealAgain(hands);
        } while (dealAgain);

        this.log.LogInformation(this.gameId, "Game ended");
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

            if (dealerScore >= BlackjackScore)
            {
                // resolve bets

                if (dealerScore > BlackjackScore)
                {
                    hand.Set(HandPlay.Win);
                    PayBet(hand);

                    this.log.LogDebug(this.gameId, "Hand {Hand} is a win", hand.ToString());
                }
                else if (dealerScore == BlackjackScore)
                {
                    hand.Set(HandPlay.Loss);
                    CollectBet(hand);

                    this.log.LogDebug(this.gameId, "Hand {Hand} is a loss", hand.ToString());
                }

                continue;
            }

            if (Play(hand, dealerScore, out var splitHand))
            {
                // insert the split Hand after this Hand but skip both
                hands.Insert(++i, splitHand);
            }

            if (hand.Play == HandPlay.Wrong)
            {
                CollectBet(hand);

                this.log.LogDebug(this.gameId, "Hand {Hand} is out", hand.ToString());
                if (splitHand is not null)
                {
                    CollectBet(splitHand);

                    this.log.LogDebug(this.gameId, "Hand {Hand} is out", splitHand.ToString());
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

            wouldDealAgain = wouldDealAgain || hand.Play == HandPlay.None || splitHand?.Play == HandPlay.None;
        }

        // dealer's turn
        if (dealerScore < StandScore)
        {
            Hit(this.shoe.Draw());
            this.log.LogDebug(this.gameId, "Dealer {Dealer} hits", this);
        }
        else
        {
            this.log.LogDebug(this.gameId, "Dealer {Dealer} stands", this);
        }

        return wouldDealAgain;
    }

    private void PayOut(Hand hand, int dealerScore)
    {
        var handScore = hand.Score();
        if (handScore == BlackjackScore)
        {
            hand.Set(HandPlay.Blackjack);
            PayBet(hand);

            this.log.LogDebug(this.gameId, "Hand {Hand} is a blackjack", hand.ToString());
        }
        else if (handScore > BlackjackScore)
        {
            hand.Set(HandPlay.Bust);
            CollectBet(hand);

            this.log.LogDebug(this.gameId, "Hand {Hand} is a bust", hand.ToString());
        }
        else if (dealerScore >= StandScore)
        {
            if (handScore > dealerScore)
            {
                hand.Set(HandPlay.Win);
                PayBet(hand);

                this.log.LogDebug(this.gameId, "Hand {Hand} is a win", hand.ToString());
            }
            else if (handScore < dealerScore)
            {
                hand.Set(HandPlay.Loss);
                CollectBet(hand);

                this.log.LogDebug(this.gameId, "Hand {Hand} is a loss", hand.ToString());
            }
            else
            {
                hand.Set(HandPlay.Tie);
                ReturnBet(hand);

                this.log.LogDebug(this.gameId, "Hand {Hand} is a tie", hand.ToString());
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

    private bool Play(Hand hand, int dealerScore, [MaybeNullWhen(false)] out Hand splitHand)
    {
        splitHand = default;

        try
        {
            var handMove = hand.Move(this.FirstCard, dealerScore);
            this.log.LogDebug(this.gameId, "Hand {Hand} plays {HandMove}", hand.ToString(), handMove);

            if (handMove == HandMove.Stand)
            {
                return false;
            }

            if (handMove == HandMove.Split)
            {
                if (hand.Kind == HandKind.Pair)
                {
                    splitHand = hand.Split();

                    PlaceBet(splitHand);
                    hand.Hit(this.shoe.Draw());
                    splitHand.Hit(this.shoe.Draw());

                    this.log.LogDebug(this.gameId, "Hand {Hand} and {SplitHand} split", hand.ToString(), splitHand.ToString());
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
                    PlaceBet(hand, doubleDown: true);
                    hand.Hit(this.shoe.Draw());

                    this.log.LogDebug(this.gameId, "Hand {Hand} doubles down", hand.ToString());
                }
                else
                {
                    hand.Set(HandPlay.Wrong);
                }

                return false;
            }

            hand.Hit(this.shoe.Draw());
            this.log.LogDebug(this.gameId, "Hand {Hand} hits", hand.ToString());
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
        this.log.LogInformation(this.gameId, "Placing bets");

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

        var bet = hand.MakeBet(doubleDown);
        this.bets.Add(hand, bet);

        this.log.LogDebug(this.gameId, "Bet placed: {Hand} -> {Bet}", hand.ToString(), bet);
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

    public Card Draw()
    {
        return this.cards.Pop();
    }
}