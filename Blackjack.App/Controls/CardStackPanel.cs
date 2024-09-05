namespace Blackjack.App.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

// Solitaire and Spider Solitaire for WPF
// The Code Project Open License (CPOL) 1.02
// https://www.codeproject.com/Articles/252152/Solitaire-and-Spider-Solitaire-for-WPF
// https://www.codeproject.com/info/cpol10.aspx

/// <summary>
/// The offset mode - how we offset individual cards in a stack.
/// </summary>
public enum OffsetMode
{
    /// <summary>
    /// Offset every card.
    /// </summary>
    EveryCard,

    /// <summary>
    /// Offset every Nth card.
    /// </summary>
    EveryNthCard,

    /// <summary>
    /// Offset only the top N cards.
    /// </summary>
    TopNCards,

    /// <summary>
    /// Offset only the bottom N cards.
    /// </summary>
    BottomNCards,

    /// <summary>
    /// Use the offset values specified in the playing card class (see
    /// PlayingCard.FaceDownOffset and PlayingCard.FaceUpOffset).
    /// </summary>
    UseCardValues
}

/// <summary>
/// A panel for laying out cards.
/// </summary>
public class CardStackPanel : StackPanel
{
    /// <summary>
    /// Infinite size, useful later.
    /// </summary>
    private readonly Size infiniteSpace = new(double.MaxValue, double.MaxValue);

    /// <summary>
    /// Measures the child elements of a <see cref="T:System.Windows.Controls.StackPanel"/> in anticipation of arranging them during the <see cref="M:System.Windows.Controls.StackPanel.ArrangeOverride(System.Windows.Size)"/> pass.
    /// </summary>
    /// <param name="constraint">An upper limit <see cref="T:System.Windows.Size"/> that should not be exceeded.</param>
    /// <returns>
    /// The <see cref="T:System.Windows.Size"/> that represents the desired size of the element.
    /// </returns>
    protected override Size MeasureOverride(Size constraint)
    {
        //  Keep track of the overall size required.
        var resultSize = new Size(0, 0);

        //  Get the offsets that each element will need.
        var offsets = CalculateOffsets();

        //  Calculate the total.
        var totalX = (from o in offsets select o.Width).Sum();
        var totalY = (from o in offsets select o.Height).Sum();

        //  Measure each child (always needed, even if we don't use
        //  the measurement!)
        foreach(UIElement child in this.Children)
        {
            //  Measure the child against infinite space.
            child.Measure(this.infiniteSpace);
        }

        //  Add the size of the last element.
        if (this.LastChild != null)
        {
            //  Add the size.
            totalX += this.LastChild.DesiredSize.Width;
            totalY += this.LastChild.DesiredSize.Height;
        }
                    
        return new Size(totalX, totalY);
    }

    /// <summary>
    /// When overridden in a derived class, positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement"/> derived class.
    /// </summary>
    /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
    /// <returns>The actual size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0, y = 0;
        var n = 0;
        var total = this.Children.Count;

        //  Get the offsets that each element will need.
        var offsets = CalculateOffsets();
        
        //  If we're going to pass the bounds, deal with it.
        if ((this.ActualWidth > 0 && finalSize.Width > this.ActualWidth) || 
            (this.ActualHeight > 0 && finalSize.Height > this.ActualHeight))
        {
            //  Work out the amount we have to remove from the offsets.
            var overrunX = finalSize.Width - this.ActualWidth;
            var overrunY = finalSize.Height - this.ActualHeight;

            //  Now as a per-offset.
            var dx = overrunX / offsets.Count;
            var dy = overrunY / offsets.Count;

            //  Now nudge each offset.
            for (var i = 0; i < offsets.Count; i++)
            {
                offsets[i] = new Size(Math.Max(0, offsets[i].Width - dx), Math.Max(0, offsets[i].Height - dy));
            }

            //  Make sure the final size isn't increased past what we can handle.
            finalSize.Width -= overrunX;
            finalSize.Height -= overrunY;
        }

        //  Arrange each child.
        foreach (UIElement child in this.Children)
        {
            //  Get the card. If we don't have one, skip.
            if (child is not CardControl card)
                continue;

            //  Arrange the child at x,y (the first will be at 0,0)
            child.Arrange(new Rect(x, y, child.DesiredSize.Width, child.DesiredSize.Height));

            //  Update the offset.
            x += offsets[n].Width;
            y += offsets[n].Height;

            //  Increment.
            n++;
        }

