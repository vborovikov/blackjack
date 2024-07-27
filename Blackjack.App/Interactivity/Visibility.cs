namespace Blackjack.App.Interactivity
{
    using System.Windows;
    using WpfVisibility = System.Windows.Visibility;

    public static class Visibility
    {
        public static readonly DependencyProperty VisibleIfProperty =
            DependencyProperty.RegisterAttached("VisibleIf", typeof(bool),
                typeof(Visibility), new UIPropertyMetadata(true, HandleVisibleIfPropertyChanged));

        public static readonly DependencyProperty InvisibleAsProperty =
            DependencyProperty.RegisterAttached("InvisibleAs", typeof(WpfVisibility),
                typeof(Visibility), new UIPropertyMetadata(WpfVisibility.Collapsed));

        public static bool GetVisibleIf(DependencyObject obj)
        {
            return (bool)obj.GetValue(VisibleIfProperty);
        }

        public static void SetVisibleIf(DependencyObject obj, bool value)
        {
            obj.SetValue(VisibleIfProperty, value);
        }

        public static WpfVisibility GetInvisibleAs(DependencyObject obj)
        {
            return (WpfVisibility)obj.GetValue(InvisibleAsProperty);
        }

        public static void SetInvisibleAs(DependencyObject obj, WpfVisibility value)
        {
            obj.SetValue(InvisibleAsProperty, value);
        }

        private static void HandleVisibleIfPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element != null)
            {
                var isVisible = (bool)e.NewValue;
                element.Visibility = isVisible ? WpfVisibility.Visible : GetInvisibleAs(element);
            }
        }
    }
}