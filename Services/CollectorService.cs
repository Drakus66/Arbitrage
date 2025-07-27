namespace Arbitrage.Services;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Arbitrage.Database;
using Arbitrage.ExchangeConnectors;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class CollectorService : BackgroundService
{
    private readonly ILogger<CollectorService> _logger;
    private readonly IArbitrageDatabase _database;
    private readonly IEnumerable<IExchange> _exchanges;
    private readonly InitializationCompletionService _initializationCompletionService;
    
    public CollectorService(
        ILogger<CollectorService> logger,
        IArbitrageDatabase database,
        IEnumerable<IExchange> exchanges,
        InitializationCompletionService initializationCompletionService)
    {
        _logger = logger;
        _database = database;
        _exchanges = exchanges;
        _initializationCompletionService = initializationCompletionService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for initialization to complete before starting
        _logger.LogInformation("Collector service is waiting for initialization to complete...");
        await _initializationCompletionService.InitializationCompleted;
        _logger.LogInformation("Initialization completed. Starting collector service operations.");
        
        // Regular data collection cycle
        while (!stoppingToken.IsCancellationRequested)
        {
            // This is where we would put ongoing data collection logic
            // For now we just log that the service is running
            _logger.LogInformation("Collector service running at: {time}", DateTimeOffset.Now);
            await Task.Delay(10000, stoppingToken); // Check every 10 seconds instead of every second
        }
    }
}
