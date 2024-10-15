namespace Fake.Threading;

public abstract class CancellationTokenProviderBase : ICancellationTokenProvider
{
    public const string CancellationTokenProviderContextKey = "Fake.Threading.CancellationTokenScope";
    
    public abstract CancellationToken Token { get; }
    
    private readonly IAmbientScopeProvider<CancellationToken> _ambientScopeProvider;
    
    protected CancellationTokenProviderBase(IAmbientScopeProvider<CancellationToken> ambientScopeProvider)
    {
        _ambientScopeProvider = ambientScopeProvider;
    }
    
    public IDisposable Use(CancellationToken cancellationToken)
    {
        return _ambientScopeProvider.BeginScope(CancellationTokenProviderContextKey, cancellationToken);
    }


    protected CancellationToken CurrentToken => _ambientScopeProvider.GetValue(CancellationTokenProviderContextKey);
}