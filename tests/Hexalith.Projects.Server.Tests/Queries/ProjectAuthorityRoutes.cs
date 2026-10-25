#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using Hexalith.EventStore.Contracts.Queries;
using Microsoft.AspNetCore.Builder;

/// <summary>Substitutes local service discovery only; every protected response comes from a real host.</summary>
internal sealed class ProjectAuthorityRoutes
{
    /// <summary>Gets owned local receiver addresses.</summary>
    public ConcurrentDictionary<string, Uri> Hosts { get; } = new();
    /// <summary>Gets actual domain receiver observations.</summary>
    public ConcurrentQueue<ProjectAuthorityHop> Hops { get; } = new();
    /// <summary>Gets observed profile requests without credentials.</summary>
    public ConcurrentQueue<ProjectAuthorityTokenAttempt> TokenRequests { get; } = new();
    /// <summary>Gets sanitized receiver attempts before forwarding.</summary>
    public ConcurrentQueue<ProjectAuthorityInvocationAttempt> Invocations { get; } = new();
    /// <summary>Gets the complete chronological token and service attempt sequence.</summary>
    public ConcurrentQueue<string> Attempts { get; } = new();
    /// <summary>Clears observations between an authorized control and a fault.</summary>
    public void ClearAttempts()
    {
        TokenRequests.Clear(); Invocations.Clear(); Attempts.Clear(); Hops.Clear();
    }
    /// <summary>Gets or sets an envelope fault applied after issuance.</summary>
    public Func<QueryEnvelope, QueryEnvelope>? ChangeQuery { get; set; }
    /// <summary>Gets or sets the receiver targeted by a fault.</summary>
    public string? ChangeTarget { get; set; }
    /// <summary>Gets or sets a foreign-issuer assertion factory.</summary>
    public Func<QueryEnvelope, string?>? ReplaceAssertion { get; set; }
    /// <summary>Gets or sets whether a service hop carried a human bearer header.</summary>
    public bool HumanBearerForwarded { get; set; }
    /// <summary>Registers an owned host after its listener starts.</summary>
    public void Add(string name, WebApplication app) => Hosts[name] = new(app.Urls.Single());
}
#endif
