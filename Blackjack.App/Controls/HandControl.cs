namespace Blackjack.App.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

public class HandControl : Selector
{
    static HandControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HandControl), new FrameworkPropertyMetadata(typeof(HandControl)));
        
        var template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(HandPanel)));
        template.Seal();
        ItemsPanelProperty.OverrideMetadata(typeof(HandControl), new FrameworkPropertyMetadata(template));

        EventManager.RegisterClassHandler(typeof(HandControl), Mouse.MouseUpEvent, new MouseButtonEventHandler(HandleMouseButtonUp), true);
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is CardControl;
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new CardControl();
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        ((CardControl)element).SetCurrentValue(CardControl.ValueProperty, item.ToString());
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        ((CardControl)element).ClearValue(CardControl.ValueProperty);
    }

    internal void NotifyCardClicked(CardControl card, MouseButton mouseButton)
    {
        if (mouseButton == MouseButton.Left && Mouse.Captured != this)
        {
            Mouse.Capture(this, CaptureMode.SubTree);
        }

        if (!card.IsSelected)
        {
            card.SetCurrentValue(IsSelectedProperty, true);
            SetCurrentValue(SelectedItemProperty, this.ItemContainerGenerator.ItemFromContainer(card));
        }
        else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            card.SetCurrentValue(IsSelectedProperty, false);
        }
    }

    private static void HandleMouseButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            var hand = (HandControl)sender;
            hand.ReleaseMouseCapture();
        }
    }
}
