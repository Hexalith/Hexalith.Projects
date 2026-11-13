from pathlib import Path
import re,json,hashlib,difflib,subprocess
ROOT=Path('/home/administrator/projects/hexalith/projects');OUT=Path('/tmp/story65-focused-owner-overlay-20261107');OUT.mkdir(mode=0o700,exist_ok=True)
helper='''using System.Diagnostics;
using System.Text.Json;
namespace Hexalith.PrivateFocusedTiming;
internal sealed class CLASS : IDisposable
{
    private static readonly object Sync = new();
    private static readonly AsyncLocal<long> Current = new();
    private static long Sequence;
    private static int Records;
    private static int Samples;
    private static readonly Dictionary<(long Parent, string Stage), (long Count, long Total, long First, long Last)> Counts = new();
    private readonly long Id = Interlocked.Increment(ref Sequence);
    private readonly long Parent = Current.Value;
    private readonly long Started = Stopwatch.GetTimestamp();
    private readonly string Stage;
    private readonly bool Root;
    private readonly bool Aggregate;
    private int Closed;
    private CLASS(string stage, bool root = false, bool aggregate = false) { Stage = stage; Root = root; Aggregate = aggregate; if (!aggregate) { Current.Value = Id; Write("begin", 0); } }
    internal static CLASS? Start(string stage) => File.Exists("DIRECTORY/collect.enabled") ? new(stage) : null;
    internal static CLASS? Counted(string stage) => Current.Value != 0 ? new(stage, aggregate: true) : null;
    internal static CLASS? Advisory() => File.Exists("DIRECTORY/collect.enabled") && Interlocked.Increment(ref Samples) <= 3 ? new("advisory.root", true) : null;
    internal static void Open() { try { File.WriteAllText("DIRECTORY/collect.enabled", "enabled"); } catch { } }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref Closed, 1) != 0) return;
        if (Aggregate)
        {
            lock (Sync) { var key = (Parent, Stage); Counts.TryGetValue(key, out var old); long now = Stopwatch.GetTimestamp(); Counts[key] = (old.Count + 1, old.Total + now - Started, old.Count == 0 ? Started : old.First, now); }
            return;
        }
        Write("end", Stopwatch.GetTimestamp() - Started); Current.Value = Parent;
        try { lock (Sync)
        {
            foreach (var entry in Counts.Where(entry => entry.Key.Parent == Id).ToArray())
            {
                if (Interlocked.Increment(ref Records) <= 250000)
                    File.AppendAllText("DIRECTORY/COMPONENT-" + Environment.ProcessId + ".jsonl", JsonSerializer.Serialize(new { component = "COMPONENT", pid = Environment.ProcessId, stage = entry.Key.Stage, state = "aggregate", parent = Id, count = entry.Value.Count, elapsedTicks = entry.Value.Total, firstTicks = entry.Value.First, lastTicks = entry.Value.Last, frequency = Stopwatch.Frequency }) + Environment.NewLine);
                Counts.Remove(entry.Key);
            }
        }
        } catch { }
        if (Root && Volatile.Read(ref Samples) >= 3)
        {
            try { File.Delete("DIRECTORY/collect.enabled"); File.WriteAllText("DIRECTORY/sample-complete", "3"); } catch { }
        }
    }
    private void Write(string state, long elapsed)
    {
        try
        {
            if (Interlocked.Increment(ref Records) > 250000) return;
            var activity = Activity.Current;
            lock (Sync) File.AppendAllText("DIRECTORY/COMPONENT-" + Environment.ProcessId + ".jsonl",
                JsonSerializer.Serialize(new { component = "COMPONENT", pid = Environment.ProcessId, stage = Stage, state,
                    ordinal = Id, parent = Parent, ticks = Stopwatch.GetTimestamp(), elapsedTicks = elapsed, frequency = Stopwatch.Frequency,
                    traceId = activity?.TraceId.ToHexString(), spanId = activity?.SpanId.ToHexString(), parentSpanId = activity?.ParentSpanId.ToHexString() }) + Environment.NewLine);
        }
        catch { }
    }
}
'''
projects={};files={};serial=0

