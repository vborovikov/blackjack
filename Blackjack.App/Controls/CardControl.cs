namespace Blackjack.App.Controls;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

public sealed class CardPattern : Decorator
{
    private static readonly Typeface typeface = new("Palatino Linotype");

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(CardPattern),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(CardPattern),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    public static readonly DependencyProperty BorderBrushProperty =
        DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(CardPattern),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    private static readonly DependencyProperty BorderPenProperty =
        DependencyProperty.Register(nameof(BorderPen), typeof(Pen), typeof(CardPattern),
            new PropertyMetadata(null));

    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(CardPattern),
            new FrameworkPropertyMetadata(new Thickness(3d),
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(CardPattern),
            new FrameworkPropertyMetadata(new CornerRadius(6d),
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RankProperty =
        DependencyProperty.Register(nameof(Rank), typeof(CardRank), typeof(CardPattern),
            new FrameworkPropertyMetadata(CardRank.None,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SuitDrawingProperty =
        DependencyProperty.Register(nameof(SuitDrawing), typeof(Drawing), typeof(CardPattern),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Background
    {
        get { return (Brush)GetValue(BackgroundProperty); }
        set { SetValue(BackgroundProperty, value); }
    }

    public Brush Foreground
    {
        get { return (Brush)GetValue(ForegroundProperty); }
        set { SetValue(ForegroundProperty, value); }
    }

    public Brush? BorderBrush
    {
        get { return (Brush)GetValue(BorderBrushProperty); }
        set { SetValue(BorderBrushProperty, value); }
    }

    private Pen? BorderPen
    {
        get { return (Pen)GetValue(BorderPenProperty); }
        set { SetValue(BorderPenProperty, value); }
    }

    public Thickness BorderThickness
    {
        get { return (Thickness)GetValue(BorderThicknessProperty); }
        set { SetValue(BorderThicknessProperty, value); }
    }

    public CornerRadius CornerRadius
    {
        get { return (CornerRadius)GetValue(CornerRadiusProperty); }
        set { SetValue(CornerRadiusProperty, value); }
    }

    public CardRank Rank
    {
        get => (CardRank)GetValue(RankProperty);
        set => SetValue(RankProperty, value);
    }

    public Drawing? SuitDrawing
    {
        get => (Drawing)GetValue(SuitDrawingProperty);
        set => SetValue(SuitDrawingProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var bounds = new Rect(0.0, 0.0, base.ActualWidth, base.ActualHeight);
        var background = this.Background;
        var foreground = this.Foreground;
        var borderThickness = this.BorderThickness;
        var borderBrush = this.BorderBrush;
        var cornerRadius = this.CornerRadius;
        var suitDrawing = this.SuitDrawing;

        if (borderBrush is not null)
        {
            DrawBorder(drawingContext, borderBrush, borderThickness, cornerRadius, ref bounds);
        }
    }

    private void DrawBorder(DrawingContext drawingContext, Brush borderBrush, in Thickness borderThickness, in CornerRadius cornerRadius, ref Rect bounds)
    {
        // only uniform thickness and corner radius are supported

        var size = CollapseThickness(borderThickness);
        if (size.Width > 0.0 || size.Height > 0.0)
        {
            if (size.Width > bounds.Width || size.Height > bounds.Height)
            {
                if (bounds.Width > 0.0 && bounds.Height > 0.0)
                {
                    drawingContext.DrawRoundedRectangle(borderBrush, null, bounds, cornerRadius.TopLeft, cornerRadius.TopLeft);
                }
                bounds = Rect.Empty;
            }
            else
            {
                if (borderThickness.Top > 0.0)
                {
                    var borderPen = GetOrCreateBorderPen(borderBrush, borderThickness);
                    var halfThickness = borderPen.Thickness * 0.5;
                    var rect = new Rect(
                        new Point(bounds.Left + halfThickness, bounds.Top + halfThickness),
                        new Point(bounds.Width - halfThickness, bounds.Height - halfThickness));

                    drawingContext.DrawRoundedRectangle(null, borderPen, rect,
                        cornerRadius.TopLeft, cornerRadius.TopLeft);
                }
                bounds = DeflateRect(bounds, borderThickness);
            }
        }
    }

    private Pen GetOrCreateBorderPen(Brush borderBrush, in Thickness borderThickness)
    {
        var pen = this.BorderPen;

        if (pen is null)
        {
            pen = new Pen
            {
                Brush = borderBrush,
                Thickness = borderThickness.Left
            };
            if (borderBrush.IsFrozen)
            {
                pen.Freeze();
            }

            this.BorderPen = pen;
        }

        return pen;
    }

    private static Rect DeflateRect(in Rect rect, in Thickness thickess)
    {
        return new(rect.Left + thickess.Left, rect.Top + thickess.Top,
            Math.Max(0.0, rect.Width - thickess.Left - thickess.Right),
            Math.Max(0.0, rect.Height - thickess.Top - thickess.Bottom));
    }

    private static Size CollapseThickness(in Thickness thickness)
    {
        return new(Math.Max(0.0, thickness.Left) + Math.Max(0.0, thickness.Right),
            Math.Max(0.0, thickness.Top) + Math.Max(0.0, thickness.Bottom));
    }
}