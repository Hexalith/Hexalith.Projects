using Hexalith.EventStore.Contracts.Routing; using Hexalith.EventStore.ServiceDefaults.Routing; using Microsoft.AspNetCore.Routing;
internal delegate string? RequestFingerprint(TechnicalHttpRouteCatalogReceiver receiver,RouteEndpoint endpoint,out RouteCatalogTechnicalHttpEndpoint? description);
