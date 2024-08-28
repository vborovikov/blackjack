namespace Blackjack.App;

using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.PresentationModel;

public record AppOptions
{
    public const string Name = "Blackjack";

    public int DealerBank { get; init; } = 50000;
    public int HandBank { get; init; } = 1000;
    public int Bet { get; init; } = 250;
}

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

    private readonly ServiceProvider serviceProvider;

    public App()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        this.serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Presenter.RegisterCommandManager(new WpfCommandManager());
        this.Resources["AppOptions"] = this.serviceProvider.GetRequiredService<IOptions<AppOptions>>();
        this.Resources["LoggerFactory"] = this.serviceProvider.GetRequiredService<ILoggerFactory>();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        this.serviceProvider.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(ServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.Name))
            .Configure(options =>
            {
            });

        services.AddLogging(logging =>
        {
            logging.AddConfiguration(configuration.GetSection("Logging"));
#if DEBUG
            logging.AddDebug();
#endif
        });
    }
}
