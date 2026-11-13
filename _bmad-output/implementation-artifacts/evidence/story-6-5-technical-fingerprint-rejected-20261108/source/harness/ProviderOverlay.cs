namespace Story65.TechnicalFingerprint.Harness;
internal sealed class ProviderOverlay(IServiceProvider inner, IReadOnlyDictionary<Type, object> replacements) : IServiceProvider
{
    public object? GetService(Type serviceType) => replacements.TryGetValue(serviceType, out var value) ? value : inner.GetService(serviceType);
}
