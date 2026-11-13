using Microsoft.AspNetCore.Routing;
namespace Story65.TechnicalFingerprint.Harness;
internal sealed class WrongConstraintResolver : IInlineConstraintResolver
{
    internal int Calls;
    public IRouteConstraint? ResolveConstraint(string inlineConstraint) { Calls++; throw new FixtureFailure("custom-resolver-must-not-be-called", "WrongConstraintResolver.cs", 0); }
}
