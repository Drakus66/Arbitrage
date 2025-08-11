namespace Arbitrage.ExchangeConnectors;

using Arbitrage.ExchangeConnectors.Settings;
using Arbitrage.SharedModels;
using Binance.Net.Clients;
using Binance.Net.Enums;
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

        /*if (binanceSettings == null)
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
        }*/

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
                _logger.LogError("Failed to get exchange info: {Error}", exchangeInfo.Error?.Message);
                return [];
            }

            var exchangeSymbols = exchangeInfo.Data.Symbols;

            exchangeSymbols = await FilterSymbolsAsync(exchangeSymbols);

            return exchangeSymbols.Select(MapToTickerSymbol).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting symbols from Binance");
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
                _logger.LogError("Failed to get order book: {Error}", orderBookData.Error?.Message);
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
            _logger.LogError(ex, "Error getting order book from Binance for {Symbol}", symbol.ExchangeSymbol);
            return new OrderBook { Symbol = symbol };
        }
    }
    
    public async Task<List<Symbol>> GetCurrenciesAsync()
    {
        _logger.LogInformation("Skip getting currencies from {Name}", ExchangeName);
        return [];

        try
        {
            var userAssets = await _restClient.SpotApi.Account.GetUserAssetsAsync();

            if (!userAssets.Success)
            {
                _logger.LogError("Failed to get coin information: {Error}", userAssets.Error?.Message);
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
            _logger.LogError(ex, "Error getting currency networks from Binance");
            return [];
        }
    }

    public async Task<Dictionary<string, decimal>> GetTickersLastPricesAsync(IEnumerable<string> tickerNames)
    {
        var tickersData = await _restClient.SpotApi.ExchangeData.GetTickersAsync();
        if (!tickersData.Success)
        {
            _logger.LogError("Failed to get tickers data: {Error}", tickersData.Error?.Message);
            return new Dictionary<string, decimal>();
        }

        var tickers = tickersData.Data.Where(t => tickerNames.Contains(t.Symbol)).ToArray();
        var prices = tickers.ToDictionary(t => t.Symbol, t => t.LastPrice);

        return prices;
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
        _logger.LogInformation("Filtering {SymbolCount} symbols based on trading volume", symbols.Length);
        var filteredSymbols = new List<BinanceSymbol>();

        var tickersData = await _restClient.SpotApi.ExchangeData.GetTickersAsync();

        if (!tickersData.Success)
        {
            _logger.LogError("Failed to get tickers data: {Error}", tickersData.Error?.Message);
            return symbols;
        }

        var tickers = tickersData.Data;

        var usdtBasedTickers = tickers.Where(s => s.Symbol.EndsWith("USDT")).ToArray();

        foreach (var ticker in tickers)
        {
            var tickerName = ticker.Symbol;

            var symbol = symbols.FirstOrDefault(s => s.Name == tickerName);

            if (symbol == null || symbol.Status != SymbolStatus.Trading)
            {
                continue;
            }

            var tickerVolume = ticker.Volume;
            var tickerQuoteName = symbol.BaseAsset;
            var usdtBasedTicker = usdtBasedTickers.FirstOrDefault(s => s.Symbol.StartsWith(tickerQuoteName));

            if (usdtBasedTicker == null)
            {
                continue;
            }
            var usdtVolume = usdtBasedTicker.LastPrice * tickerVolume;

            if (usdtVolume < 1_000_000)
            {
                continue;
            }

            _logger.LogDebug("Adding {ExchangeName} symbol {Symbol} with USDT volume {Volume:C0}", ExchangeName, symbol.Name, usdtVolume);
            filteredSymbols.Add(symbol);
        }

        return filteredSymbols.ToArray();
    }
}
