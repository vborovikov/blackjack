﻿namespace Blackjack
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
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
        protected readonly List<Card> cards;

        protected HandBase(int bank)
        {
            this.cards = new List<Card>(capacity: 4);
            this.Bank = bank;
        }

        public int Bank { get; }

        public Card FirstCard => this.cards.Count > 0 ? this.cards[0] : Card.Joker;
        public bool IsNatural => this.cards.Count == 2 && Score() == 21;
        protected bool HasAce => this.cards.Any(card => card.Rank == CardRank.Ace);

        public override string ToString() =>
            Join("-", this.cards.OrderByDescending(card => card.Order).Select(card => card.ToString()));

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
    }

    public sealed class Hand : HandBase, IEnumerable<Card>
    {
        private sealed class HandLayoutEqualityComparer : IEqualityComparer<Hand>
        {
            public bool Equals(Hand x, Hand y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
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
                .Where(h => h.Score() < 21)
                .Distinct(LayoutEqualityComparer)
                .OrderBy(hand => hand.Kind)
                .ThenByDescending(hand => hand.Sum(c => c.Order))
                .ToArray();
        }

        public Hand(Player player) : base(bank: 1000)
        {
            this.player = player;
        }

        private Hand() : this(Player.None)
        {
        }

        private Hand(Player player, Card card) : this(player)
        {
            this.IsSplit = true;
            Hit(card);
        }

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

        public HandMove Move(Card upcard)
        {
            return this.player.Move(this, upcard);
        }

        public Hand Split()
        {
            if (this.Kind != HandKind.Pair)
                throw new InvalidOperationException();

            var secondCard = this.cards[1];
            this.cards.RemoveAt(1);
            return new Hand(this.player, secondCard);
        }

        public void Set(HandPlay play)
        {
            this.Play = play;
            this.player.EndPlay(this);
        }

        public IEnumerator<Card> GetEnumerator() => this.cards.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public void Add(Card card) => Hit(card);
    }
}