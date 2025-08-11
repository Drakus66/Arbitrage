namespace Arbitrage.Services;

using Arbitrage.Database;
using Arbitrage.SharedModels;
using Arbitrage.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

public sealed class FindArbitrageService
{
    private readonly ILogger<FindArbitrageService> _logger;
    private readonly IArbitrageDatabase _database;
    private readonly IMemoryCache _memoryCache;
    private readonly ArbitrageOpportunityManager _opportunityManager;
    
    // Ключ для хранения всех символов в кэше
    private const string AllSymbolsCacheKey = "AllTickerSymbols";
    
    // Время жизни кэша в секундах
    private const int CacheDurationSeconds = 5;
    
    // Минимальная разница в процентах для определения арбитражной возможности
    private const decimal MinArbitragePercentDifference = 0.5m;

    public FindArbitrageService(
        ILogger<FindArbitrageService> logger,
        IArbitrageDatabase database,
        IMemoryCache memoryCache,
        ArbitrageOpportunityManager opportunityManager)
    {
        _logger = logger;
        _database = database;
        _memoryCache = memoryCache;
        _opportunityManager = opportunityManager;
    }

    public Task FindArbitrageOpportunities(CancellationToken stoppingToken)
    {
        // Получаем тикеры из кэша или из базы данных с сохранением в кэш
        /* var allSymbols = GetCachedTickerSymbols(); */
        var allSymbols = _database.GetAllTickerSymbols();
        
        // Создаем словарь для унифицированных имен символов для избежания повторных вычислений
        var symbolNameMapping = new Dictionary<string, string>();
        
        // Оптимизированная группировка с предварительным вычислением унифицированных имен
        var groupedSymbols = allSymbols
            .Select(s => {
                // Получаем или вычисляем унифицированное имя
                if (!symbolNameMapping.TryGetValue(s.ExchangeSymbol, out var unifiedName))
                {
                    unifiedName = UnifyUsdSymbolName(s.ExchangeSymbol);
                    symbolNameMapping[s.ExchangeSymbol] = unifiedName;
                }
                return new { Symbol = s, UnifiedName = unifiedName };
            })
            .GroupBy(x => x.UnifiedName)
            .Where(g => g.Count() > 1) // Более эффективный способ проверки, чем g.Skip(1).Any()
            .ToList(); // Материализуем только после фильтрации
            
        foreach (var group in groupedSymbols)
        {
            var symbolGroup = group.Select(x => x.Symbol);
            var symbolsArray = symbolGroup.ToArray(); // Материализуем один раз
            var minPriceTicker = symbolsArray.MinBy(s => s.LastPrice);
            var maxPriceTicker = symbolsArray.MaxBy(s => s.LastPrice);

            if (minPriceTicker == null || maxPriceTicker == null ||
                minPriceTicker.LastPrice.IsEquals(0) || maxPriceTicker.LastPrice.IsEquals(0))
            {
                continue;
            }

            var percentDifference = (maxPriceTicker.LastPrice - minPriceTicker.LastPrice) / minPriceTicker.LastPrice * 100;

            if (percentDifference > MinArbitragePercentDifference)
            {
                // Создаем и сохраняем объект арбитражной возможности
                var opportunity = new ArbitrageOpportunity
                {
                    UnifiedSymbolName = group.Key, // Используем ключ группы (унифицированное имя)
                    MinPriceSymbol = minPriceTicker.ExchangeSymbol,
                    MinPriceExchange = minPriceTicker.Exchange,
                    MinPrice = minPriceTicker.LastPrice,
                    MaxPriceSymbol = maxPriceTicker.ExchangeSymbol,
                    MaxPriceExchange = maxPriceTicker.Exchange,
                    MaxPrice = maxPriceTicker.LastPrice,
                    PriceDifferencePercent = percentDifference,
                    DetectedAt = DateTimeOffset.Now,
                    LastUpdatedAt = DateTimeOffset.Now
                };
                
                // Добавляем или обновляем в менеджере возможностей
                var isNew = _opportunityManager.AddOrUpdate(opportunity);
                
                if (isNew)
                {
                    _logger.LogInformation("Новая арбитражная возможность: {Symbol1}/{Symbol2} " +
                                           "(min: {MinPrice} {ExchangeMin}, max: {MaxPrice} {ExchangeMax} " +
                                           "Diff {Diff:F2}%)",
                        minPriceTicker.ExchangeSymbol, maxPriceTicker.ExchangeSymbol,
                        minPriceTicker.LastPrice, minPriceTicker.Exchange,
                        maxPriceTicker.LastPrice, maxPriceTicker.Exchange,
                        percentDifference);
                }
                else
                {
                    _logger.LogDebug("Обновлена существующая арбитражная возможность: {Symbol1}/{Symbol2} " +
                                    "(min: {MinPrice} {ExchangeMin}, max: {MaxPrice} {ExchangeMax} " +
                                    "Diff {Diff:F2}%)",
                        minPriceTicker.ExchangeSymbol, maxPriceTicker.ExchangeSymbol,
                        minPriceTicker.LastPrice, minPriceTicker.Exchange,
                        maxPriceTicker.LastPrice, maxPriceTicker.Exchange,
                        percentDifference);
                }
            }
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Получает символы тикеров из кэша или из базы данных с сохранением в кэш
    /// </summary>
    private IReadOnlyCollection<TickerSymbol> GetCachedTickerSymbols()
    {
        return _memoryCache.GetOrCreate(AllSymbolsCacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheDurationSeconds);
            _logger.LogDebug("Cache miss for ticker symbols, loading from database");
            var symbols = _database.GetAllTickerSymbols();
            _logger.LogDebug("Loaded {Count} ticker symbols from database", symbols.Count);
            return symbols;
        })!;
    }

    /// <summary>
    /// Унифицирует имя символа, удаляя суффиксы стейблкоинов
    /// </summary>
    private static string UnifyUsdSymbolName(string symbol)
    {
        // Исправляем ошибку с удалением только одного символа
        if (symbol.EndsWith("USDT", StringComparison.OrdinalIgnoreCase))
        {
            return symbol[..^1].Replace("-", ""); // Удаляем все 4 символа USDT
        }
        
        if (symbol.EndsWith("USDC", StringComparison.OrdinalIgnoreCase))
        {
            return symbol[..^1].Replace("-", "");
        }

        return symbol.Replace("-", "");
    }
}
