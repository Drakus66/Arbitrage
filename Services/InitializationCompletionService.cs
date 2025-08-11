namespace Arbitrage.Services;

/// <summary>
/// Service that signals when initialization is complete and allows other services to wait for it
/// </summary>
public class InitializationCompletionService
{
    private TaskCompletionSource<bool> _initializationCompletionSource = new();

    /// <summary>
    /// Task that completes when initialization is done
    /// </summary>
    public Task InitializationCompleted => _initializationCompletionSource.Task;

    /// <summary>
    /// Signal that initialization has been completed
    /// </summary>
    public void SignalInitializationCompleted()
    {
        _initializationCompletionSource.TrySetResult(true);
    }

    /// <summary>
    /// Reset the initialization state (mainly for testing purposes)
    /// </summary>
    public void ResetInitialization()
    {
        if (_initializationCompletionSource.Task.IsCompleted)
        {
            _initializationCompletionSource = new();
        }
    }
}
