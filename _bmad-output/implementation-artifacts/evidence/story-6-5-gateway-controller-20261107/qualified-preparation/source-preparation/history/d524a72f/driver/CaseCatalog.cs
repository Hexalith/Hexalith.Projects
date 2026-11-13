namespace Story65.GatewayControllerProof;

internal static class CaseCatalog
{
    internal static DriverCase[] All =>
    [
        new("valid-schema3-origin"), new("valid-no-origin", Origin: false),
        new("initial-guard-forbid", Fault: "guard"), new("signature-forbid", Fault: "signature"),
        new("origin-forbid", Fault: "origin"), new("acquire-throw", Fault: "acquire"),
        new("admission-forbid", Fault: "surface"), new("mediator-throw-with-prior-marker", Fault: "mediator", PriorMarker: true),
        new("mediator-cancel", Fault: "mediator-cancel"), new("response-header-throw", Fault: "response"),
        new("release-throw", Fault: "release"), new("prior-marker-success", PriorMarker: true),
        new("delayed-release", Fault: "held-release", Slow: true),
        new("caller-cancel-during-release", Fault: "cancel-release", Slow: true),
        new("uncooperative-acquire-cancel", Fault: "held-acquire", Slow: true),
    ];
}
