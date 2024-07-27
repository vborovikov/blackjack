namespace Blackjack.App
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using Relay.PresentationModel;

    public partial class App : Application
    {
        private sealed class WpfCommandManager : ICommandManager
        {
            public event EventHandler RequerySuggested
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public void InvalidateRequerySuggested()
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Presenter.RegisterCommandManager(new WpfCommandManager());
        }
    }
}
