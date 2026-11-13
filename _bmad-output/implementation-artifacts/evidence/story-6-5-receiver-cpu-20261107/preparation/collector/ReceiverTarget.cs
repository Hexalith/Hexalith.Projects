internal sealed record ReceiverTarget(string Role, string Resource, string SourceProject, string EntryAssemblyName, string EntryDll, string NativeApphost)
{
    internal string Directory => Path.GetDirectoryName(EntryDll)!;
    internal string DomainServiceDll => Path.Combine(Directory, "Hexalith.EventStore.DomainService.dll");
    internal string SdkDll => Path.Combine(Directory, "Hexalith.EventStore.ServiceDefaults.dll");
}
