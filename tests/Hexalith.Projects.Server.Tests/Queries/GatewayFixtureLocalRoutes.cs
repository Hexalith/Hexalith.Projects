#if HEXALITH_SOURCE_FIXTURE
namespace Hexalith.Projects.Server.Tests.Queries;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;

internal sealed class GatewayFixtureLocalRoutes
    {
        public ConcurrentDictionary<string, Uri> Hosts { get; } = new();
        public System.Net.HttpStatusCode? OwnerStatusOverride { get; set; }
        public string? OwnerResponseOverride { get; set; }
        public bool TamperNextProjectQuery { get; set; }
        public bool HumanBearerForwarded { get; set; }
        public int OwnerQueries { get; set; }
        public void Add(string name, WebApplication app) => Hosts[name] = new Uri(app.Urls.Single());
    }
#endif
