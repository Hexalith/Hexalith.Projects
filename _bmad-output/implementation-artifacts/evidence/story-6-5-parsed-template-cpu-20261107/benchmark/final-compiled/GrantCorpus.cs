using Hexalith.EventStore.Contracts.Routing;
internal sealed record GrantCorpus(string Name,RouteCatalogTechnicalHttpEndpoint[] Endpoints,RouteCatalogDeclaredInvocation[] Invocations,string Json,object[] Refused);
