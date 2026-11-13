namespace Story65.DelegatedClientProof;

internal static class CaseCatalog
{
    internal static DriverCase[] All =>
    [
        new("success-unbounded"), new("success-owner3-caller2", Budget: "00:00:03"),
        new("value-source-sync", ValueSource: "source-sync"),
        new("value-source-sync-bounded", Budget: "00:00:03", ValueSource: "source-sync"),
        new("value-source-async", ValueSource: "source-async"),
        new("value-source-async-bounded", Budget: "00:00:03", ValueSource: "source-async"),
        new("issuer-null", Fault: "issuer-null"), new("issuer-throw", Fault: "issuer-throw"),
        new("value-source-fault", Fault: "issuer-source-fault", ValueSource: "source-async"),
        new("value-source-fault-bounded", Fault: "issuer-source-fault", Budget: "00:00:03", ValueSource: "source-async"),
        new("value-source-cancel", Fault: "issuer-source-cancel", ValueSource: "source-async"),
        new("value-source-cancel-bounded", Fault: "issuer-source-cancel", Budget: "00:00:03", ValueSource: "source-async"),
        new("null-query", Fault: "null-query"), new("missing-caller", Fault: "missing-caller"),
        new("foreign-tenant", Fault: "foreign-tenant"), new("duplicate-assertion", Fault: "duplicate-assertion"),
        new("missing-workload", Workload: ""), new("whitespace-workload", Workload: " "),
        new("missing-key", Fault: "missing-key"), new("whitespace-key", Fault: "whitespace-key"),
        new("nonprojects", Workload: "conversations"), new("case-sensitive-workload", Workload: "Projects"),
        new("nontrimmed-workload", Workload: "projects "),
        new("workload-provider-throw", Fault: "config-workload"), new("key-provider-throw", Fault: "config-key"),
        new("timeout-provider-throw", Fault: "config-timeout"), new("gateway-provider-throw", Fault: "config-gateway"),
        new("invalid-budget", Budget: "invalid-protected-synthetic"),
        new("factory-throw", Fault: "factory-throw"), new("proof-clock-throw", Fault: "proof-clock-throw"),
        new("dapr-request-throw", Fault: "dapr-request-throw"),
        new("handler-throw", Fault: "handler-throw"),
        new("proof-key-invalid", Fault: "proof-key-invalid"), new("buffer-throw", Fault: "buffer-throw"),
        new("json-malformed", Fault: "json-malformed"), new("response-mismatch", Fault: "response-mismatch"),
        new("response-disposal-throw", Fault: "response-disposal-throw"),
        new("request-disposal-throw", Fault: "request-disposal-throw"),
        new("late-response-disposal", Fault: "late-response-disposal", Budget: "00:00:00.100"),
        new("late-request-disposal", Fault: "late-request-disposal", Budget: "00:00:00.100"),
        new("buffered-body-delay", Fault: "buffered-body-delay"),
        new("status401", Status: 401), new("status403", Status: 403), new("status404", Status: 404), new("status500", Status: 500),
        new("concurrent-two", Concurrency: 2), new("concurrent-four", Concurrency: 4),
        new("profile-owner3-caller2-issuer", Fault: "issuer-cooperative", Budget: "00:00:03", ValueSource: "source-async", Slow: true),
        new("profile-owner3-caller2-transport", Fault: "transport-cooperative", Budget: "00:00:03", Slow: true),
        new("profile-owner3-caller2-buffer", Fault: "buffer-cooperative", Budget: "00:00:03", Slow: true),
        new("bounded-uncooperative-issuer", Fault: "issuer-uncooperative", Budget: "00:00:03", ValueSource: "source-async", Slow: true),
        new("unbounded-uncooperative-issuer", Fault: "issuer-uncooperative", ValueSource: "source-async", Slow: true),
    ];

    internal static bool Eligible(DriverCase test) => test.Workload == "projects"
        && test.Fault is not ("null-query" or "missing-caller" or "foreign-tenant" or "duplicate-assertion"
            or "missing-key" or "whitespace-key" or "config-workload" or "config-key");
}
