namespace Story65.DomainReceiverPhase;
internal sealed record CaseExpectation(int Entries,int AuthorityAcquireCalls,int NextCalls,int ReleaseCallbacks,
    int Faults,int Mask,string? ErrorType,bool OwnsDeferredResult);
