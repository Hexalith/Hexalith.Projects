"""Verify the guarded owner-only source boundary, without restoring or adopting package-mode product dependencies."""
import json,os,subprocess
from pathlib import Path
root=Path.cwd();out=Path(__file__).resolve().parent;work=Path(os.environ['TMPDIR'])/'release-boundary';work.mkdir()
commands=[]
def run(label,command):
 result=subprocess.run(command,cwd=root,capture_output=True,text=True);commands.append({'label':label,'command':command,'exitCode':result.returncode})
 (out/(label+'.log')).write_text(result.stdout+result.stderr)
 assert result.returncode==0,(result.stdout+result.stderr)[-2500:]
 return result.stdout
metadata=json.loads(run('release-items',['dotnet','msbuild','tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj','-nologo','-p:Configuration=Release','-p:UseHexalithProjectReferences=false','-getProperty:DefineConstants','-getItem:Compile,ProjectReference']))
constants=metadata['Properties']['DefineConstants'].split(';');assert 'HEXALITH_SOURCE_FIXTURE' not in constants
references=[p['Identity'] for p in metadata['Items']['ProjectReference']]
assert not any('Hexalith.Conversations.Server' in p or 'Hexalith.Folders.Server' in p for p in references)
names=['NativeConversationMembershipTransportTests.cs','NativeMembershipOwnerAccess.cs','NativeMembershipOwnerStore.cs']
paths=[root/'tests/Hexalith.Projects.Server.Tests/Queries'/name for name in names]
for p in paths:assert p.name in {Path(item['Identity']).name for item in metadata['Items']['Compile']}
(work/'boundary.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath></Reference><Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference></ItemGroup></Project>''')
(work/'Program.cs').write_text('''using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.Json;
var release = new CSharpParseOptions(preprocessorSymbols: args[0].Split(';', StringSplitOptions.RemoveEmptyEntries));
var trees = args.Skip(1).Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), release, path)).ToArray();
foreach (var tree in trees)
{
 var unit = tree.GetCompilationUnitRoot();
 if (unit.Members.Count != 0 || unit.Usings.Count != 0) throw new Exception("Owner code escaped the Release guard: " + tree.FilePath);
 var enabled = CSharpSyntaxTree.ParseText(File.ReadAllText(tree.FilePath), new CSharpParseOptions(preprocessorSymbols: new[] { "HEXALITH_SOURCE_FIXTURE" }));
 if (enabled.GetCompilationUnitRoot().Members.Count == 0) throw new Exception("Source-mode guard did not enable owner tests.");
}
var compilation = CSharpCompilation.Create("ReleaseOwnerBoundary", trees,
 new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
using var bytes = new MemoryStream();
var result = compilation.Emit(bytes);
if (!result.Success) throw new Exception(string.Join("\\n", result.Diagnostics));
Console.WriteLine(JsonSerializer.Serialize(new { passed = true, files = trees.Length, releaseOwnerDeclarations = 0, ownerServerReferences = 0, packageAdoption = false }));
''')
output=run('release-preprocessor-compile',['dotnet','run','--project',str(work/'boundary.csproj'),'--artifacts-path',str(work/'artifacts'),'--',';'.join(constants),*[str(p) for p in paths]])
proof=json.loads(output[output.rfind('{'):]);proof.update({'releaseConstants':constants,'releaseProjectReferences':references,'commands':commands})
(out/'release-owner-boundary.json').write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps({'passed':True,'guardedFiles':len(paths),'packageAdoption':False}))
