namespace Story65.DaprInvokerProof;

internal static class CaseCatalog
{
    internal static readonly DriverCase[] All =
    [
        new("legacy-bound-success", Legacy: true),
        new("committed-conflicting-resolver"),
        new("null-query", "null"),
        new("acquire-throw", "acquire"),
        new("undeclared-query", "undeclared"),
        new("resolver-throw", "resolver", Legacy: true),
        new("registration-null", "registration-null", Legacy: true),
        new("missing-delegated-context", "missing-caller"),
        new("dapr-create-throw", "dapr"),
        new("real-binding-poison", "binding-poison", Legacy: true),
        new("client-factory-throw", "factory"),
        new("transport-throw", "transport"),
        new("transport-catalog", "transport-catalog"),
        new("transport-cancel", "transport-cancel"),
        new("default-buffer-io-fault", "buffer-io"),
        new("default-buffer-cancel", "buffer-cancel"),
        new("status-403", "status-403"),
        new("status-503-committed", "status-503"),
        new("json-malformed", "json-malformed"),
        new("json-null", "json-null"),
        new("projection-mismatch", "projection"),
        new("response-dispose-throw", "response-dispose"),
        new("request-dispose-throw", "request-dispose"),
        new("logger-throw", "logger"),
        new("lease-release-throw", "release"),
        new("lease-release-cancel", "release-cancel"),
        new("lease-release-catalog", "release-catalog"),
        new("held-release", "held-release", Slow: true),
        new("caller-cancel-release", "cancel-release", Slow: true),
        new("uncooperative-acquire-cancel", "held-acquire", Slow: true),
    ];
}
