namespace Blackjack.App.Controls;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

public class HandPanel : Panel
{
    private const double RotationAngle = 15d;

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
        var horizontalOffset = this.HorizontalItemOffset;
        var verticalOffset = this.VerticalItemOffset;
        var childCount = this.Children.Count;
        var doTransform = CanRotate(childCount);

        var renderBounds = new Rect();
        var angle = GetInitialRotationAngle(childCount);
        var x = 0d; var y = 0d;

        var desiredWidth = 0d;
        var desiredHeight = 0d;

        foreach (UIElement child in this.Children)
        {
            child.Measure(availableSize);

            if (doTransform)
            {
                var desiredBounds = new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height);

                var rotateMatrix = new Matrix();
                rotateMatrix.RotateAt(angle, desiredBounds.Width / 2, desiredBounds.Height);
                desiredBounds.Transform(rotateMatrix);
                renderBounds.Union(desiredBounds);

                angle += RotationAngle;
                x += horizontalOffset;
                y += verticalOffset;
            }
            else
            {
                desiredWidth += horizontalOffset;
                desiredHeight += verticalOffset;
            }
        }

        if (!doTransform && this.Children is [.., UIElement lastChild])
        {
            desiredWidth += lastChild.DesiredSize.Width - horizontalOffset;
            desiredHeight += lastChild.DesiredSize.Height - verticalOffset;
        }

        if (doTransform)
        {
            desiredWidth = renderBounds.Width;
            desiredHeight = renderBounds.Height;
        }

        return new(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontalOffset = this.HorizontalItemOffset;
        var verticalOffset = this.VerticalItemOffset;
        var childCount = this.Children.Count;
        var doTransform = CanRotate(childCount);
        var x = 0d; var y = 0d;
        var angle = GetInitialRotationAngle(childCount);

        if (doTransform && this.Children is [UIElement firstChild, ..])
        {
            var desiredBounds = new Rect(x, y, firstChild.DesiredSize.Width, firstChild.DesiredSize.Height);
            var rotateMatrix = new Matrix();
            rotateMatrix.RotateAt(angle, desiredBounds.Width / 2, desiredBounds.Height);
            desiredBounds.Transform(rotateMatrix);

            x = Math.Abs(desiredBounds.X);
            y = Math.Abs(desiredBounds.Y);
        }

        foreach (UIElement child in this.Children)
        {
            child.Arrange(new(x, y, child.DesiredSize.Width, child.DesiredSize.Height));

            x += horizontalOffset;
            y += verticalOffset;
        }

        if (doTransform)
        {
            foreach (UIElement child in this.Children)
            {
                if (TryGetRotateTransform(child, out var rotate))
                {
                    rotate.CenterX = child.DesiredSize.Width / 2;
                    rotate.CenterY = child.DesiredSize.Height;
                    rotate.BeginAnimation(RotateTransform.AngleProperty, MakeAnimation(angle));
                    angle += RotationAngle;
                }
            }
        }

        return finalSize;
    }

    private static bool TryGetRotateTransform(UIElement element, [MaybeNullWhen(false)] out RotateTransform rotate)
    {
        if (element.RenderTransform is RotateTransform { IsFrozen: false } existingRotate)
        {
            rotate = existingRotate;
            return true;
        }

        if (element.RenderTransform is TransformGroup { IsFrozen: false } group)
        {
            if (group.Children.OfType<RotateTransform>().FirstOrDefault() is not RotateTransform rotateInGroup)
            {
                rotateInGroup = new RotateTransform();
                group.Children.Add(rotateInGroup);
            }

            rotate = rotateInGroup;
            return true;
        }

        if (element.RenderTransform is null || element.RenderTransform == Transform.Identity)
        {
            rotate = new RotateTransform();
            element.RenderTransform = rotate;
            return true;
        }
        else if (!element.RenderTransform.IsFrozen)
        {
            rotate = new RotateTransform();
            element.RenderTransform = new TransformGroup
            {
                Children =
                {
                    element.RenderTransform,
                    rotate,
                }
            };
            return true;
        }

        rotate = default;
        return false;
    }

    private static bool CanRotate(int childCount) => childCount is > 0 and < 6;

    private static double GetInitialRotationAngle(int childCount) => childCount switch
    {
        1 => RotationAngle,
        _ => -RotationAngle * (childCount / 2)
    };

    private static DoubleAnimation MakeAnimation(double to, EventHandler? onCompleted = null)
    {
        var animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(500))
        {
            AccelerationRatio = 0.2,
            DecelerationRatio = 0.7
        };

        if (onCompleted is not null)
        {
            animation.Completed += onCompleted;
        }

        return animation;
    }
}
