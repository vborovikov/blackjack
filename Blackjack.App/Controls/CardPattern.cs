namespace Blackjack.App.Controls;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public sealed class CardPattern : Decorator
{
    private const double ScorePadding = 3.0;
    private static readonly Typeface typeface = new(new(/*"Palatino Linotype"*/"Georgia"), 
        FontStyles.Normal, FontWeights.Normal, FontStretches.UltraCondensed);

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
        var rank = this.Rank;

        if (background is not null)
        {
            // fill the background for the card pattern
            var rect = bounds;
            if (borderBrush is not null)
            {
                var halfThickness = borderThickness.Left * 0.5;
                rect = new(new Point(bounds.Left + halfThickness, bounds.Top + halfThickness),
                    new Point(bounds.Width - halfThickness, bounds.Height - halfThickness));
            }
            drawingContext.DrawRoundedRectangle(background, null, rect, cornerRadius.TopLeft, cornerRadius.TopLeft);
        }
        if (borderBrush is not null)
        {
            // draw outer border
            DrawBorder(drawingContext, borderBrush, borderThickness, cornerRadius, ref bounds);
        }
        if (foreground is not null)
        {
            DrawScore(drawingContext, foreground, rank, suitDrawing, ref bounds);
            if (suitDrawing is not null)
            {
                // draw the pattern for a non-face card
                DrawPattern(drawingContext, foreground, rank, suitDrawing, ref bounds);
            }
        }
    }

    private void DrawScore(DrawingContext drawingContext, Brush brush, CardRank rank, Drawing? suit, ref Rect bounds)
    {
        var textHeight = ScorePadding;
        if (GetRankText(rank) is string { Length: > 0 } str)
        {
            var text = new FormattedText(
                    str, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    typeface, bounds.Height * 0.07, brush, 1.25)
            {
                TextAlignment = TextAlignment.Left,
                Trimming = TextTrimming.None,
                MaxTextWidth = bounds.Width / 2,
                MaxTextHeight = bounds.Height / 2,
            };

            // top left corner
            var textOrigin = new Point(bounds.Left + ScorePadding, bounds.Top);
            drawingContext.DrawText(text, textOrigin);

            // bottom right corner
            drawingContext.PushTransform(new RotateTransform(180.0, base.ActualWidth / 2, base.ActualHeight / 2));
            drawingContext.DrawText(text, textOrigin);
            drawingContext.Pop();

            textHeight = text.Height;
        }

        if (suit is not null)
        {
            //todo: offset and scale transforms
            drawingContext.DrawDrawing(suit);
        }
    }

    private static string GetRankText(CardRank rank) => rank switch
    {
        CardRank.None => "JOKER",
        CardRank.Ace => "A",
        CardRank.Jack => "J",
        CardRank.Queen => "Q",
        CardRank.King => "K",
        _ => new((char)(0x30 + (((int)rank) % 10)), 1)
    };

    private void DrawPattern(DrawingContext drawingContext, Brush brush, CardRank rank, Drawing suit, ref Rect bounds)
    {
    }

    private void DrawBorder(DrawingContext drawingContext, Brush brush, in Thickness thickness, in CornerRadius cornerRadius, ref Rect bounds)
    {
        // only uniform thickness and corner radius are supported

        var size = CollapseThickness(thickness);
        if (size.Width > 0.0 || size.Height > 0.0)
        {
            if (size.Width > bounds.Width || size.Height > bounds.Height)
            {
                if (bounds.Width > 0.0 && bounds.Height > 0.0)
                {
                    drawingContext.DrawRoundedRectangle(brush, null, bounds, cornerRadius.TopLeft, cornerRadius.TopLeft);
                }
                bounds = Rect.Empty;
            }
            else
            {
                if (thickness.Top > 0.0)
                {
                    var pen = GetOrCreateBorderPen(brush, thickness);
                    var halfThickness = pen.Thickness * 0.5;
                    var rect = new Rect(
                        new Point(bounds.Left + halfThickness, bounds.Top + halfThickness),
                        new Point(bounds.Width - halfThickness, bounds.Height - halfThickness));

                    drawingContext.DrawRoundedRectangle(null, pen, rect,
                        cornerRadius.TopLeft, cornerRadius.TopLeft);
                }
                bounds = DeflateRect(bounds, thickness);
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

    private static Rect DeflateRect(in Rect rect, in Thickness thickness)
    {
        return new(rect.Left + thickness.Left, rect.Top + thickness.Top,
            Math.Max(0.0, rect.Width - thickness.Left - thickness.Right),
            Math.Max(0.0, rect.Height - thickness.Top - thickness.Bottom));
    }

    private static Size CollapseThickness(in Thickness thickness)
    {
        return new(Math.Max(0.0, thickness.Left) + Math.Max(0.0, thickness.Right),
            Math.Max(0.0, thickness.Top) + Math.Max(0.0, thickness.Bottom));
    }
}