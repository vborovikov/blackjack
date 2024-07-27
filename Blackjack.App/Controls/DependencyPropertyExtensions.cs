namespace Blackjack.App.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;

    internal static class DependencyPropertyExtensions
    {
        public static DependencyPropertyChangedEventArgs ChangedEventArgs(this DependencyProperty dependencyProperty, object newValue)
        {
            return new DependencyPropertyChangedEventArgs(dependencyProperty,
                dependencyProperty.DefaultMetadata.DefaultValue, newValue);
        }
    }
}