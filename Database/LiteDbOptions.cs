namespace Arbitrage.Database;

/// <summary>
/// Configuration options for LiteDB
/// </summary>
public class LiteDbOptions
{
    /// <summary>
    /// Path to the LiteDB database file
    /// </summary>
    public string DatabasePath { get; set; } = "Data/arbitrage.db";
}
