namespace Blackjack.App.Presentation
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Microsoft.Extensions.Logging;
    using Relay.PresentationModel;

    public class MainPresenter : Presenter
    {
        private readonly ILoggerFactory loggerFactory;
        private Player player1;
        private Player player2;

        public MainPresenter(ILoggerFactory loggerFactory)
        {
            this.player1 = new AdaptivePlayer();
            this.player2 = Player.Basic;
            this.loggerFactory = loggerFactory;
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
            var logger = this.loggerFactory.CreateLogger<Dealer>();
            using (WithStatus("Playing"))
            {
                await Parallel.ForAsync(0, 1000000, //new ParallelOptions { MaxDegreeOfParallelism = 1 },
                    (playNumber, cancellationToken) =>
                    {
                        var dealer = new Dealer(logger);
                        dealer.Play(this.player1.Play());

                        RaisePropertyChanged(nameof(this.Player1));
                        return ValueTask.CompletedTask;
                    });
            }
        }
    }
}