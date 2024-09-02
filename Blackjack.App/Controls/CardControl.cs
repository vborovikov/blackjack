namespace Blackjack.App.Controls;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

[TemplatePart(Name = TemplateParts.LayoutRoot, Type = typeof(Panel))]
public class CardControl : Control
{
    private static class TemplateParts
    {
        public const string LayoutRoot = "LayoutRoot";
    }

    static CardControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CardControl), new FrameworkPropertyMetadata(typeof(CardControl)));
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(CardControl), 
            new PropertyMetadata(null, (d, e) => ((CardControl)d).OnValueChanged(e)));

    internal static readonly DependencyPropertyKey SuitPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(Suit), typeof(CardSuit), typeof(CardControl),
            new PropertyMetadata(CardSuit.Joker));

    public static readonly DependencyProperty SuitProperty = SuitPropertyKey.DependencyProperty;

    internal static readonly DependencyPropertyKey RankPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(Rank), typeof(CardRank), typeof(CardControl),
            new PropertyMetadata(CardRank.None));

    public static readonly DependencyProperty RankProperty = RankPropertyKey.DependencyProperty;

    [Bindable(true), Category("Content")]
    public string? Value
    {
        get => GetValue(ValueProperty) as string;
        set => SetValue(ValueProperty, value);
    }

    [Bindable(false), Browsable(false)]
    public CardSuit Suit => (CardSuit)GetValue(SuitProperty);

    [Bindable(false), Browsable(false)]
    public CardRank Rank => (CardRank)GetValue(RankProperty);

    protected virtual void OnValueChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is string cardStr && Card.TryParse(cardStr, out var card))
        {
            SetValue(SuitPropertyKey, card.Suit);
            SetValue(RankPropertyKey, card.Rank);
        }
    }
}