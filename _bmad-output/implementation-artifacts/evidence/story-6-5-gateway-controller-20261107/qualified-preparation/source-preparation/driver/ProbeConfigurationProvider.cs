namespace Story65.GatewayControllerProof;

using Microsoft.Extensions.Configuration;

internal sealed class ProbeConfigurationProvider(FixtureState state, Dictionary<string, string?> data) : ConfigurationProvider, IConfigurationSource
{
    public override void Load() => Data = data;
    public override bool TryGet(string key, out string? value)
    { state.Observe("configuration.read", key); return base.TryGet(key, out value); }
    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
}
