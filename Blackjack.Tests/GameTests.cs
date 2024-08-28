namespace Blackjack.Tests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class GameTests
{
    private static ILoggerFactory loggerFactory;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        loggerFactory = new LoggerFactory();
        loggerFactory.AddProvider(new DebugLoggerProvider());
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        loggerFactory.Dispose();
    }

    [TestMethod]
    public void ObjectOfTheGame()
    {
        var dealer = new Dealer(loggerFactory.CreateLogger<Dealer>());
        var player = Player.Basic;
        var hands = player.Play(1, Hand.DefaultBank);
        var hand = hands[0];

        dealer.Play(hands);

        Assert.AreNotEqual(HandPlay.Wrong, hand.Play);
        if (hand.Play is HandPlay.Win or HandPlay.Blackjack)
        {
            Assert.IsTrue(hand.Bank > Hand.DefaultBank);
        }
        else if (hand.Play == HandPlay.Tie)
        {
            Assert.AreEqual(Hand.DefaultBank, hand.Bank);
        }
        else
        {
            Assert.IsTrue(hand.Bank < Hand.DefaultBank);
        }
    }
}
