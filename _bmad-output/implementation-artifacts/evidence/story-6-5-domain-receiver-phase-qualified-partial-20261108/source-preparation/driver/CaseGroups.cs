namespace Story65.DomainReceiverPhase;
internal static class CaseGroups
{
    internal static readonly IReadOnlyDictionary<string,string[]> All = new Dictionary<string,string[]>
    {
        ["returns"]=["custom-no-lease-nonresult","custom-owned-nonresult","concrete-delayed-result","terminal-refusal-owned-result"],
        ["faults"]=["concrete-result-fault","concrete-result-cancel","concrete-release-override","result-release-cancel-override"],
        ["guard-next"]=["filter-next-fault","filter-next-cancel","guard-acquire-fault","guard-validation-fault"],
        ["nested"]=["nested-two-results-success","nested-inner-result-fault"],
        ["double-success"]=["filter-double-dispose-retry-success"],
        ["double-fault"]=["filter-double-dispose-second-fault"],
    };
    internal static readonly IReadOnlyDictionary<string,CaseExpectation> Expected = new Dictionary<string,CaseExpectation>
    {
        ["custom-no-lease-nonresult"]=new(1,0,1,0,0,147,null,false),
        ["custom-owned-nonresult"]=new(1,0,1,1,0,179,null,false),
        ["concrete-delayed-result"]=new(1,1,1,1,0,255,null,true),
        ["terminal-refusal-owned-result"]=new(1,1,0,1,0,255,null,true),
        ["concrete-result-fault"]=new(1,1,1,1,1,255,"System.InvalidOperationException",true),
        ["concrete-result-cancel"]=new(1,1,1,1,1,255,"System.OperationCanceledException",true),
        ["concrete-release-override"]=new(1,1,1,1,2,255,"System.IO.IOException",true),
        ["result-release-cancel-override"]=new(1,1,1,1,2,255,"System.OperationCanceledException",true),
        ["filter-next-fault"]=new(1,1,1,1,1,191,"System.InvalidOperationException",false),
        ["filter-next-cancel"]=new(1,1,1,1,1,191,"System.OperationCanceledException",false),
        ["guard-acquire-fault"]=new(1,1,0,0,1,135,null,false),
        ["guard-validation-fault"]=new(1,1,0,1,0,175,null,false),
        ["nested-two-results-success"]=new(2,2,2,2,0,255,null,true),
        ["nested-inner-result-fault"]=new(2,2,2,2,1,255,"System.InvalidOperationException",true),
        ["filter-double-dispose-retry-success"]=new(1,0,1,2,1,179,"Hexalith.EventStore.ServiceDefaults.Routing.RouteCatalogUnavailableException",false),
        ["filter-double-dispose-second-fault"]=new(1,0,1,2,2,179,"System.IO.IOException",false),
    };
}
