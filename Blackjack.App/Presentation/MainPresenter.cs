namespace Blackjack.App.Presentation
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Relay.PresentationModel;

    public class MainPresenter : Presenter
    {
        private Player player1;
        private Player player2;

        public MainPresenter()
        {
            this.player1 = new AdaptivePlayer();
            this.player2 = Player.Basic;
        }

        public Player Player1
        {
            get => this.player1;
            private set => Set(ref this.player1, value);
        }

        public Player Player2
        {
            get => this.player2;
            private set => Set(ref this.player2, value);
        }

        public ICommand RunCommand => GetCommand(RunAsync);

        private async Task RunAsync()
        {
            using (WithStatus("Playing"))
            {
                await Parallel.ForAsync(0, 100000, (playNumber, cancellationToken) =>
                {
                    var dealer = new Dealer();
                    dealer.Play(this.player1.Play());

                    RaisePropertyChanged(nameof(this.Player1));
                    return ValueTask.CompletedTask;
                });
            }
        }
    }
}