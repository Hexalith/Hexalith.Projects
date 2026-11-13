namespace Story65.OutboundAssertionProof;

internal sealed record InvocationOutcome(Exception? Error, FixtureState State, string FinalHeaders, string InitialHeaders,
    bool ResponseReferencePreserved, bool RequestContentPreserved, bool OptionsPreserved,
    bool CallerCanceled, bool ErrorTokenIsCaller, bool WaitHeld,
    int RequestDisposedBeforeCaller, int ResponseDisposedBeforeCaller);
