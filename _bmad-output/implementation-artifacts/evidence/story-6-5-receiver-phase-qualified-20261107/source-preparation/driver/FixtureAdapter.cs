namespace Story65.ReceiverPhase;
using System.Reflection;
internal sealed class FixtureAdapter
{
    private readonly Type api;
    internal FixtureAdapter(RuntimeInputs inputs) => api=inputs.Companion.GetType("Hexalith.PrivateReceiverFixture.FixtureApi",true)!;
    private MethodInfo Method(string name) => api.GetMethod(name,BindingFlags.Public|BindingFlags.Static)!;
    internal string[] Names() => (string[])Method("Names").Invoke(null,null)!;
    internal Task<object> CreateAsync(string name,Action<int>? probe=null) => (Task<object>)Method("CreateAsync").Invoke(null,[name,probe])!;
    internal Task<object[]> ExecuteAsync(object handle,bool suppressed=false) => (Task<object[]>)Method("ExecuteAsync").Invoke(null,[handle,suppressed])!;
    internal Task DisposeAsync(object handle) => (Task)Method("DisposeAsync").Invoke(null,[handle])!;
}