def owner(project):
 if project not in projects:
  classname='Timing'+project.replace('.','')
  hp=OUT/(classname+'.cs');hp.write_text(helper.replace('CLASS',classname).replace('DIRECTORY',str(OUT)).replace('COMPONENT',project))
  projects[project]={'helper':hp,'type':'global::Hexalith.PrivateFocusedTiming.'+classname,'sources':[]}
 return projects[project]

def file(repo,project,relative,compiled=None):
 original=ROOT/('references/'+repo if repo else '')/'src'/project/relative
 key=str(original)
 if key not in files:files[key]={'path':original,'original':original.read_text(),'text':original.read_text(),'owner':owner(compiled or project),'relative':('../'+project+'/' if compiled and compiled!=project else '')+relative}
 return files[key]

def inject(f,method,label,root=False):
 global serial
 serial+=1;s=f['text'];pattern=re.compile(r'\b(?:public|private|internal|protected)\s+[^\n;{}=]*?\b'+re.escape(method)+r'\s*\(')
 matches=list(pattern.finditer(s));assert len(matches)==1,(f['path'],method,len(matches))
 start=matches[0].end()-1;depth=0;end=None
 for index in range(start,len(s)):
  if s[index]=='(':depth+=1
  elif s[index]==')':
   depth-=1
   if depth==0:end=index+1;break
 assert end is not None
 brace=s.index('{',end);assert '=>' not in s[end:brace] and ';' not in s[end:brace],(f['path'],method,'expression-bodied')
 call=f['owner']['type']+('.Advisory()' if root else '.Start("'+label+'")')
 f['text']=s[:brace+1]+'\n        using var __focus'+str(serial)+' = '+call+';'+s[brace+1:]

def statement(f,anchor,label,allow_first=False):
 global serial
 serial+=1;s=f['text'];assert (s.count(anchor)>=1 if allow_first else s.count(anchor)==1),(f['path'],anchor,s.count(anchor));start=s.index(anchor);end=s.index('.ConfigureAwait(false);',start)+len('.ConfigureAwait(false);');name='__focus'+str(serial)
 f['text']=s[:start]+'using var '+name+' = '+f['owner']['type']+'.Start("'+label+'");\n        '+s[start:end]+'\n        '+name+'?.Dispose();'+s[end:]

def block(f,marker,label):
 global serial
 s=f['text'];positions=[m.start() for m in re.finditer(re.escape(marker),s)];assert positions,(f['path'],marker)
 for start in reversed(positions):
  brace=s.index('{',start);serial+=1;s=s[:brace+1]+'\n            using var __focus'+str(serial)+' = '+f['owner']['type']+'.Start("'+label+'");'+s[brace+1:]
 f['text']=s

