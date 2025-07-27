namespace Arbitrage.SharedModels;

using System.Collections.Generic;

/// <summary>
/// Represents a standardized order book across different exchanges
/// </summary>
public class OrderBook
{
    /// <summary>
    /// The ticker symbol this order book belongs to
    /// </summary>
    public TickerSymbol Symbol { get; set; } = null!;
    
    /// <summary>
    /// Timestamp when this order book was captured (in UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// List of ask orders (sell orders) sorted by price ascending
    /// </summary>
    public List<OrderBookEntry> Asks { get; set; } = new List<OrderBookEntry>();
    
    /// <summary>
    /// List of bid orders (buy orders) sorted by price descending
    /// </summary>
    public List<OrderBookEntry> Bids { get; set; } = new List<OrderBookEntry>();
    
    /// <summary>
    /// The exchange-specific update ID or sequence number
    /// </summary>
    public long UpdateId { get; set; }
    
    /// <summary>
    /// Gets the best ask price (lowest sell price)
    /// </summary>
    public decimal BestAskPrice => Asks.Count > 0 ? Asks[0].Price : 0;
    
    /// <summary>
    /// Gets the best bid price (highest buy price)
    /// </summary>
    public decimal BestBidPrice => Bids.Count > 0 ? Bids[0].Price : 0;
    
    /// <summary>
    /// Gets the spread between best ask and best bid
    /// </summary>
    public decimal Spread => BestAskPrice - BestBidPrice;
    
    /// <summary>
    /// Gets the spread percentage
    /// </summary>
    public decimal SpreadPercentage => BestBidPrice > 0 ? (Spread / BestBidPrice) * 100 : 0;
    
    /// <summary>
    /// Gets the total ask volume up to a certain price level
    /// </summary>
    public decimal GetAskVolume(decimal priceLevel)
    {
        return Asks.Where(a => a.Price <= priceLevel).Sum(a => a.Quantity);
    }
    
    /// <summary>
    /// Gets the total bid volume down to a certain price level
    /// </summary>
    public decimal GetBidVolume(decimal priceLevel)
    {
        return Bids.Where(b => b.Price >= priceLevel).Sum(b => b.Quantity);
    }
}
