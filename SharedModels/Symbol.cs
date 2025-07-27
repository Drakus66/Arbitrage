namespace Arbitrage.SharedModels;

/// <summary>
/// Represents a cryptocurrency symbol with network information across different exchanges
/// </summary>
public record Symbol
{
    /// <summary>
    /// The symbol code (e.g., BTC, ETH, USDT)
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// The full name of the cryptocurrency (e.g., Bitcoin, Ethereum, Tether)
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The exchange where this symbol is traded
    /// </summary>
    public string Exchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether deposits are enabled for this symbol on this network
    /// </summary>
    public bool DepositEnabled { get; set; }
    
    /// <summary>
    /// Whether withdrawals are enabled for this symbol on this network
    /// </summary>
    public bool WithdrawalEnabled { get; set; }
    
    /// <summary>
    /// Minimum amount required for a withdrawal
    /// </summary>
    public decimal MinimumWithdrawal { get; set; }
    
    /// <summary>
    /// Fee charged for withdrawals
    /// </summary>
    public decimal WithdrawalFee { get; set; }
    
    /// <summary>
    /// Number of confirmations required for a deposit to be credited
    /// </summary>
    public int DepositConfirmations { get; set; }
    
    /// <summary>
    /// Additional metadata specific to this symbol-network pair
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    public SymbolNetwork[] Networks { get; init; } = [];
    
    /// <summary>
    /// Returns a unique identifier for this symbol across exchanges and networks
    /// </summary>
    public string UniqueId => $"{Exchange}:{Code}";
    
    public override string ToString()
    {
        return $"{Code} on {Exchange}";
    }
}
