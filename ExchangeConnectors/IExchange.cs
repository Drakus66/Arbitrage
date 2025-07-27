namespace Arbitrage.ExchangeConnectors;

using Arbitrage.SharedModels;

public interface IExchange
{
    /// <summary>
    /// Gets all available trading symbols from the exchange
    /// </summary>
    /// <returns>A list of standardized ticker symbols</returns>
    public Task<List<TickerSymbol>> GetSymbolsAsync();
    
    /// <summary>
    /// Gets the current order book for a specific symbol
    /// </summary>
    /// <param name="symbol">The ticker symbol to get the order book for</param>
    /// <param name="depth">The depth of the order book (number of price levels)</param>
    /// <returns>A standardized order book</returns>
    public Task<OrderBook> GetOrderBookAsync(TickerSymbol symbol, int depth = 20);
    
    /// <summary>
    /// Gets information about all currencies supported by the exchange, including their networks
    /// </summary>
    /// <returns>A list of symbols with network information</returns>
    public Task<List<Symbol>> GetCurrenciesAsync();
    
    /// <summary>
    /// Gets the name of the exchange
    /// </summary>
    string ExchangeName { get; }
}
