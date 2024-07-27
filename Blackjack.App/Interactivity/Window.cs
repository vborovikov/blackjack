namespace Blackjack.App.Interactivity
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Threading;
    using WpfWindow = System.Windows.Window;

    public static class Window
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(Window),
                new UIPropertyMetadata(null, HandleDialogResultPropertyChanged));

        public static readonly DependencyProperty AutoCloseProperty =
            DependencyProperty.RegisterAttached("AutoClose", typeof(bool?), typeof(Window),
                new UIPropertyMetadata(null, HandleAutoClosePropertyChanged));

        public static bool? GetDialogResult(DependencyObject obj)
        {
            return (bool?)obj.GetValue(DialogResultProperty);
        }

        public static void SetDialogResult(DependencyObject obj, bool? value)
        {
            obj.SetValue(DialogResultProperty, value);
        }

        public static bool? GetAutoClose(DependencyObject obj)
        {
            return (bool?)obj.GetValue(AutoCloseProperty);
        }

        public static void SetAutoClose(DependencyObject obj, bool? value)
        {
            obj.SetValue(AutoCloseProperty, value);
        }

        private static void HandleDialogResultPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var window = dependencyObject as WpfWindow;
            if (window != null && window.IsVisible)
            {
                window.DialogResult = (bool?)e.NewValue;
            }
        }

        private static void HandleAutoClosePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var window = dependencyObject as WpfWindow;
            if (window != null)
            {
                if ((e.NewValue != null) && (e.OldValue == null))
                {
                    window.PreviewKeyDown += HandleWindowKeyDown;
                    window.AddHandler(Button.ClickEvent, new RoutedEventHandler(HandleWindowButtonClick));
                }
                else if ((e.NewValue == null) && (e.OldValue != null))
                {
                    window.RemoveHandler(Button.ClickEvent, new RoutedEventHandler(HandleWindowButtonClick));
                    window.PreviewKeyDown -= HandleWindowKeyDown;
                }
            }
        }

        private static void HandleWindowKeyDown(object sender, KeyEventArgs e)
        {
            var window = (WpfWindow)sender;
            if ((GetAutoClose(window) ?? false) && e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                window.Close();
            }
        }

        private static void HandleWindowButtonClick(object sender, RoutedEventArgs e)
        {
            var window = (WpfWindow)sender;
            if (GetAutoClose(window) ?? false)
            {
                var button = e.Source as Button;
                if (button != null && button.IsCancel)
                {
                    window.Close();
                }
            }
        }

        private static void CloseWindow(WpfWindow window)
        {
            var dialogResult = GetDialogResult(window);
            if (dialogResult.HasValue)
            {
                window.DialogResult = dialogResult;
            }
            else
            {
                window.Close();
            }
        }
    }
}