namespace Blackjack.App.Controls;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

[TemplatePart(Name = TemplateParts.LayoutRoot, Type = typeof(Panel))]
[TemplatePart(Name = TemplateParts.MoveSymbol, Type = typeof(TextBlock))]
public class HandMoveControl : Control
{
    private static class TemplateParts
    {
        public const string LayoutRoot = "LayoutRoot";
        public const string MoveSymbol = "MoveSymbol";
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(HandMove?),
            typeof(HandMoveControl), new PropertyMetadata(null, (d, e) => ((HandMoveControl)d).OnValueChanged(e)));

    public static readonly DependencyProperty EmptyTextProperty =
        DependencyProperty.Register(nameof(EmptyText), typeof(string),
            typeof(HandMoveControl), new PropertyMetadata(String.Empty, (d, e) => ((HandMoveControl)d).OnEmptyTextChanged(e)));

    private Panel layoutRoot;
    private TextBlock moveSymbol;

    static HandMoveControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HandMoveControl), new FrameworkPropertyMetadata(typeof(HandMoveControl)));
    }

    public HandMove? Value
    {
        get { return (HandMove?)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public string EmptyText
    {
        get { return (string)GetValue(EmptyTextProperty); }
        set { SetValue(EmptyTextProperty, value); }
    }

    public override void OnApplyTemplate()
    {
        this.moveSymbol = GetTemplateChild(TemplateParts.MoveSymbol) as TextBlock;
        this.layoutRoot = GetTemplateChild(TemplateParts.LayoutRoot) as Panel;

        OnValueChanged(ValueProperty.ChangedEventArgs(this.Value));
    }

    private void OnEmptyTextChanged(DependencyPropertyChangedEventArgs e)
    {
        var text = e.NewValue as string;
        if (this.Value != null && this.moveSymbol != null)
        {
            this.moveSymbol.Text = text;
        }
    }

    private void OnValueChanged(DependencyPropertyChangedEventArgs e)
    {
        var newMove = e.NewValue as HandMove?;

        if (this.moveSymbol != null)
        {
            this.moveSymbol.Text = newMove is null ? this.EmptyText : PlayRule.MoveToSymbol(newMove.Value).ToString();
        }
        if (this.layoutRoot != null)
        {
            this.layoutRoot.Background = newMove switch
            {
                HandMove.Stand => Brushes.MediumVioletRed,
                HandMove.Hit => Brushes.LightGreen,
                HandMove.Double => Brushes.Yellow,
                HandMove.Split => Brushes.MediumPurple,
                _ => Brushes.LightGray,
            };
        }
    }
}