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
        DependencyProperty.Register(nameof(Value), typeof(Card?), typeof(CardControl), 
            new PropertyMetadata(null, (d, e) => ((CardControl)d).OnValueChanged(e)));

    [Bindable(true), Category("Content")]
    public Card? Value
    {
        get => (Card?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected virtual void OnValueChanged(DependencyPropertyChangedEventArgs e)
    {

    }
}