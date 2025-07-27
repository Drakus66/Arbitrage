namespace Arbitrage.SharedModels;

/// <summary>
/// Represents a standardized ticker symbol across different exchanges
/// </summary>
public class TickerSymbol
{
    /// <summary>
    /// The base asset (e.g., BTC in BTC/USDT)
    /// </summary>
    public string BaseAsset { get; set; } = string.Empty;
    
    /// <summary>
    /// The quote asset (e.g., USDT in BTC/USDT)
    /// </summary>
    public string QuoteAsset { get; set; } = string.Empty;
    
    /// <summary>
    /// The exchange-specific symbol name (may vary between exchanges)
    /// </summary>
    public string ExchangeSymbol { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the exchange this symbol belongs to
    /// </summary>
    public string Exchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum order quantity for the base asset
    /// </summary>
    public decimal MinimumQuantity { get; set; }
    
    /// <summary>
    /// Minimum order value in the quote asset
    /// </summary>
    public decimal MinimumOrderValue { get; set; }
    
    /// <summary>
    /// Tick size - the minimum volume change allowed (e.g., 0.00001)
    /// </summary>
    public decimal TickSize { get; set; }
    
    /// <summary>
    /// Price precision (number of decimal places)
    /// </summary>
    public int PricePrecision { get; set; }
    
    /// <summary>
    /// Whether trading is currently allowed for this symbol
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Returns a standardized symbol format (BASE/QUOTE)
    /// </summary>
    public string StandardizedSymbol => $"{BaseAsset}/{QuoteAsset}";
    
    /// <summary>
    /// Returns a unique identifier for this symbol across exchanges
    /// </summary>
    public string UniqueId => $"{Exchange}:{ExchangeSymbol}";
    
    public override string ToString()
    {
        return $"{Exchange}:{BaseAsset}/{QuoteAsset}";
    }
}
