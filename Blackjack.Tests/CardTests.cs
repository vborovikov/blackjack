namespace Blackjack.Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CardTests
{
    [TestMethod]
    public void TryFormat_Default_Joker()
    {
        Span<char> span = stackalloc char[2];
        Card card = default;

        Assert.IsTrue(card.TryFormat(span, out var charsWritten, [], null));
        Assert.AreEqual(2, charsWritten);
        Assert.AreEqual("0X", span[..charsWritten].ToString());
    }
}