        return finalSize;
    }

    /// <summary>
    /// Calculates the offsets.
    /// </summary>
    /// <returns></returns>
    private List<Size> CalculateOffsets()
    {
        //  Calculate the offsets on a card by card basis.
        var offsets = new List<Size>();

        var n = 0;
        var total = this.Children.Count;

        //  Go through each card.
        foreach (UIElement child in this.Children)
        {
            //  Get the card. If we don't have one, skip.
            if (child is not CardControl card)
                continue;

            //  The amount we'll offset by.
            double faceDownOffset = 0;
            double faceUpOffset = 0;

            //  We are now going to offset only if the offset mode is appropriate.
            switch (this.OffsetMode)
            {
                case OffsetMode.EveryCard:
                    //  Offset every card.
                    faceDownOffset = this.FaceDownOffset;
                    faceUpOffset = this.FaceUpOffset;
                    break;
                case OffsetMode.EveryNthCard:
                    //  Offset only if n Mod N is zero.
                    if (((n + 1) % this.NValue) == 0)
                    {
                        faceDownOffset = this.FaceDownOffset;
                        faceUpOffset = this.FaceUpOffset;
                    }
                    break;
                case OffsetMode.TopNCards:
                    //  Offset only if (Total - N) <= n < Total
                    if ((total - this.NValue) <= n && n < total)
                    {
                        faceDownOffset = this.FaceDownOffset;
                        faceUpOffset = this.FaceUpOffset;
                    }
                    break;
                case OffsetMode.BottomNCards:
                    //  Offset only if 0 < n < N
                    if (n < this.NValue)
                    {
                        faceDownOffset = this.FaceDownOffset;
                        faceUpOffset = this.FaceUpOffset;
                    }
                    break;
                case OffsetMode.UseCardValues:
                    //  Offset each time by the amount specified in the card object.
                    //faceDownOffset = card.FaceDownOffset;
                    //faceUpOffset = card.FaceUpOffset;
                    break;
                default:
                    break;
            }

            n++;

            //  Create the offset as a size.
            var offset = new Size(0, 0);
            
            //  Offset.
            switch (this.Orientation)
            {
                case Orientation.Horizontal:
                    offset.Width = card.Suit == CardSuit.Unknown ? faceDownOffset : faceUpOffset;
                    break;
                case Orientation.Vertical:
                    offset.Height = card.Suit == CardSuit.Unknown ? faceDownOffset : faceUpOffset;
                    break;
                default:
                    break;
            }

            //  Add to the list.
            offsets.Add(offset);
        }

        return offsets;
    }

    /// <summary>
    /// Gets the last child.
    /// </summary>
    /// <value>The last child.</value>
    private UIElement? LastChild => this.Children.Count > 0 ? this.Children[^1] : null;

    /// <summary>
    /// Face down offset.
    /// </summary>
    private static readonly DependencyProperty FaceDownOffsetProperty =
      DependencyProperty.Register(nameof(FaceDownOffset), typeof(double), typeof(CardStackPanel),
      new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>
    /// Gets or sets the face down offset.
    /// </summary>
    /// <value>The face down offset.</value>
    public double FaceDownOffset
    {
        get => (double)GetValue(FaceDownOffsetProperty);
        set => SetValue(FaceDownOffsetProperty, value);
    }

    /// <summary>
    /// Face up offset.
    /// </summary>
    private static readonly DependencyProperty FaceUpOffsetProperty =
      DependencyProperty.Register(nameof(FaceUpOffset), typeof(double), typeof(CardStackPanel),
      new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>
    /// Gets or sets the face up offset.
    /// </summary>
    /// <value>The face up offset.</value>
    public double FaceUpOffset
    {
        get => (double)GetValue(FaceUpOffsetProperty);
        set => SetValue(FaceUpOffsetProperty, value);
    }

    /// <summary>
    /// The offset mode.
    /// </summary>
    private static readonly DependencyProperty OffsetModeProperty =
      DependencyProperty.Register(nameof(OffsetMode), typeof(OffsetMode), typeof(CardStackPanel),
      new PropertyMetadata(OffsetMode.EveryCard));

    /// <summary>
    /// Gets or sets the offset mode.
    /// </summary>
    /// <value>The offset mode.</value>
    public OffsetMode OffsetMode
    {
        get => (OffsetMode)GetValue(OffsetModeProperty);
        set => SetValue(OffsetModeProperty, value);
    }

    /// <summary>
    /// The NValue, used for some modes.
    /// </summary>
    private static readonly DependencyProperty NValueProperty =
      DependencyProperty.Register(nameof(NValue), typeof(int), typeof(CardStackPanel),
      new PropertyMetadata(1));

    /// <summary>
    /// Gets or sets the N value.
    /// </summary>
    /// <value>The N value.</value>
    public int NValue
    {
        get => (int)GetValue(NValueProperty);
        set => SetValue(NValueProperty, value);
    }
}
