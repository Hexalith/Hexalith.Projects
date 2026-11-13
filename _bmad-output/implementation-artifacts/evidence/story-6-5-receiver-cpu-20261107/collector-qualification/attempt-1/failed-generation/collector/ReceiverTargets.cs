internal static class ReceiverTargets
{
    private static readonly ReceiverTarget[] Allowed = [
        new("projects", "projects", "/home/administrator/projects/hexalith/projects/src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj", "Hexalith.Projects.Server", "/home/administrator/projects/hexalith/projects/src/Hexalith.Projects.Server/bin/Debug/net10.0/Hexalith.Projects.Server.dll", "/home/administrator/projects/hexalith/projects/src/Hexalith.Projects.Server/bin/Debug/net10.0/Hexalith.Projects.Server"),
        new("tenants", "tenants", "/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants/src/Hexalith.Tenants/Hexalith.Tenants.csproj", "Hexalith.Tenants", "/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants/src/Hexalith.Tenants/bin/Debug/net10.0/Hexalith.Tenants.dll", "/home/administrator/projects/hexalith/projects/references/Hexalith.Tenants/src/Hexalith.Tenants/bin/Debug/net10.0/Hexalith.Tenants"),
        new("folders", "folders", "/home/administrator/projects/hexalith/projects/references/Hexalith.Folders/src/Hexalith.Folders.Server/Hexalith.Folders.Server.csproj", "Hexalith.Folders.Server", "/home/administrator/projects/hexalith/projects/references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0/Hexalith.Folders.Server.dll", "/home/administrator/projects/hexalith/projects/references/Hexalith.Folders/src/Hexalith.Folders.Server/bin/Debug/net10.0/Hexalith.Folders.Server"),
    ];
    internal static ReceiverTarget? Match(string role, string resource, string sourceProject, string commandToken)
        => Allowed.SingleOrDefault(target => target.Role == role && target.Resource == resource && target.SourceProject == sourceProject
            && (commandToken == target.EntryDll || commandToken == target.NativeApphost));
    internal static bool MatchesAssembly(ReceiverTarget target, string label, string path) => label switch
    {
        "target-entry-assembly" => path == target.EntryDll,
        "domain-service-assembly" => path == target.DomainServiceDll,
        "sdk-assembly" => path == target.SdkDll,
        _ => false,
    };
}