f=file(None,'Hexalith.Projects.Server','Queries/ProjectAdvisoryQueryExecutor.cs');inject(f,'ExecuteAsync','advisory.root',True)
f=file('Hexalith.EventStore','Hexalith.EventStore.DomainService','DelegatedOwnerQueryClient.cs');inject(f,'InvokeAsync','owner.client');inject(f,'InvokeOwnerAsync','owner.operation');statement(f,'using HttpResponseMessage response = await clients.CreateClient().SendAsync','owner.client-send')
f=file('Hexalith.EventStore','Hexalith.EventStore','Controllers/QueriesController.cs','Hexalith.EventStore.Gateway');inject(f,'SubmitBoundOwnerAuthorityAsync','gateway.owner')
f=file('Hexalith.EventStore','Hexalith.EventStore','Queries/DaprDomainQueryInvoker.cs','Hexalith.EventStore.Gateway');inject(f,'InvokeAsync','gateway.invoker');statement(f,'using HttpResponseMessage httpResponse = await httpClient.SendAsync','gateway.owner-send')
f=file('Hexalith.EventStore','Hexalith.EventStore','Controllers/StreamsController.cs','Hexalith.EventStore.Gateway');inject(f,'ReadStreamCoreAsync','gateway.native-stream');statement(f,'metadataPair = await NativeSourceReadDiagnostics.ObservePair','gateway.actor-pair-await')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogAuthority.cs')
for method,label in [('AcquireAsync','catalog.acquire'),('AcquireCoreAsync','catalog.acquire-core'),('ValidateAsync','catalog.validate'),('ValidateNativeReadLeaseAsync','catalog.native-revalidate'),('GetHeldMetadataDispatchAsync','catalog.held-metadata'),('ReadAdmissionStateAsync','catalog.state-read'),('ReleaseAsync','catalog.release-with-gate'),('ReleaseCoreAsync','catalog.release-core')]:inject(f,method,label)
for name,label in [('CatalogAdmissionWait','catalog.admission-wait'),('CatalogActivationDigest','catalog.binding-digest'),('CatalogHostCapture','catalog.host-capture'),('CatalogTrust','catalog.trust'),('CatalogCompareExchange','catalog.cas')]:block(f,'using (var stage = diagnostics?.Start(DispatchStageName.'+name+'))',''+label)
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogActivationBinding.cs');inject(f,'ComputeDigest','binding.compute');inject(f,'ComputeDigestCore','binding.core')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogBindingInput.cs');inject(f,'TryCapture','binding.capture')
f=file('Hexalith.EventStore','Hexalith.EventStore.Contracts','Routing/RouteCatalogCodec.cs');inject(f,'Decode','codec.decode')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/ObservedRouteCatalogHost.cs');inject(f,'ObserveAsync','host.observe');statement(f,'RouteCatalogHostObservation actual = await capture','host.capture')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/DevelopmentRouteCatalogInputs.cs');inject(f,'CaptureAsync','host.configuration-inputs')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/TechnicalHttpRouteCatalogReceiver.cs');inject(f,'Observe','technical.observe');inject(f,'ConcreteGrants','technical.grants');inject(f,'InvokeNativeAsync','native.receiver')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/TechnicalHttpObservationPass.cs');inject(f,'Complete','technical.complete');inject(f,'Schema','technical.schema-total');f['text']=f['text'].replace('.Start("technical.schema-total")','.Counted("technical.schema-total")')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/TechnicalHttpActionObservation.cs')
# Select only the overload with the real fresh image read.
needle='internal static string AssemblyIdentity(Assembly assembly, ArrayPool<byte> imageBuffers)';s=f['text'];a=s.index(needle);b=s.index('{',a);serial+=1;f['text']=s[:b+1]+'\n        using var __focus'+str(serial)+' = '+f['owner']['type']+'.Start("technical.image-read-hash");'+s[b+1:]
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/TechnicalHttpRouteTemplate.cs');inject(f,'Matches','technical.template-match');f['text']=f['text'].replace('.Start("technical.template-match")','.Counted("technical.template-match")')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogInvocationHandler.cs');inject(f,'SendAsync','transport.guarded-send')
# A named local function marks the actual downstream transport without recording its destination.
s=f['text'];needle='async Task<HttpResponseMessage> SendTransportAsync(CancellationToken token)';a=s.index(needle);b=s.index('{',a);serial+=1;f['text']=s[:b+1]+'\n            using var __focus'+str(serial)+' = '+f['owner']['type']+'.Start("transport.actual-send");'+s[b+1:]
f=file('Hexalith.EventStore','Hexalith.EventStore.Client','Streams/GatewayNativeSourceMetadataReader.cs');inject(f,'ReadAsync','native.client')
f=file('Hexalith.EventStore','Hexalith.EventStore.Server','Actors/AggregateActor.SourceProof.cs')
for method,label in [('InspectSourceMetadataPairAsync','actor.pair'),('InspectSourceProofAsync','actor.inspect'),('ClearSourceReadCacheAsync','actor.cache-clear')]:inject(f,method,label)
statement(f,'ConditionalValue<AggregateMetadata> stored = await StateManager\n            .TryGetStateAsync<AggregateMetadata>(identity.MetadataKey)','actor.metadata-state-read',allow_first=True)
actor_dir=ROOT/'references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors'
barrier=[p for p in actor_dir.glob('AggregateActor*.cs') if re.search(r'(?:private|internal|public)\s+[^\n]+\bEnsureStateCacheBarrierAsync\s*\(',p.read_text())];assert len(barrier)==1,barrier
f=file('Hexalith.EventStore','Hexalith.EventStore.Server','Actors/'+barrier[0].name);inject(f,'EnsureStateCacheBarrierAsync','actor.cache-barrier')
f=file('Hexalith.Folders','Hexalith.Folders.Server','Queries/FolderOwnerReadQueryHandler.cs');inject(f,'ExecuteAsync','folders.owner');statement(f,'LayeredFolderAuthorizationResult result = await authorization.AuthorizeCurrentMetadataReadAsync','folders.authorize-first');statement(f,'LayeredFolderAuthorizationResult final = await authorization.AuthorizeCurrentMetadataReadAsync','folders.authorize-final')
f=file('Hexalith.Folders','Hexalith.Folders','Authorization/LayeredFolderAuthorizationService.cs');inject(f,'AuthorizeCoreAsync','folders.authority-core');statement(f,'folderEvidence = await folderPermissionEvidenceProvider.GetEvidenceAsync','folders.evidence')
# Open collection in the private test fixture only, immediately before genuine advisory readiness.
p=ROOT/'tests/Hexalith.Projects.Integration.Tests/SupportedProjectReadDistributedFixture.cs';f={'path':p,'original':p.read_text(),'text':p.read_text(),'owner':owner('Hexalith.Projects.Integration.Tests'),'relative':p.name};files[str(p)]=f
anchor='private async Task WaitForBrowserAdvisoryReadinessAsync(string credential)';s=f['text'];a=s.index(anchor);b=s.index('{',a);f['text']=s[:b+1]+'\n        '+f['owner']['type']+'.Open();'+s[b+1:]
manifest={};patch=[]
for f in files.values():
 target=OUT/(f['owner']['helper'].stem+'-'+f['path'].name);target.write_text(f['text']);f['owner']['sources'].append((f['relative'],target))
 manifest[str(f['path'].relative_to(ROOT))]={'sourceSha256':hashlib.sha256(f['path'].read_bytes()).hexdigest(),'privateSha256':hashlib.sha256(target.read_bytes()).hexdigest(),'privatePath':str(target)}
 revision=subprocess.check_output(['git','rev-parse','HEAD'],cwd=f['path'].parent,text=True).strip()
 patch.extend(difflib.unified_diff(f['original'].splitlines(True),f['text'].splitlines(True),fromfile=str(f['path'].relative_to(ROOT))+' at '+revision,tofile=str(target)))
lines=['<Project>']
for project,v in projects.items():
 lines+=['<Target Name="Focused'+project.replace('.','')+'" BeforeTargets="CoreCompile" Condition="\'$(MSBuildProjectName)\' == \''+project+'\'">','<ItemGroup>']
 for rel,path in v['sources']:lines+=['<Compile Remove="'+rel+'" />','<Compile Include="'+str(path)+'" />']
 lines+=['<Compile Include="'+str(v['helper'])+'" />','</ItemGroup>','</Target>']
lines+=['</Project>'];(OUT/'focused.targets').write_text('\n'.join(lines)+'\n');(OUT/'source-inputs.json').write_text(json.dumps(manifest,indent=2)+'\n');(OUT/'source.patch').write_text(''.join(patch))
print(json.dumps({'privateDirectory':str(OUT),'replacedFiles':len(files),'owningAssemblies':len(projects),'sampleAdvisories':3,'collection':'gate disabled throughout startup/seeding, enabled by test-owned readiness method, closed after three Projects advisory roots','recordCapPerProcessAssembly':250000}))
