namespace Story65.ReceiverPhase;
internal static class CaseGroups
{
    internal static readonly IReadOnlyDictionary<string,string[]> All = new Dictionary<string,string[]>
    {
        ["ingress"]=["bypass","invalid-channel","native-config-discovery","native-allows-refusal"],
        ["refusals"]=["receiver-not-ready","native-acquire-refusal","native-snapshot-refusal","technical-auth-refusal"],
        ["execution"]=["native-success","native-next-fault","native-next-cancellation","technical-success"],
        ["denial"]=["technical-grant-denial","technical-next-fault-release-precedence"],
        ["double-success"]=["technical-double-dispose-retry-success"],
        ["double-fault"]=["technical-double-dispose-second-fault"],
    };
}
