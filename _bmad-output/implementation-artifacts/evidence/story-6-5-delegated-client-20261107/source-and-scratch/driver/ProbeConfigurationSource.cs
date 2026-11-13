namespace Story65.DelegatedClientProof;

using Microsoft.Extensions.Configuration;

internal sealed class ProbeConfigurationSource(ProbeConfigurationProvider provider) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => provider;
}
