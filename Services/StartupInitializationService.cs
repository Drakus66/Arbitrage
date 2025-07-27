namespace Arbitrage.Services;

using Arbitrage.Database;
using Arbitrage.ExchangeConnectors;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class StartupInitializationService : IHostedService
{
    private readonly ILogger<StartupInitializationService> _logger;
    private readonly IArbitrageDatabase _database;
    private readonly IEnumerable<IExchange> _exchanges;
    private readonly InitializationCompletionService _initializationCompletionService;

    public StartupInitializationService(
        ILogger<StartupInitializationService> logger,
        IArbitrageDatabase database,
        IEnumerable<IExchange> exchanges,
        InitializationCompletionService initializationCompletionService)
    {
        _logger = logger;
        _database = database;
        _exchanges = exchanges;
        _initializationCompletionService = initializationCompletionService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing database with exchange data...");
        await InitializeDatabaseAsync(cancellationToken);
        _logger.LogInformation("Database initialization complete.");

        // Signal that initialization is complete
        _initializationCompletionService.SignalInitializationCompleted();
        _logger.LogInformation("Signaled completion of initialization to dependent services");

        return;
    }

    /// <summary>
    /// Initialize the database with information from all registered exchange connectors
    /// </summary>
    private async Task InitializeDatabaseAsync(CancellationToken stoppingToken)
    {
        foreach (var exchange in _exchanges)
        {
            try
            {
                _logger.LogInformation("Initializing data for exchange: {Exchange}", exchange.ExchangeName);

                // Get and store trading pairs (ticker symbols)
                await InitializeTickerSymbolsAsync(exchange, stoppingToken);

                // Get and store currency network information
                await InitializeCurrenciesAsync(exchange, stoppingToken);

                _logger.LogInformation("Completed initialization for exchange: {Exchange}", exchange.ExchangeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing data for exchange: {Exchange}", exchange.ExchangeName);
            }
        }
    }

    /// <summary>
    /// Get and store ticker symbols (trading pairs) from an exchange
    /// </summary>
    private async Task InitializeTickerSymbolsAsync(IExchange exchange, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Getting pairs for {Exchange}...", exchange.ExchangeName);
            var tickerSymbols = await exchange.GetSymbolsAsync();

            _logger.LogInformation("Retrieved {Count} pairs from {Exchange}", tickerSymbols.Count, exchange.ExchangeName);

            // Store in database
            _database.SaveTickerSymbols(tickerSymbols);

            _logger.LogInformation("Successfully stored pairs for {Exchange} in database", exchange.ExchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticker pairs from {Exchange}", exchange.ExchangeName);
        }
    }

    /// <summary>
    /// Get and store currency network information from an exchange
    /// </summary>
    private async Task InitializeCurrenciesAsync(IExchange exchange, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Getting currency information for {Exchange}...", exchange.ExchangeName);
            var symbols = await exchange.GetCurrenciesAsync();

            _logger.LogInformation("Retrieved {Count} currencies entries from {Exchange}", symbols.Count, exchange.ExchangeName);

            // Store in database
            _database.SaveSymbols(symbols);

            _logger.LogInformation("Successfully stored currencies information for {Exchange} in database", exchange.ExchangeName);

            // Log some stats about networks
            var currenciesWithMultipleNetworks = symbols
                .GroupBy(s => s.Code)
                .Where(g => g.Count() > 1)
                .Select(g => new { Currency = g.Key, NetworkCount = g.Count() })
                .ToList();

            if (currenciesWithMultipleNetworks.Any())
            {
                foreach (var currency in currenciesWithMultipleNetworks.Take(5))
                {
                    _logger.LogInformation("Currency {Currency} has {NetworkCount} networks on {Exchange}",
                        currency.Currency, currency.NetworkCount, exchange.ExchangeName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving currency information from {Exchange}", exchange.ExchangeName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
