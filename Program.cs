namespace Arbitrage;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Arbitrage.Database;
using Arbitrage.ExchangeConnectors;
using Arbitrage.ExchangeConnectors.Settings;
using Arbitrage.Services;

using Microsoft.Extensions.Configuration;

public static class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();

        IHostBuilder builder = Host.CreateDefaultBuilder(args)
            .SetupHost()
            .SetupServices();

        var app = builder.Build();

        app.Run();
    }

    private static IHostBuilder SetupHost(this IHostBuilder builder)
    {
        builder.UseSerilog(
            new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger()
        );

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var exchangeConfigPath = Path.Combine(baseDir, "ExchangeConnectors", "Settings", "ExchangesSettings.json");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(exchangeConfigPath, optional: false, reloadOnChange: true);
        });

        builder.ConfigureServices((context, services) =>
        {
            services.Configure<ExchangeConnectionSettings>(context.Configuration.GetSection("ConnectionStrings"));
        });

        builder
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices(services =>
            {
                // Register initialization coordination service as singleton
                services.AddSingleton<InitializationCompletionService>();
                
                // Register hosted services in the order they should start
                services.AddHostedService<StartupInitializationService>();
                services.AddHostedService<CollectorService>();

                // Configure LiteDB options
                services.Configure<LiteDbOptions>(options =>
                {
                    options.DatabasePath = "Data/arbitrage.db";
                });
            });

        return builder;
    }

    private static IHostBuilder SetupServices(this IHostBuilder builder)
    {
        builder.ConfigureContainer<ContainerBuilder>(
            b =>
            {
                // Register exchange connectors
                b.RegisterType<BinanceConnector>().As<IExchange>();
                
                // Register database services
                b.RegisterType<LiteDbArbitrageDatabase>().As<IArbitrageDatabase>().SingleInstance();
            });
        return builder;
    }
}
