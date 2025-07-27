namespace Arbitrage.ExchangeConnectors;

using Arbitrage.ExchangeConnectors.Settings;
using Arbitrage.SharedModels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Public;

public class OkxConnector : IExchange
{
    private readonly OKXRestClient _restClient;
    private readonly ILogger<OkxConnector>? _logger;

    public string ExchangeName => "OKX";
    public OkxConnector(ILogger<OkxConnector> logger, IOptions<ExchangeConnectionSettings>? settings)
    {
        _logger = logger;
        var okxSettings = settings?.Value.Okx;

        if (okxSettings == null)
        {
            _logger.LogError("{Name} settings not configured. Using only public info", ExchangeName);
        }
        else
        {
            _logger.LogDebug("Initializing Binance connector with API key: {ApiKeyStart}...",
                !string.IsNullOrEmpty(okxSettings.ApiKey) ? okxSettings.ApiKey.Substring(0, 5) + "..." : "Not provided");

            OKXRestClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new(okxSettings.ApiKey, okxSettings.SecretKey);
            });
            OKXSocketClient.SetDefaultOptions(options =>
            {
                options.ApiCredentials = new(okxSettings.ApiKey, okxSettings.SecretKey);
            });
        }
        _restClient = new OKXRestClient();

        _logger?.LogInformation("{Name} connector initialized", ExchangeName);
    }

    public async Task<List<TickerSymbol>> GetSymbolsAsync()
    {
        try
        {
            var exchangeInfo = await _restClient.UnifiedApi.ExchangeData.GetSymbolsAsync(InstrumentType.Spot);

            if (!exchangeInfo.Success)
            {
                _logger?.LogError("Failed to get exchange info: {Error}", exchangeInfo.Error?.Message);
                return new List<TickerSymbol>();
            }

            var result = new List<TickerSymbol>();

            var exchangeSymbols = exchangeInfo.Data;

            exchangeSymbols = await FilterSymbolsAsync(exchangeSymbols);

            foreach (var symbol in exchangeSymbols)
            {
                result.Add(MapToTickerSymbol(symbol));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting symbols from OKX");
            return new List<TickerSymbol>();
        }
    }

    public async Task<OrderBook> GetOrderBookAsync(TickerSymbol symbol, int depth = 20)
    {
        try
        {
            var orderBookResult = await _restClient.UnifiedApi.ExchangeData.GetOrderBookAsync(symbol.ExchangeSymbol);

            if (!orderBookResult.Success)
            {
                _logger?.LogError("Failed to get order book for {Symbol}: {Error}", symbol.ExchangeSymbol, orderBookResult.Error?.Message);
                return new OrderBook { Symbol = symbol };
            }

            var orderBook = new OrderBook
            {
                Symbol = symbol,
                Timestamp = DateTime.UtcNow,
                UpdateId = orderBookResult.Data.SequenceId ?? 0
            };

            // Map asks (sell orders)
            if (orderBookResult.Data?.Asks != null)
            {
                orderBook.Asks = orderBookResult.Data.Asks
                    .Select(a => new OrderBookEntry(a.Price, a.Quantity))
                    .OrderBy(a => a.Price)
                    .Take(depth)
                    .ToList();
            }

            // Map bids (buy orders)
            if (orderBookResult.Data?.Bids != null)
            {
                orderBook.Bids = orderBookResult.Data.Bids
                    .Select(b => new OrderBookEntry(b.Price, b.Quantity))
                    .OrderByDescending(b => b.Price)
                    .Take(depth)
                    .ToList();
            }

            return orderBook;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting order book for {Symbol} from OKX", symbol.ExchangeSymbol);
            return new OrderBook { Symbol = symbol };
        }
    }

    public async Task<List<Symbol>> GetCurrenciesAsync()
    {
        try
        {
            /*var currenciesResult = await _restClient.UnifiedApi.ExchangeData.GetCurrenciesAsync(); // ToDo: implement GET all currencies from OKX

            if (!currenciesResult.Success)
            {
                _logger?.LogError("Failed to get currencies: {Error}", currenciesResult.Error?.Message);
                return new List<Symbol>();
            }*/

            var result = new List<Symbol>();

            /*foreach (var currency in currenciesResult.Data)
            {
                // Create a Symbol object for each currency
                var symbol = new Symbol
                {
                    Code = currency.Currency,
                    Name = currency.Name ?? currency.Currency,
                    Exchange = ExchangeName,
                    DepositEnabled = currency.CanDeposit,
                    WithdrawalEnabled = currency.CanWithdraw,
                    Networks = currency.Chains?.Select(chain => new SymbolNetwork
                    {
                        Name = chain.Chain,
                        ContractAddress = chain.ContractAddress ?? string.Empty,
                        WithdrawFee = chain.WithdrawalFee,
                        WithdrawMin = chain.MinWithdrawal
                    }).ToArray() ?? (SymbolNetwork[])[]
                };

                // Add metadata if available
                if (currency.MinimumWithdrawal.HasValue)
                {
                    symbol.MinimumWithdrawal = currency.MinimumWithdrawal.Value;
                }

                result.Add(symbol);
            }*/

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting currencies from OKX");
            return new List<Symbol>();
        }
    }

    private async Task<OKXInstrument[]> FilterSymbolsAsync(IEnumerable<OKXInstrument> symbols)
    {
        return await Task.FromResult(symbols.Where(s => s.State == InstrumentState.Live).ToArray());
    }

    private TickerSymbol MapToTickerSymbol(OKXInstrument symbol)
    {
        return new TickerSymbol
        {
            BaseAsset = symbol.BaseAsset,
            QuoteAsset = symbol.QuoteAsset,
            ExchangeSymbol = symbol.Symbol,
            Exchange = ExchangeName,
            IsActive = symbol.State == InstrumentState.Live,
            MinimumOrderValue = symbol.MinimumOrderSize ?? 0,
            TickSize = symbol.TickSize ?? 0.00001m,
        };
    }
}
