namespace Arbitrage.Database;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Arbitrage.SharedModels;

/// <summary>
/// LiteDB implementation of the arbitrage database
/// </summary>
public class LiteDbArbitrageDatabase : IArbitrageDatabase
{
    private readonly string _dbPath;
    private readonly ILogger<LiteDbArbitrageDatabase> _logger;

    public LiteDbArbitrageDatabase(IOptions<LiteDbOptions> options, ILogger<LiteDbArbitrageDatabase> logger)
    {
        _dbPath = options.Value.DatabasePath;
        _logger = logger;

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ClearDataBase();

        _logger.LogInformation("LiteDB database initialized at {DbPath}", _dbPath);
    }

    private void ClearDataBase()
    {
        var db = GetDatabase();
        var collections  = db.GetCollectionNames();
        foreach (var collectionName in collections)
        {
            var collection = db.GetCollection(collectionName);
            collection.DeleteAll();
        }
    }

    private LiteDatabase GetDatabase()
    {
        return new (_dbPath);
    }
    
    #region Symbol Operations
    
    public void SaveSymbols(List<Symbol> symbols)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<Symbol>("symbols");
            
            // Ensure index on important fields for faster querying
            collection.EnsureIndex(x => x.Exchange);
            collection.EnsureIndex(x => x.Code);
            
            // Upsert (insert or update) the symbols
            foreach (var symbol in symbols)
            {
                collection.Upsert(symbol);
            }
            
