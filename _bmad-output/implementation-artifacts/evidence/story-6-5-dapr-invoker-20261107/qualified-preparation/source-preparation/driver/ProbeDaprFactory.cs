namespace Story65.DaprInvokerProof;

using System.Text.Json;
using Dapr.Client;
using Hexalith.EventStore.Contracts.Queries;
using NSubstitute;

internal static class ProbeDaprFactory
{
    internal static DaprClient Create(FixtureState state, QueryEnvelope query)
    {
        var client = Substitute.For<DaprClient>();
        client.CreateInvokeMethodRequest<QueryEnvelope>(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<KeyValuePair<string, string>>>(), Arg.Any<QueryEnvelope>())
            .Returns(call =>
            {
                HttpMethod method = call.ArgAt<HttpMethod>(0); string app = call.ArgAt<string>(1), path = call.ArgAt<string>(2);
                var parameters = call.ArgAt<IReadOnlyCollection<KeyValuePair<string, string>>>(3); var body = call.ArgAt<QueryEnvelope>(4);
                // Do not eagerly serialize derived query properties: the real request body is lazy,
                // and the binding-poison case must fail inside original Bind after request creation.
                state.RequestArgumentsJson = JsonSerializer.Serialize(new { Method = method.Method, App = app, Path = path, Parameters = parameters });
                state.RequestUsesOriginalQuery = ReferenceEquals(body, query);
                state.Observe("dapr.request", state.RequestArgumentsJson); state.ThrowIf("dapr");
                Check.Require(method == HttpMethod.Post && parameters is { Count: 0 }, "Original Dapr 3arg forwarding semantics changed.");
                HttpRequestMessage message = SharedResources.Dapr.CreateInvokeMethodRequest(method, app, path, parameters, body);
                state.Requests++;
                message.Content = new ProbeRequestContent(message.Content!, state).CopyHeaders();
                return message;
            });
        return client;
    }
}
