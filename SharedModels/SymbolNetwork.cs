namespace Arbitrage.SharedModels;

public record SymbolNetwork
{
    public string Name { get; init; }
    public string ContractAddress { get; init; }
    public decimal WithdrawFee { get; set; }
    public decimal WithdrawMin { get; set; }
}
