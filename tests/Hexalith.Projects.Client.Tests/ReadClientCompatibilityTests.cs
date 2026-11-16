namespace Hexalith.Projects.Client.Tests;

using System.Reflection;
using Hexalith.Projects.Client.Generated;
using Hexalith.Projects.Contracts.Queries;
using Shouldly;
using Xunit;

/// <summary>Public constructors, deconstruction, legacy callable methods and implementer compatibility.</summary>
public sealed class ReadClientCompatibilityTests
{
    [Fact]
    public void ExistingConstructorsAndDeconstructorsRemainCallable()
    {
        foreach (Type query in new[] { typeof(GetProjectContextQuery), typeof(GetConversationStartSetupQuery), typeof(ExplainContextSelectionQuery) })
        {
            query.GetConstructor([typeof(string)]).ShouldNotBeNull();
            query.GetMethod("Deconstruct", [typeof(string).MakeByRefType()]).ShouldNotBeNull();
            query.GetProperty("ConversationId").ShouldNotBeNull();
        }
        foreach (string operation in new[] { "GetProjectAsync", "GetProjectContextAsync", "GetProjectContextExplanationAsync", "GetConversationStartSetupAsync", "RefreshProjectContextAsync" })
        {
            typeof(IClient).GetMethod(operation, [typeof(string), typeof(string), typeof(ReadConsistencyClass?), typeof(CancellationToken)]).ShouldNotBeNull();
            typeof(Generated.Client).GetMethod(operation, [typeof(string), typeof(string), typeof(ReadConsistencyClass?), typeof(CancellationToken)]).ShouldNotBeNull();
        }
    }

    [Fact]
    public void AdditiveMetadataMethodsHaveDefaultBodiesForExistingClientImplementers()
    {
        MethodInfo[] additive = typeof(IClient).GetMethods().Where(method => method.GetParameters()
            .Any(parameter => parameter.Name is "inspectDescriptiveMetadata" or "conversationId")
            && method.Name is "ListProjectsAsync" or "GetProjectAsync" or "GetProjectContextAsync" or "GetProjectContextExplanationAsync" or "GetConversationStartSetupAsync" or "RefreshProjectContextAsync").ToArray();
        additive.Length.ShouldBe(6);
        foreach (MethodInfo method in additive) { method.IsAbstract.ShouldBeFalse(); method.GetMethodBody().ShouldNotBeNull(); }
    }
}
