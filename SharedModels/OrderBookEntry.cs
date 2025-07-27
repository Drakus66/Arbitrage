namespace Arbitrage.SharedModels;

/// <summary>
/// Represents a single entry in an order book (bid or ask)
/// </summary>
public class OrderBookEntry
{
    /// <summary>
    /// The price level of this order
    /// </summary>
    public decimal Price { get; set; }
    
    /// <summary>
    /// The quantity available at this price level
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// The total value of this order (Price * Quantity)
    /// </summary>
    public decimal Value => Price * Quantity;
    
    public OrderBookEntry() { }
    
    public OrderBookEntry(decimal price, decimal quantity)
    {
        Price = price;
        Quantity = quantity;
    }
    
    public override string ToString()
    {
        return $"{Price} x {Quantity}";
    }
}
