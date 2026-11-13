namespace Hexalith.PrivateCaptureCounter;

internal enum AdvisoryTermination
{
    UnobservedExit, ResponseReturnedLocalOmissionNotObserved, ResponseReturnedLocalOmissionObservedUnattributed,
    DenialReturnEntered, InvalidReturnEntered, InitialScopeFallbackEntered,
    OperationFallbackEntered, CallerCancellationRethrown,
}
