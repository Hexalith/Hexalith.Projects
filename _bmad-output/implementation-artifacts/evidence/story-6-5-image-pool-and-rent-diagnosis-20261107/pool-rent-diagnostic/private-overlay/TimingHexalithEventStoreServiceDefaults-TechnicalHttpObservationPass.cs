using System.Reflection;
using System.Text.Json;
using System.Text.Json.Schema;

using Hexalith.EventStore.Contracts.Routing;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.EventStore.ServiceDefaults.Routing;

/// <summary>Deduplicates reflection work within one observation while rechecking current files, schemas and policies before publishing it.</summary>
internal sealed class TechnicalHttpObservationPass(IServiceProvider services)
{
    private readonly Dictionary<Assembly, string> _assemblies = [];
    private readonly Dictionary<Type, string> _schemas = [];
    private readonly Dictionary<string, AuthorizationPolicy?> _policies = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _policyIdentities = new(StringComparer.Ordinal);
    private string? _declaredInvocationText;
    private RouteCatalogDeclaredInvocation[] _declaredInvocations = [];
    private readonly List<(Func<string?> Current, string? Expected)> _observations = [];
    private readonly JsonSerializerOptions _options = Options(services);

    internal string AssemblyIdentity(Assembly assembly)
    {
        if (!_assemblies.TryGetValue(assembly, out string? identity))
            _assemblies.Add(assembly, identity = TechnicalHttpActionObservation.AssemblyIdentity(assembly));
        return identity;
    }

    internal string TypeIdentity(Type type) => $"{type.AssemblyQualifiedName}:{type.Module.ModuleVersionId}:"
        + (type.Assembly.GetName().Name?.StartsWith("Hexalith.EventStore", StringComparison.Ordinal) == true ? AssemblyIdentity(type.Assembly) : "framework");

    internal string Schema(Type type)
    {
        using var __focus45 = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("technical.schema.lookup");
        if (type == typeof(void)) return "http-no-body";
        if (typeof(FileResult).IsAssignableFrom(type) || typeof(Stream).IsAssignableFrom(type)) return "http-binary:" + TypeIdentity(type);
        if (!_schemas.TryGetValue(type, out string? schema)) _schemas.Add(type, schema = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Schema(_options, type, "technical.schema.initial.export"));
        return schema;
    }

    internal AuthorizationPolicy? Policy(string name)
    {
        using var __focus46 = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("technical.policy");
        if (!_policies.TryGetValue(name, out var policy))
        {
            policy = services.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(name).GetAwaiter().GetResult();
            _policies.Add(name, policy); _policyIdentities.Add(name, PolicyIdentity(policy));
        }
        return policy;
    }

    internal void RequireUnchanged(Func<string?> current, string? expected) => _observations.Add((current, expected));

    /// <summary>Reuses parsing only for exactly equal freshly read configuration text within this observation.</summary>
    internal IReadOnlyList<RouteCatalogDeclaredInvocation> DeclaredInvocations(string? json)
    {
        if (!string.Equals(json, _declaredInvocationText, StringComparison.Ordinal))
        {
            var invocations = json is null ? [] : JsonSerializer.Deserialize<RouteCatalogDeclaredInvocation[]>(json) ?? [];
            _declaredInvocationText = json;
            _declaredInvocations = invocations;
        }
        return _declaredInvocations;
    }

    internal void Complete()
    {
        using var __focus44 = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("technical.complete");
        foreach (var observation in _observations)
            if (observation.Current() != observation.Expected)
                throw new RouteCatalogUnavailableException("Native SDK runtime inventory changed during observation.", RouteCatalogRefusalReason.Inventory);
        var currentOptions = Options(services);
        foreach (var schema in _schemas)
            if (global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Schema(currentOptions, schema.Key, "technical.schema.complete.export") != schema.Value)
                throw new RouteCatalogUnavailableException("Technical HTTP serializer metadata changed during observation.", RouteCatalogRefusalReason.Inventory);
        foreach (var policy in _policyIdentities)
            if (PolicyIdentity(services.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(policy.Key).GetAwaiter().GetResult()) != policy.Value)
                throw new RouteCatalogUnavailableException("Technical HTTP policy changed during observation.", RouteCatalogRefusalReason.Inventory);
        foreach (var assembly in _assemblies)
            if (TechnicalHttpActionObservation.AssemblyIdentity(assembly.Key) != assembly.Value)
                throw new RouteCatalogUnavailableException("Technical HTTP loaded image changed during observation.", RouteCatalogRefusalReason.Inventory);
    }

    private static JsonSerializerOptions Options(IServiceProvider provider)
    {
        using var __focus47 = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("technical.options.total");
        using var __copy = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("technical.options.copy");
        var options = new JsonSerializerOptions(provider.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);
        __copy?.Dispose();
        using var __readonly = global::Hexalith.PrivateFocusedTiming.TimingHexalithEventStoreServiceDefaults.Counted("technical.options.readonly");
        options.MakeReadOnly(populateMissingResolver: true); return options;
    }

    private static string PolicyIdentity(AuthorizationPolicy? policy) => JsonSerializer.Serialize(policy is null ? null : new
    {
        policy.AuthenticationSchemes, requirements = policy.Requirements.Select(requirement => requirement is ClaimsAuthorizationRequirement claim
            ? $"claim:{claim.ClaimType}:{string.Join(',', (claim.AllowedValues ?? []).Order(StringComparer.Ordinal))}" : requirement.GetType().AssemblyQualifiedName).ToArray(),
    });
}
