namespace Story65.InvocationTransportProof;
internal static class CaseCatalog
{
    private static readonly string[] Whole = ["Whole"], Transport = ["Whole", "ComponentFile"], Release = ["Whole", "ComponentPolicy"], Both = ["Whole", "ComponentFile", "ComponentPolicy"];
    // Derived from the unchanged SendAsync ownership/control branches, never from callback/transport counts.
    internal static readonly DriverCase[] All = [
        new("owned-success", false, [Both]), new("borrowed-success", false, [Transport]), new("held-success", false, [Transport]),
        new("unsupported-basic-refusal", false, [Whole]), new("undeclared-target-refusal", false, [Release]), new("pre-cancel-before-acquire", false, [Whole]),
        new("cancel-before-enrollment", false, [Release]), new("transport-fault-retains", false, [Both]), new("transport-cancel-retains", false, [Both]),
        new("release-fault-overrides-response", false, [Both]), new("release-fault-overrides-validation", false, [Release]), new("borrowed-observer-refusal", false, [Whole]),
        new("held-scope-closed-refusal", false, [Whole]), new("ordinary-direct-nochildren", false, [Whole]), new("control-nochildren", false, [Whole]),
        new("outer-buffer-success", false, [Both]), new("outer-buffer-fault", false, [Both]), new("ordinary-redirect-reentry", false, [Whole, Both]),
        new("held-transport", true, [Both]), new("held-release", true, [Both]),
    ];
}
