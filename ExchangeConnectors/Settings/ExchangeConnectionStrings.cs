namespace Arbitrage.ExchangeConnectors.Settings;

/// <summary>
/// Configuration settings for Binance exchange
/// </summary>
public class ExchangeConnectionStrings
{
    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Secret Key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
}
