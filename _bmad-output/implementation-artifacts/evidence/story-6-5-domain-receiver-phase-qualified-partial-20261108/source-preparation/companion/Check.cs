using System.Runtime.CompilerServices;
namespace Hexalith.PrivateDomainReceiverFixture;

/// <summary>Throws no arbitrary values or exception text across the reflection boundary.</summary>
internal static class Check
{
    internal static void Require(bool condition, string closedCheck, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (!condition) throw new FixtureFailure(closedCheck, Path.GetFileName(file), line);
    }
}
