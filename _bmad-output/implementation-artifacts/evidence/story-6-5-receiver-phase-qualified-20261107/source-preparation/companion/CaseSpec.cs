namespace Hexalith.PrivateReceiverFixture;

/// <summary>Defines only closed fixture choices and expectations.</summary>
/// <param name="Name">Closed case name.</param>
/// <param name="Technical">Whether actual MVC middleware composition is required.</param>
/// <param name="Fault">Closed fault or refusal selector.</param>
/// <param name="ExpectedStatus">Expected response status, checked only in RAM.</param>
/// <param name="ExpectedError">Expected exception type.</param>
/// <param name="Acquires">Expected authority calls.</param>
/// <param name="NextCalls">Expected downstream calls.</param>
/// <param name="Releases">Expected actual release callbacks.</param>
/// <param name="Faults">Expected injected exceptions.</param>
/// <param name="StageMask">Expected local recorder aliases.</param>
internal sealed record CaseSpec(string Name, bool Technical, string Fault, int ExpectedStatus, Type? ExpectedError,
    int Acquires, int NextCalls, int Releases, int Faults, int StageMask);
