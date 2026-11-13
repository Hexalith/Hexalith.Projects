namespace Story65.DomainReceiverPhase;
internal sealed record CaseResult(string Case,bool Passed,int FilterEntries,int AuthorityAcquireCalls,int NextCalls,int ReleaseCallbacks,
    string? ErrorType,int Faults,bool TokensMatched,bool LifetimeAndHandoffMatched,bool CallerRestored,int ExpectedStageMask,
    long MinimumLastDisposeTicks,long MaximumLastDisposeTicks,long FirstDisposeHoldTicks,bool DoubleDisposeOrder,
    bool ReleaseBoundaryRestored,bool ExactContextReachedNext,bool FirstDependencyProbeObserved,bool EntrySuppressionObserved)
{
    internal static CaseResult Read(object[] r)
    {
        Check.Require(r.Length==20 && r[0] is string && r[1] is true, "closed-twenty-slot-result");
        var x=new CaseResult((string)r[0],(bool)r[1],(int)r[2],(int)r[3],(int)r[4],(int)r[5],(string?)r[6],(int)r[7],
            (bool)r[8],(bool)r[9],(bool)r[10],(int)r[11],(long)r[12],(long)r[13],(long)r[14],(bool)r[15],(bool)r[16],(bool)r[17],(bool)r[18],(bool)r[19]);
        Check.Require(CaseGroups.Expected.ContainsKey(x.Case),"closed-case-name");
        CaseExpectation expected=CaseGroups.Expected[x.Case];
        Check.Require(x.FilterEntries==expected.Entries && x.AuthorityAcquireCalls==expected.AuthorityAcquireCalls
            && x.NextCalls==expected.NextCalls && x.ReleaseCallbacks==expected.ReleaseCallbacks && x.Faults==expected.Faults
            && x.ErrorType==expected.ErrorType && x.ExpectedStageMask==expected.Mask,"exact-case-counts-mask-error");
        Check.Require(x.TokensMatched && x.LifetimeAndHandoffMatched && x.CallerRestored
            && x.ReleaseBoundaryRestored && x.ExactContextReachedNext,"semantic-lifetime-context-valid");
        if(x.Case.StartsWith("filter-double-dispose-",StringComparison.Ordinal))
            Check.Require(x.ReleaseCallbacks==2 && x.DoubleDisposeOrder && x.MinimumLastDisposeTicks>0
                && x.MaximumLastDisposeTicks>=x.MinimumLastDisposeTicks && x.FirstDisposeHoldTicks>x.MaximumLastDisposeTicks,"both-release-callbacks-and-bracket");
        return x;
    }
}
