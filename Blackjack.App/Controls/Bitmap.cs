namespace Blackjack.App.Controls;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public sealed class Bitmap : UIElement
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(BitmapSource), typeof(Bitmap),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                new PropertyChangedCallback(Bitmap.OnSourceChanged)));

    private readonly EventHandler sourceDownloaded;
    private readonly EventHandler<ExceptionEventArgs> sourceFailed;
    private Point pixelOffset;

    public Bitmap()
    {
        this.LayoutUpdated += new EventHandler(HandleLayoutUpdated);
        this.sourceDownloaded = new EventHandler(HandleSourceDownloaded);
        this.sourceFailed = new EventHandler<ExceptionEventArgs>(HandleSourceFailed);
    }

    public BitmapSource Source
    {
        get => (BitmapSource)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public event EventHandler<ExceptionEventArgs>? BitmapFailed;

    // Return our measure size to be the size needed to display the bitmap pixels.
    protected override Size MeasureCore(Size availableSize)
    {
        var measureSize = new Size();

        if (this.Source is BitmapSource bitmapSource && PresentationSource.FromVisual(this) is PresentationSource ps)
        {
            var fromDevice = ps.CompositionTarget.TransformFromDevice;

            var pixelSize = new Vector(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            var measureSizeV = fromDevice.Transform(pixelSize);
            measureSize = new Size(measureSizeV.X, measureSizeV.Y);
        }

        return measureSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (this.Source is BitmapSource bitmapSource)
        {
            this.pixelOffset = GetPixelOffset();

            // Render the bitmap offset by the needed amount to align to pixels.
            dc.DrawImage(bitmapSource, new Rect(this.pixelOffset, this.DesiredSize));
        }
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bitmap = (Bitmap)d;

        if (e.OldValue is BitmapSource { IsFrozen: false } oldBitmapSource)
        {
            oldBitmapSource.DownloadCompleted -= bitmap.sourceDownloaded;
            oldBitmapSource.DownloadFailed -= bitmap.sourceFailed;
            oldBitmapSource.DecodeFailed -= bitmap.sourceFailed;
        }
        if (e.NewValue is BitmapSource { IsFrozen: false } newBitmapSource)
        {
            newBitmapSource.DownloadCompleted += bitmap.sourceDownloaded;
            newBitmapSource.DownloadFailed += bitmap.sourceFailed;
            newBitmapSource.DecodeFailed += bitmap.sourceFailed;
        }
    }

    private void HandleSourceDownloaded(object? sender, EventArgs e)
    {
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void HandleSourceFailed(object? sender, ExceptionEventArgs e)
    {
        SetCurrentValue(SourceProperty, null); // setting a local value seems scetchy...
        this.BitmapFailed?.Invoke(this, e);
    }

    private void HandleLayoutUpdated(object? sender, EventArgs e)
    {
        // This event just means that layout happened somewhere.  However, this is
        // what we need since layout anywhere could affect our pixel positioning.
        var pixelOffset = GetPixelOffset();
        if (!AreClose(pixelOffset, this.pixelOffset))
        {
            InvalidateVisual();
        }
    }

    // Gets the matrix that will convert a point from "above" the
    // coordinate space of a visual into the the coordinate space
    // "below" the visual.
    private static Matrix GetVisualTransform(Visual v)
    {
        if (v is not null)
        {
            var m = Matrix.Identity;

            var transform = VisualTreeHelper.GetTransform(v);
            if (transform is not null)
            {
                var cm = transform.Value;
                m = Matrix.Multiply(m, cm);
            }

            var offset = VisualTreeHelper.GetOffset(v);
            m.Translate(offset.X, offset.Y);

            return m;
        }

        return Matrix.Identity;
    }

    private static Point TryApplyVisualTransform(Point point, Visual v, bool inverse, bool throwOnError, out bool success)
    {
        success = true;
        if (v is not null)
        {
            var visualTransform = GetVisualTransform(v);
            if (inverse)
            {
                if (!throwOnError && !visualTransform.HasInverse)
                {
                    success = false;
                    return new Point(0, 0);
                }
                visualTransform.Invert();
            }
            point = visualTransform.Transform(point);
        }
        return point;
    }

    private static Point ApplyVisualTransform(Point point, Visual v, bool inverse)
    {
        return TryApplyVisualTransform(point, v, inverse, true, out _);
    }

    private Point GetPixelOffset()
    {
        var pixelOffset = new Point();

        if (PresentationSource.FromVisual(this) is PresentationSource ps)
        {
            var rootVisual = ps.RootVisual;

            // Transform (0,0) from this element up to pixels.
            pixelOffset = TransformToAncestor(rootVisual).Transform(pixelOffset);
            pixelOffset = ApplyVisualTransform(pixelOffset, rootVisual, false);
            pixelOffset = ps.CompositionTarget.TransformToDevice.Transform(pixelOffset);

            // Round the origin to the nearest whole pixel.
            pixelOffset.X = Math.Round(pixelOffset.X);
            pixelOffset.Y = Math.Round(pixelOffset.Y);

            // Transform the whole-pixel back to this element.
            pixelOffset = ps.CompositionTarget.TransformFromDevice.Transform(pixelOffset);
            pixelOffset = ApplyVisualTransform(pixelOffset, rootVisual, true);
            pixelOffset = rootVisual.TransformToDescendant(this).Transform(pixelOffset);
        }

        return pixelOffset;
    }

    private static bool AreClose(Point point1, Point point2)
    {
        return AreClose(point1.X, point2.X) && AreClose(point1.Y, point2.Y);
    }

    private static bool AreClose(double value1, double value2)
    {
        if (value1 == value2)
        {
            return true;
        }
        var delta = value1 - value2;
        return ((delta < 1.53E-06) && (delta > -1.53E-06));
    }
}
