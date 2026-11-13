namespace Story65.OutboundAssertionProof;

internal static class CaseCatalog
{
    internal static readonly DriverCase[] All =
    [
        new("query-issued-sync", "direct-sync"),
        new("command-issued-bound", Route: "process"),
        new("command-issued-unbound", Route: "process", Bindings: false),
        new("command-bind-refused", "cannot-bind", Route: "process"),
        new("command-bind-missing-issuer", "missing-command", Route: "process"),
        new("noncanonical-untouched", Route: "api/v1/commands"),
        new("null-request", "null"),
        new("issuer-missing", "missing"),
        new("issuer-resolution-invalidoperation", "resolve-invalid"),
        new("issuer-resolution-other", "resolve-other"),
        new("issuer-null-bounded-diagnostic", "issuer-null"),
        new("logger-missing-fallback", "missing-logger"),
        new("issuer-sync-fault", "issuer-sync-fault", SourceSync: true),
        new("issuer-async-fault", "issuer-async-fault"),
        new("issuer-cancel", "issuer-cancel"),
        new("issuer-invalidoperation", "issuer-invalid"),
        new("base-fault", "base"),
        new("base-cancel", "base-cancel"),
        new("logger-resolution-throw", "logger-resolution"),
        new("logger-throw", "logger"),
        new("outer-buffer-success", OuterBuffer: true),
        new("outer-buffer-fault", "buffer", OuterBuffer: true),
        new("issuer-source-sync-success", SourceSync: true),
        new("held-issuer", "held-issuer", Slow: true),
        new("held-base", "held-base", Slow: true),
    ];
}
