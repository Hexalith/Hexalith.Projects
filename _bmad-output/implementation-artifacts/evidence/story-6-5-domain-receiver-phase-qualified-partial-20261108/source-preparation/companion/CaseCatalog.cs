namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Fixes the sixteen reviewed scenarios and exact counts.</summary>
internal static class CaseCatalog
{
    internal static readonly CaseSpec[] All =
    [
        new("custom-no-lease-nonresult",1,0,1,0,0,147,null),
        new("custom-owned-nonresult",1,0,1,1,0,179,null),
        new("concrete-delayed-result",1,1,1,1,0,255,null),
        new("concrete-result-fault",1,1,1,1,1,255,"System.InvalidOperationException"),
        new("concrete-result-cancel",1,1,1,1,1,255,"System.OperationCanceledException"),
        new("concrete-release-override",1,1,1,1,2,255,"System.IO.IOException"),
        new("filter-next-fault",1,1,1,1,1,191,"System.InvalidOperationException"),
        new("filter-next-cancel",1,1,1,1,1,191,"System.OperationCanceledException"),
        new("guard-acquire-fault",1,1,0,0,1,135,null),
        new("guard-validation-fault",1,1,0,1,0,175,null),
        new("terminal-refusal-owned-result",1,1,0,1,0,255,null),
        new("filter-double-dispose-retry-success",1,0,1,2,1,179,"Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogUnavailableException"),
        new("filter-double-dispose-second-fault",1,0,1,2,2,179,"System.IO.IOException"),
        new("nested-two-results-success",2,2,2,2,0,255,null),
        new("nested-inner-result-fault",2,2,2,2,1,255,"System.InvalidOperationException"),
        new("result-release-cancel-override",1,1,1,1,2,255,"System.OperationCanceledException"),
    ];
}
