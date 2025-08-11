namespace Arbitrage.SharedModels;

/// <summary>
/// Представляет возможность арбитража между двумя биржами
/// </summary>
public class ArbitrageOpportunity
{
    /// <summary>
    /// Унифицированное название базового актива (без суффикса стейблкоина)
    /// </summary>
    public string UnifiedSymbolName { get; set; } = string.Empty;
    
    /// <summary>
    /// Символ на бирже с минимальной ценой
    /// </summary>
    public string MinPriceSymbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Название биржи с минимальной ценой
    /// </summary>
    public string MinPriceExchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Минимальная цена актива
    /// </summary>
    public decimal MinPrice { get; set; }
    
    /// <summary>
    /// Символ на бирже с максимальной ценой
    /// </summary>
    public string MaxPriceSymbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Название биржи с максимальной ценой
    /// </summary>
    public string MaxPriceExchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Максимальная цена актива
    /// </summary>
    public decimal MaxPrice { get; set; }
    
    /// <summary>
    /// Разница в процентах между максимальной и минимальной ценой
    /// </summary>
    public decimal PriceDifferencePercent { get; set; }
    
    /// <summary>
    /// Время обнаружения возможности
    /// </summary>
    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.Now;
    
    /// <summary>
    /// Время последнего обновления возможности
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.Now;
    
    /// <summary>
    /// Создает уникальный ключ для этой арбитражной возможности
    /// </summary>
    public string GetUniqueKey() => $"{MinPriceExchange}_{MaxPriceExchange}_{UnifiedSymbolName}";
}
