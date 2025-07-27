namespace Arbitrage.Database;

using Arbitrage.SharedModels;

/// <summary>
/// Interface for arbitrage database operations
/// </summary>
public interface IArbitrageDatabase
{
    // Symbol operations
    void SaveSymbols(List<Symbol> symbols);
    List<Symbol> GetAllSymbols();
    List<Symbol> GetSymbolsByExchange(string exchange);
    List<Symbol> GetSymbolsByCode(string code);
    Symbol GetSymbol(string exchange, string code, SymbolNetwork network);
    
    // TickerSymbol operations
    void SaveTickerSymbols(List<TickerSymbol> tickerSymbols);
    List<TickerSymbol> GetAllTickerSymbols();
    List<TickerSymbol> GetTickerSymbolsByExchange(string exchange);
    TickerSymbol GetTickerSymbol(string exchange, string baseAsset, string quoteAsset);
    
    // OrderBook operations
    void SaveOrderBook(OrderBook orderBook);
    OrderBook GetLatestOrderBook(string exchange, string baseAsset, string quoteAsset);
    List<OrderBook> GetOrderBookHistory(string exchange, string baseAsset, string quoteAsset, DateTime from, DateTime to);
    
    // Market data operations
    void SaveMarketSnapshot(DateTime timestamp, Dictionary<string, Dictionary<string, OrderBook>> marketSnapshot);
    Dictionary<string, Dictionary<string, OrderBook>> GetMarketSnapshot(DateTime timestamp);
    List<DateTime> GetAvailableMarketSnapshots(DateTime from, DateTime to);
}
