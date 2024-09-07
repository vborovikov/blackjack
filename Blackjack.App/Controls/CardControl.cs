namespace Blackjack.App.Controls;

using System.Collections.Frozen;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

[TemplatePart(Name = TemplateParts.LayoutRoot, Type = typeof(Panel))]
public class CardControl : Control
{
    private static class TemplateParts
    {
        public const string LayoutRoot = "LayoutRoot";
    }

    private static readonly string DefaultBackgroundKey = Card.Back.ToString();
    private static readonly FrozenDictionary<string, Brush> backgrounds;

    static CardControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CardControl), new FrameworkPropertyMetadata(typeof(CardControl)));

        backgrounds = Card.StandardDeck
            .Select(card => card.ToString())
            .Concat([DefaultBackgroundKey])
            .ToFrozenDictionary(card => card, card =>
            {
                var image = new BitmapImage(
                    new Uri($"pack://application:,,,/Blackjack.App;component/Resources/Decks/Classic/{card}.png"));

                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

                var brush = new ImageBrush(image);
                brush.Freeze();

                return (Brush)brush;
            }, StringComparer.Ordinal);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
    DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(CardControl),
        new FrameworkPropertyMetadata(new CornerRadius(6d),
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public CornerRadius CornerRadius
    {
        get { return (CornerRadius)GetValue(CornerRadiusProperty); }
        set { SetValue(CornerRadiusProperty, value); }
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(CardControl),
            new PropertyMetadata(null, (d, e) => ((CardControl)d).OnValueChanged(e)));

    internal static readonly DependencyPropertyKey SuitPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(Suit), typeof(CardSuit), typeof(CardControl),
            new PropertyMetadata(CardSuit.Unknown));

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

    public static readonly DependencyProperty IsSelectedProperty = 
        Selector.IsSelectedProperty.AddOwner(typeof(CardControl), new FrameworkPropertyMetadata(false, 
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal));

    /// <summary>
    ///     Indicates whether this ListBoxItem is selected.
    /// </summary>
    [Bindable(true), Category("Appearance")]
    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
    }

    private HandControl? ParentHand => ItemsControl.ItemsControlFromItemContainer(this) as HandControl;

    protected virtual void OnValueChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is string cardStr && Card.TryParse(cardStr, out var card))
        {
            SetValue(SuitPropertyKey, card.Suit);
            SetValue(RankPropertyKey, card.Rank);
        }

        ChangeBackground(e.NewValue as string);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ChangeBackground(this.Value);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (!e.Handled)
        {
            e.Handled = true;
            HandleMouseButtonDown(MouseButton.Left);
        }
        base.OnMouseLeftButtonDown(e);
    }

    private void HandleMouseButtonDown(MouseButton mouseButton)
    {
        if (Focus())
        {
            this.ParentHand?.NotifyCardClicked(this, mouseButton);
        }
    }

    private void ChangeBackground(string? value)
    {
        if (!backgrounds.TryGetValue(value ?? DefaultBackgroundKey, out var brush))
        {
            brush = backgrounds[DefaultBackgroundKey];
        }
        SetCurrentValue(BackgroundProperty, brush);
    }
}
