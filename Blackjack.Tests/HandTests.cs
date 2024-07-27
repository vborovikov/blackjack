namespace Blackjack.Tests
{
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HandTests
    {
        [TestMethod]
        public void ToString_AceNine_ReverseOrder()
        {
            var hand = new Hand(Player.None)
            {
                new Card(CardSuit.Clubs, CardRank.Nine),
                new Card(CardSuit.Diamonds, CardRank.Ace),
            };

            Assert.AreEqual("A-9", hand.ToString());
        }

        [TestMethod]
        public void Score_AceTen_IsNatural()
        {
            var hand = new Hand(Player.None)
            {
                new Card(CardSuit.Spades, CardRank.Ace),
                new Card(CardSuit.Clubs, CardRank.Ten),
            };

            Assert.IsTrue(hand.IsNatural);
        }

        [TestMethod]
        public void DeckDeals_AllHands_1326()
        {
            Assert.AreEqual(1326, Hand.AllDeals.Length);
        }

        [TestMethod]
        public void DeckDeals_UniqueScores_34()
        {
            Assert.AreEqual(34, Hand.Deals.Length);
        }

        [TestMethod]
        public void Equals_Sixes_AreEqual()
        {
            var sixes = Hand.AllDeals
                .Where(h =>
                    h.Kind == HandKind.Pair &&
                    h.FirstCard.Rank != CardRank.Ace &&
                    h.Score() == 12)
                .ToArray();

            for (var i = 0; i != sixes.Length; ++i)
            {
                for (var j = i + 1; j != sixes.Length; ++j)
                {
                    var first = sixes[i];
                    var second = sixes[j];

                    Assert.AreEqual(first, second);
                    Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
                    Assert.AreEqual("6-6", first.ToString());
                    Assert.AreEqual("6-6", second.ToString());
                }
            }
        }
    }
}