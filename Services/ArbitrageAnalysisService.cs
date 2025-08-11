namespace Arbitrage.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using Arbitrage.SharedModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Сервис для анализа арбитражных возможностей и принятия торговых решений
/// </summary>
public class ArbitrageAnalysisService : BackgroundService
{
    private readonly ILogger<ArbitrageAnalysisService> _logger;
    private readonly ArbitrageOpportunityManager _opportunityManager;
    private readonly InitializationCompletionService _initializationCompletionService;
    
    // Интервал между проверками возможностей (в миллисекундах)
    private const int AnalysisIntervalMs = 2000;
    
    // Минимальная разница в процентах для принятия торгового решения
    private const decimal MinProfitablePercentDifference = 0.7m;

    public ArbitrageAnalysisService(
        ILogger<ArbitrageAnalysisService> logger,
        ArbitrageOpportunityManager opportunityManager,
        InitializationCompletionService initializationCompletionService)
    {
        _logger = logger;
        _opportunityManager = opportunityManager;
        _initializationCompletionService = initializationCompletionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ожидаем завершения инициализации системы
        _logger.LogInformation("Arbitrage analysis service is waiting for initialization to complete...");
        await _initializationCompletionService.InitializationCompleted;
        _logger.LogInformation("Initialization completed. Starting arbitrage analysis operations.");
        
        // Основной цикл анализа
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await AnalyzeArbitrageOpportunities(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during arbitrage analysis");
            }
            
            // Ждем указанное время перед следующим анализом
            await Task.Delay(AnalysisIntervalMs, stoppingToken);
        }
    }
    
    /// <summary>
    /// Анализирует текущие арбитражные возможности и принимает торговые решения
    /// </summary>
    private Task AnalyzeArbitrageOpportunities(CancellationToken stoppingToken)
    {
        var opportunities = _opportunityManager.GetAll();
        
        if (opportunities.Count == 0)
        {
            return Task.CompletedTask;
        }
        
        _logger.LogDebug("Analyzing {Count} arbitrage opportunities", opportunities.Count);
        
        foreach (var opportunity in opportunities)
        {
            // Проверяем, достаточно ли выгодна возможность для входа в сделку
            if (IsOpportunityProfitable(opportunity))
            {
                // Заглушка: Имитация принятия торгового решения
                _logger.LogInformation(
                    "DECISION: Would execute trade for {Symbol} between {Exchange1} and {Exchange2} with {Profit:F2}% profit",
                    opportunity.UnifiedSymbolName,
                    opportunity.MinPriceExchange,
                    opportunity.MaxPriceExchange,
                    opportunity.PriceDifferencePercent);
                
                // Удаляем обработанную возможность
                _opportunityManager.Remove(opportunity);
            }

            // Удаляем устаревшие возможности
            if (_opportunityManager.Remove(opportunity))
            {
                _logger.LogDebug(
                    "Removed unprofitable opportunity for {Symbol} ({Exchange1}-{Exchange2})",
                    opportunity.UnifiedSymbolName,
                    opportunity.MinPriceExchange,
                    opportunity.MaxPriceExchange);
            }
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Заглушка: Проверяет, является ли арбитражная возможность прибыльной для входа в сделку
    /// </summary>
    /// <param name="opportunity">Арбитражная возможность для анализа</param>
    /// <returns>true если возможность прибыльна, иначе false</returns>
    private static bool IsOpportunityProfitable(ArbitrageOpportunity opportunity)
    {
        // Заглушка: В реальной реализации здесь будет сложная логика анализа,
        // включающая комиссии, глубину ордеров, волатильность и т.д.
        
        // Пока просто проверяем, что разница больше минимального порога
        return opportunity.PriceDifferencePercent >= MinProfitablePercentDifference;
    }
}
