namespace Arbitrage.ExchangeConnectors;

using Arbitrage.ExchangeConnectors.Settings;
using Arbitrage.SharedModels;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Spot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BinanceConnector : IExchange
{
    private readonly BinanceRestClient _restClient;
    private readonly ILogger<BinanceConnector> _logger;

    public string ExchangeName => "Binance";

    public BinanceConnector(ILogger<BinanceConnector> logger, IOptions<ExchangeConnectionSettings>? settings)
    {
        _logger = logger;
        var binanceSettings = settings?.Value.Binance;

        if (binanceSettings == null)
        {
            _logger.LogError("{Name} settings not configured. Using only public info", ExchangeName);
        }
        else
        {
            _logger.LogDebug("Initializing Binance connector with API key: {ApiKeyStart}...",
                !string.IsNullOrEmpty(binanceSettings.ApiKey) ? binanceSettings.ApiKey.Substring(0, 5) + "..." : "Not provided");

            BinanceRestClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new(binanceSettings.ApiKey, binanceSettings.SecretKey);
            });
            BinanceSocketClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new(binanceSettings.ApiKey, binanceSettings.SecretKey);
            });
        }

        // Create the client with options
        _restClient = new ();
        
        _logger.LogInformation("{Name} connector initialized", ExchangeName);
    }

    public async Task<List<TickerSymbol>> GetSymbolsAsync()
    {
        try
        {
            var exchangeInfo = await _restClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
            
            if (!exchangeInfo.Success)
            {
                _logger?.LogError("Failed to get exchange info: {Error}", exchangeInfo.Error?.Message);
                return [];
            }

            var exchangeSymbols = exchangeInfo.Data.Symbols;

            exchangeSymbols = await FilterSymbolsAsync(exchangeSymbols);

            return exchangeSymbols.Select(MapToTickerSymbol).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting symbols from Binance");
            return [];
        }
    }
    
    public async Task<OrderBook> GetOrderBookAsync(TickerSymbol symbol, int depth = 20)
    {
        try
        {
            var orderBookData = await _restClient.SpotApi.ExchangeData.GetOrderBookAsync(symbol.ExchangeSymbol, depth);
            
            if (!orderBookData.Success)
            {
                _logger?.LogError("Failed to get order book: {Error}", orderBookData.Error?.Message);
                return new OrderBook { Symbol = symbol };
            }
            
            var result = new OrderBook
            {
                Symbol = symbol,
                Timestamp = DateTime.UtcNow,
                UpdateId = orderBookData.Data.LastUpdateId,
                // Map asks (lowest price first)
                Asks = orderBookData.Data.Asks
                    .Select(a => new OrderBookEntry(a.Price, a.Quantity))
                    .ToList(),
                // Map bids (highest price first)
                Bids = orderBookData.Data.Bids
                    .Select(b => new OrderBookEntry(b.Price, b.Quantity))
                    .ToList()
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting order book from Binance for {Symbol}", symbol.ExchangeSymbol);
            return new OrderBook { Symbol = symbol };
        }
    }
    
    public async Task<List<Symbol>> GetCurrenciesAsync()
    {
        try
        {
            var userAssets = await _restClient.SpotApi.Account.GetUserAssetsAsync();

            if (!userAssets.Success)
            {
                _logger?.LogError("Failed to get coin information: {Error}", userAssets.Error?.Message);
                return [];
            }

            var coinsInfo = userAssets.Data;
            var result = new List<Symbol>();

            coinsInfo = coinsInfo.Where(c => c is { DepositAllEnable: true, WithdrawAllEnable: true }).ToArray();
            
            foreach (var coin in coinsInfo)
            {
                var networks = coin.NetworkList;
                var nets = networks.Select(n => new SymbolNetwork
                {
                    Name = n.Name,
                    ContractAddress = n.ContractAddress,
                    WithdrawMin = n.WithdrawMin,
                    WithdrawFee = n.WithdrawFee
                }).ToArray();

                result.Add(new Symbol
                {
                    Code = coin.Asset,
                    Exchange = ExchangeName,
                    Name = coin.Name,
                    DepositEnabled = coin.DepositAllEnable,
                    WithdrawalEnabled = coin.WithdrawAllEnable,
                    WithdrawalFee = networks.Max(n => n.WithdrawFee),
                    DepositConfirmations = networks.Max(n => n.MinConfirmations),
                    Networks = nets,
                    MinimumWithdrawal = networks.Max(n => n.WithdrawMin)
                });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting currency networks from Binance");
            return [];
        }
    }

    private TickerSymbol MapToTickerSymbol(BinanceSymbol symbol)
    {
        return new TickerSymbol
        {
            BaseAsset = symbol.BaseAsset,
            QuoteAsset = symbol.QuoteAsset,
            ExchangeSymbol = symbol.Name,
            Exchange = ExchangeName,
            IsActive = symbol.Status == SymbolStatus.Trading,
            MinimumQuantity = symbol.LotSizeFilter?.MinQuantity ?? 0,
            MinimumOrderValue = symbol.MinNotionalFilter?.MinNotional ?? 0,
            PricePrecision = symbol.QuoteAssetPrecision,
            TickSize = (decimal)Math.Pow(10, -symbol.BaseAssetPrecision)
        };
    }

    private async Task<BinanceSymbol[]> FilterSymbolsAsync(BinanceSymbol[] symbols)
    {
        _logger?.LogInformation("Filtering {SymbolCount} symbols based on trading volume", symbols.Length);
        
        var result = new List<BinanceSymbol>();
        var usdtPairs = symbols.Where(s => s.QuoteAsset == "USDT").ToArray();
        
        // Create lookup dictionaries for faster access
        var baseAssetToUsdtPair = usdtPairs.ToDictionary(s => s.BaseAsset, s => s);
        var quoteAssetToUsdtPair = usdtPairs.ToDictionary(s => s.BaseAsset, s => s);
        
        // Cache for price lookups to avoid redundant API calls
        var priceCache = new Dictionary<string, decimal>();
        
        try
        {
            // Process symbols in batches of 100 (API limit)
            const int batchSize = 100;
            var symbolNamesList = symbols.Select(s => s.Name).ToList();
            var symbolsByName = symbols.ToDictionary(s => s.Name, s => s);
            
            for (var i = 0; i < symbolNamesList.Count; i += batchSize)
            {
                // Take the next batch
                var currentBatch = symbolNamesList.Skip(i).Take(batchSize).ToArray();
                _logger?.LogDebug("Processing batch {BatchNumber} with {BatchSize} symbols (total {TotalProcessed}/{TotalSymbols})", 
                    i / batchSize + 1, currentBatch.Length, i + currentBatch.Length, symbolNamesList.Count);
                
                // Get trading data for the current batch
                var symbolsTradingDataResult = await _restClient.SpotApi.ExchangeData.GetRollingWindowTickersAsync(currentBatch);
                
                if (!symbolsTradingDataResult.Success)
                {
                    _logger?.LogError("Failed to get rolling window tickers: {Error}", symbolsTradingDataResult.Error?.Message);
                    continue;
                }
                
                var symbolsTradingData = symbolsTradingDataResult.Data;
                
                foreach (var symbolData in symbolsTradingData)
                {
                    try
                    {
                        if (!symbolsByName.TryGetValue(symbolData.Symbol, out var symbol))
                        {
                            continue;
                        }
                        
                        var usdtVolume = await CalculateUsdtVolumeAsync(
                            symbol,
                            symbolData,
                            baseAssetToUsdtPair,
                            quoteAssetToUsdtPair,
                            priceCache);
                        
                        if (usdtVolume < 1_000_000)
                        {
                            continue;
                        }
                        
                        _logger?.LogDebug("Adding {ExchangeName} symbol {Symbol} with USDT volume {Volume:C0}", ExchangeName, symbol.Name, usdtVolume);
                        result.Add(symbol);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing symbol data for {Symbol}", symbolData.Symbol);
                    }
                }
            }
            
            _logger?.LogInformation("Filtered {ResultCount} symbols based on trading volume criteria", result.Count);
            return result.ToArray();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error filtering symbols");
            return [];
        }
    }
    
    private async Task<decimal> CalculateUsdtVolumeAsync(
        BinanceSymbol symbol, 
        IBinance24HPrice symbolData,
        Dictionary<string, BinanceSymbol> baseAssetToUsdtPair,
        Dictionary<string, BinanceSymbol> quoteAssetToUsdtPair,
        Dictionary<string, decimal> priceCache)
    {
        var usdtVolume = 0m;
        
        try
        {
            // Try to calculate volume using base asset to USDT pair
            if (baseAssetToUsdtPair.TryGetValue(symbol.BaseAsset, out var baseAssetToUsdt))
            {
                var price = await GetCachedPriceAsync(baseAssetToUsdt.Name, priceCache);
                usdtVolume = symbolData.Volume * price;
            }
            // If no direct conversion to USDT via base asset, try with quote asset
            else if (quoteAssetToUsdtPair.TryGetValue(symbol.QuoteAsset, out var quoteAssetToUsdt))
            {
                var price = await GetCachedPriceAsync(quoteAssetToUsdt.Name, priceCache);
                usdtVolume = symbolData.QuoteVolume * price;
            }
            // If the quote asset is already USDT, use quote volume directly
            else if (symbol.QuoteAsset == "USDT")
            {
                usdtVolume = symbolData.QuoteVolume;
            }
            
            return usdtVolume;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error calculating USDT volume for {Symbol}", symbol.Name);
            return 0m;
        }
    }
    
    private async Task<decimal> GetCachedPriceAsync(string symbol, Dictionary<string, decimal> priceCache)
    {
        // Return cached price if available
        if (priceCache.TryGetValue(symbol, out var cachedPrice))
        {
            return cachedPrice;
        }
        
        // Get price from API and cache it
        try
        {
            var priceResult = await _restClient.SpotApi.ExchangeData.GetPriceAsync(symbol);
            
            if (!priceResult.Success)
            {
                _logger?.LogError("Failed to get price for {Symbol}: {Error}", symbol, priceResult.Error?.Message);
                return 0m;
            }
            
            var price = priceResult.Data.Price;
            priceCache[symbol] = price; // Cache the result
            return price;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting price for {Symbol}", symbol);
            return 0m;
        }
    }
}
