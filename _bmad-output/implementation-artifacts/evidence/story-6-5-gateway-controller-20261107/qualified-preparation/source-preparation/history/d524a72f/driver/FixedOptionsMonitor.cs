namespace Story65.GatewayControllerProof;

using Microsoft.Extensions.Options;

internal sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
{
    public T CurrentValue => value;
    public T Get(string? name) => value;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
