namespace Story65.DelegatedClientProof;

using System.Net.Http.Json;
using System.Text.Json;
using Dapr.Client;
using Hexalith.EventStore.Client.Gateway;
using NSubstitute;

internal static class ProbeDaprFactory
{
    internal static DaprClient Create(FixtureState state)
    {
        var client = Substitute.For<DaprClient>();
        client.CreateInvokeMethodRequest<BoundOwnerQueryRequest>(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<KeyValuePair<string, string>>>(), Arg.Any<BoundOwnerQueryRequest>())
            .Returns(call =>
            {
                var body = call.ArgAt<BoundOwnerQueryRequest>(4);
                state.Observe("dapr.request", JsonSerializer.Serialize(new { Method = call.ArgAt<HttpMethod>(0).Method,
                    Gateway = call.ArgAt<string>(1), Route = call.ArgAt<string>(2), Request = body }));
                state.Trigger("dapr-request-throw");
                return new HttpRequestMessage(call.ArgAt<HttpMethod>(0), "http://synthetic-dapr.invalid/owner-query")
                { Content = new ProbeRequestContent(JsonContent.Create(body), state).CopyHeaders() };
            });
        return client;
    }
}
