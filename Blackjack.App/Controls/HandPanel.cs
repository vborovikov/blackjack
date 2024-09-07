namespace Blackjack.App.Controls;

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

public class HandPanel : Panel
{
    public static readonly DependencyProperty HorizontalItemOffsetProperty =
        DependencyProperty.Register(nameof(HorizontalItemOffset), typeof(double), typeof(HandPanel),
            new FrameworkPropertyMetadata(5d, FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceOffsetPropertyValue));

    public static readonly DependencyProperty VerticalItemOffsetProperty =
        DependencyProperty.Register(nameof(VerticalItemOffset), typeof(double), typeof(HandPanel),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceOffsetPropertyValue));

    public double HorizontalItemOffset
    {
        get => (double)GetValue(HorizontalItemOffsetProperty);
        set => SetValue(HorizontalItemOffsetProperty, value);
    }

    public double VerticalItemOffset
    {
        get => (double)GetValue(VerticalItemOffsetProperty);
        set => SetValue(VerticalItemOffsetProperty, value);
    }

    private static object CoerceOffsetPropertyValue(DependencyObject d, object baseValue)
    {
        return baseValue is double offset ? Math.Max(0d, offset) : 0d;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredWidth = 0d;
        var desiredHeight = 0d;
        var horizontalOffset = this.HorizontalItemOffset;
        var verticalOffset = this.VerticalItemOffset;
        var childCount = this.Children.Count;
        var doTransform = childCount is > 1 and < 6;

        foreach (UIElement child in this.Children)
        {
            child.Measure(availableSize);
            desiredWidth += horizontalOffset;
            desiredHeight += verticalOffset;
        }

        if (this.Children is [.., UIElement lastChild])
        {
            desiredWidth += lastChild.DesiredSize.Width - horizontalOffset;
            desiredHeight += lastChild.DesiredSize.Height - verticalOffset;
        }

        if (doTransform)
        {
            desiredWidth += GetTransformOffsetWidth(childCount, horizontalOffset);
            desiredHeight += GetTransformOffsetHeight(childCount, verticalOffset);
        }

        if (!double.IsPositiveInfinity(availableSize.Width))
        {
            desiredWidth = availableSize.Width;
        }
        if (!double.IsPositiveInfinity(availableSize.Height))
        {
            desiredHeight = availableSize.Height;
        }

        return new(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontalOffset = this.HorizontalItemOffset;
        var verticalOffset = this.VerticalItemOffset;
        var childCount = this.Children.Count;
        var doTransform = childCount is > 1 and < 6;
        var x = doTransform ? GetTransformOffsetWidth(childCount, horizontalOffset) : 0d;
        var y = doTransform ? GetTransformOffsetHeight(childCount, verticalOffset) : 0d;
        
        foreach (UIElement child in this.Children)
        {
            if (doTransform && child.RenderTransform is not RotateTransform)
            {
                child.RenderTransformOrigin = new(0.5, 1);
                child.RenderTransform = new RotateTransform();
            }

            child.Arrange(new(x, y, child.DesiredSize.Width, child.DesiredSize.Height));

            x += horizontalOffset;
            y += verticalOffset;
        }

        if (doTransform)
        {
            var angle = -15d * (childCount / 2);
            foreach (UIElement child in this.Children)
            {
                if (child.RenderTransform is RotateTransform rotate)
                {
                    //rotate.Angle = angle;
                    rotate.BeginAnimation(RotateTransform.AngleProperty, MakeAnimation(angle));
                    angle += 15;
                }
            }
        }

        return finalSize;
    }

    private static double GetTransformOffsetWidth(int childCount, double horizontalOffset)
    {
        return horizontalOffset * childCount * 2 - horizontalOffset;
    }

    private static double GetTransformOffsetHeight(int childCount, double verticalOffset)
    {
        return Math.Max(verticalOffset, 5d) * childCount / 2;
    }

    private static DoubleAnimation MakeAnimation(double to, EventHandler? onCompleted = null)
    {
        var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(500))
        {
            AccelerationRatio = 0.2,
            DecelerationRatio = 0.7
        };

        if (onCompleted is not null)
        {
            anim.Completed += onCompleted;
        }

        return anim;
    }
}