            _logger.LogInformation("Saved {Count} symbols to the database", symbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving symbols to the database");
            throw;
        }
    }
    
    public List<Symbol> GetAllSymbols()
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<Symbol>("symbols");
            return collection.FindAll().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbols from the database");
            return new List<Symbol>();
        }
    }
    
    public List<Symbol> GetSymbolsByExchange(string exchange)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<Symbol>("symbols");
            return collection.Find(x => x.Exchange == exchange).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbols for exchange {Exchange}", exchange);
            return new List<Symbol>();
        }
    }
    
    public List<Symbol> GetSymbolsByCode(string code)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<Symbol>("symbols");
            return collection.Find(x => x.Code == code).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbols for code {Code}", code);
            return new List<Symbol>();
        }
    }
    
    public Symbol? TryGetSymbol(string exchange, string code)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<Symbol>("symbols");
            return collection.FindOne(x => x.Exchange == exchange && x.Code == code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving symbol {Exchange}:{Code}", exchange, code);
            return null;
        }
    }
    
    #endregion
    
    #region TickerSymbol Operations
    
    public void SaveTickerSymbols(List<TickerSymbol> tickerSymbols)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<TickerSymbol>("tickerSymbols");
            
            // Ensure index on important fields for faster querying
            collection.EnsureIndex(x => x.Exchange);
            collection.EnsureIndex(x => x.BaseAsset);
            collection.EnsureIndex(x => x.QuoteAsset);
            
            // Upsert (insert or update) the ticker symbols
            foreach (var symbol in tickerSymbols)
            {
                collection.Upsert(symbol);
            }
            
            _logger.LogInformation("Saved {Count} ticker symbols to the database", tickerSymbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving ticker symbols to the database");
            throw;
        }
    }

    public void UpdateTickerSymbols(List<TickerSymbol> tickerSymbols)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<TickerSymbol>("tickerSymbols");

            // Upsert (insert or update) the ticker symbols
            foreach (var symbol in tickerSymbols)
            {
                var dbSymbol = collection.FindOne(x =>
                    x.Exchange == symbol.Exchange &&
                    x.BaseAsset == symbol.BaseAsset &&
                    x.QuoteAsset == symbol.QuoteAsset);

                dbSymbol.LastPrice = symbol.LastPrice;
                var updated =collection.Update(dbSymbol);
                if (!updated)
                {
                    _logger.LogError("Failed to update ticker symbol {Exchange}:{BaseAsset}/{QuoteAsset}", symbol.Exchange, symbol.BaseAsset, symbol.QuoteAsset);
                }
            }

            _logger.LogInformation("Updated {Count} ticker symbols to the database", tickerSymbols.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving ticker symbols to the database");
            throw;
        }
    }


    
    public List<TickerSymbol> GetAllTickerSymbols()
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<TickerSymbol>("tickerSymbols");
            return collection.FindAll().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticker symbols from the database");
            return new List<TickerSymbol>();
        }
    }
    
    public List<TickerSymbol> GetTickerSymbolsByExchange(string exchange)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<TickerSymbol>("tickerSymbols");
            return collection.Find(x => x.Exchange == exchange).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticker symbols for exchange {Exchange}", exchange);
            return new List<TickerSymbol>();
        }
    }
    
    public TickerSymbol GetTickerSymbol(string exchange, string baseAsset, string quoteAsset)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<TickerSymbol>("tickerSymbols");
            return collection.FindOne(x => x.Exchange == exchange && x.BaseAsset == baseAsset && x.QuoteAsset == quoteAsset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticker symbol {Exchange}:{BaseAsset}/{QuoteAsset}", exchange, baseAsset, quoteAsset);
            return null;
        }
    }
    
    #endregion
    
    #region OrderBook Operations
    
    public void SaveOrderBook(OrderBook orderBook)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<OrderBook>("orderBooks");
            
            // Ensure index on important fields for faster querying
            collection.EnsureIndex(x => x.Symbol.Exchange);
            collection.EnsureIndex(x => x.Symbol.BaseAsset);
            collection.EnsureIndex(x => x.Symbol.QuoteAsset);
            collection.EnsureIndex(x => x.Timestamp);
            
            // Insert the order book
            collection.Insert(orderBook);
            
            _logger.LogInformation("Saved order book for {Symbol} at {Timestamp}", orderBook.Symbol.ExchangeSymbol, orderBook.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving order book to the database");
            throw;
        }
    }
    
    public OrderBook GetLatestOrderBook(string exchange, string baseAsset, string quoteAsset)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<OrderBook>("orderBooks");
            return collection
                .Find(x => x.Symbol.Exchange == exchange && x.Symbol.BaseAsset == baseAsset && x.Symbol.QuoteAsset == quoteAsset)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest order book for {Exchange}:{BaseAsset}/{QuoteAsset}", exchange, baseAsset, quoteAsset);
            return null;
        }
    }
    
    public List<OrderBook> GetOrderBookHistory(string exchange, string baseAsset, string quoteAsset, DateTime from, DateTime to)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<OrderBook>("orderBooks");
            return collection
                .Find(x => x.Symbol.Exchange == exchange && 
                      x.Symbol.BaseAsset == baseAsset && 
                      x.Symbol.QuoteAsset == quoteAsset && 
                      x.Timestamp >= from && 
                      x.Timestamp <= to)
                .OrderBy(x => x.Timestamp)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order book history for {Exchange}:{BaseAsset}/{QuoteAsset} from {From} to {To}", 
                exchange, baseAsset, quoteAsset, from, to);
            return new List<OrderBook>();
        }
    }
    
    #endregion
    
    #region Market Snapshot Operations
    
    public void SaveMarketSnapshot(DateTime timestamp, Dictionary<string, Dictionary<string, OrderBook>> marketSnapshot)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<BsonDocument>("marketSnapshots");
            
            // Create a document with timestamp as the ID
            var doc = new BsonDocument();
            doc["_id"] = timestamp.Ticks;
            doc["Timestamp"] = timestamp;
            
            // Serialize the market snapshot data
            foreach (var exchangePair in marketSnapshot)
            {
                var exchange = exchangePair.Key;
                var symbolDict = exchangePair.Value;
                
                var exchangeDoc = new BsonDocument();
                foreach (var symbolPair in symbolDict)
                {
                    var symbol = symbolPair.Key;
                    var orderBook = symbolPair.Value;
                    
                    // Store a reference to the order book in the OrderBooks collection
                    var orderBooksCollection = db.GetCollection<OrderBook>("orderBooks");
                    orderBooksCollection.Upsert(orderBook);
                    
                    // Add the reference to the market snapshot
                    exchangeDoc[symbol] = $"{orderBook.Symbol.Exchange}_{orderBook.Symbol.BaseAsset}_{orderBook.Symbol.QuoteAsset}_{orderBook.Timestamp.Ticks}";
                }
                
                doc[exchange] = exchangeDoc;
            }
            
            collection.Upsert(doc);
            
            _logger.LogInformation("Saved market snapshot at {Timestamp}", timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving market snapshot to the database");
            throw;
        }
    }
    
    public Dictionary<string, Dictionary<string, OrderBook>> GetMarketSnapshot(DateTime timestamp)
    {
        try
        {
            using var db = GetDatabase();
            var snapshotsCollection = db.GetCollection<BsonDocument>("marketSnapshots");
            var orderBooksCollection = db.GetCollection<OrderBook>("orderBooks");
            
            // Find the snapshot with the given timestamp
            var snapshot = snapshotsCollection.FindById(timestamp.Ticks);
            if (snapshot == null)
            {
                _logger.LogWarning("No market snapshot found at {Timestamp}", timestamp);
                return new Dictionary<string, Dictionary<string, OrderBook>>();
            }
            
            // Deserialize the market snapshot data
            var result = new Dictionary<string, Dictionary<string, OrderBook>>();
            
            foreach (var key in snapshot.Keys.Where(k => k != "_id" && k != "Timestamp"))
            {
                var exchange = key;
                var exchangeData = snapshot[exchange].AsDocument;
                var symbolDict = new Dictionary<string, OrderBook>();
                
                foreach (var symbolKey in exchangeData.Keys)
                {
                    var symbol = symbolKey;
                    var orderBookId = exchangeData[symbol].AsString;
                    var orderBook = orderBooksCollection.FindById(orderBookId);
                    
                    if (orderBook != null)
                    {
                        symbolDict[symbol] = orderBook;
                    }
                }
                
                if (symbolDict.Count > 0)
                {
                    result[exchange] = symbolDict;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving market snapshot at {Timestamp}", timestamp);
            return new Dictionary<string, Dictionary<string, OrderBook>>();
        }
    }
    
    public List<DateTime> GetAvailableMarketSnapshots(DateTime from, DateTime to)
    {
        try
        {
            using var db = GetDatabase();
            var collection = db.GetCollection<BsonDocument>("marketSnapshots");
            
            // Find all snapshots between the given dates
            var snapshots = collection.Find(Query.And(
                Query.GTE("Timestamp", from),
                Query.LTE("Timestamp", to)
            )).ToList();
            
            return snapshots.Select(s => s["Timestamp"].AsDateTime).OrderBy(t => t).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available market snapshots from {From} to {To}", from, to);
            return new List<DateTime>();
        }
    }
    
    #endregion
}
