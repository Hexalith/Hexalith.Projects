using Hexalith.EventStore.Contracts.Routing; using Hexalith.EventStore.ServiceDefaults.Routing; using Microsoft.AspNetCore.Routing;
internal delegate string? Fingerprint(TechnicalHttpRouteCatalogReceiver receiver,RouteEndpoint endpoint,TechnicalHttpObservationPass pass,out RouteCatalogTechnicalHttpEndpoint? description);
