from pathlib import Path
import re,json,hashlib,difflib,subprocess
ROOT=Path('/home/administrator/projects/hexalith/projects');OUT=Path('/tmp/story65-owner-domain-overlay-20261107');OUT.mkdir(mode=0o700,exist_ok=True)
helper='using System.Diagnostics;\nusing System.Text.Json;\nnamespace Hexalith.PrivateFocusedTiming;\ninternal sealed class CLASS : IDisposable\n{\n    private static readonly object Sync = new();\n    private static readonly AsyncLocal<long> Current = new();\n    private static long Sequence;\n    private static int Records;\n    private static int Samples;\n    private static readonly Dictionary<(long Parent, string Stage), (long Count, long Total, long Units, long Min, long Max, long First, long Last)> Counts = new();\n    private readonly long Id = Interlocked.Increment(ref Sequence);\n    private readonly long Parent = Current.Value;\n    private readonly long Started = Stopwatch.GetTimestamp();\n    private readonly string Stage;\n    private readonly bool Root;\n    private readonly bool Aggregate;\n    private int Closed;\n    private CLASS(string stage, bool root = false, bool aggregate = false) { Stage = stage; Root = root; Aggregate = aggregate; if (!aggregate) { Current.Value = Id; Write("begin", 0); } }\n    internal static CLASS? Start(string stage) => File.Exists("DIRECTORY/collect.enabled") ? new(stage) : null;\n    internal static CLASS? Counted(string stage) => Current.Value != 0 ? new(stage, aggregate: true) : null;\n    internal static CLASS? Advisory() => File.Exists("DIRECTORY/collect.enabled") && Interlocked.Increment(ref Samples) <= 3 ? new("advisory.root", true) : null;\n    internal static void Open() { try { File.WriteAllText("DIRECTORY/collect.enabled", "enabled"); } catch { } }\n    internal static void Counter(string stage, long units = 0)\n    {\n        if (Current.Value == 0) return;\n        try { Add(Current.Value, stage, 0, units, Stopwatch.GetTimestamp()); } catch { }\n    }\n    private static void Add(long parent, string stage, long elapsed, long units, long started)\n    {\n        lock (Sync) { var key = (parent, stage); Counts.TryGetValue(key, out var old); long now = Stopwatch.GetTimestamp();\n            Counts[key] = (old.Count + 1, old.Total + elapsed, old.Units + units, old.Count == 0 ? units : Math.Min(old.Min, units), old.Count == 0 ? units : Math.Max(old.Max, units), old.Count == 0 ? started : old.First, now); }\n    }\n    public void Dispose()\n    {\n        if (Interlocked.Exchange(ref Closed, 1) != 0) return;\n        if (Aggregate)\n        {\n            try { Add(Parent, Stage, Stopwatch.GetTimestamp() - Started, 0, Started); } catch { }\n            return;\n        }\n        Write("end", Stopwatch.GetTimestamp() - Started); Current.Value = Parent;\n        try { lock (Sync)\n        {\n            foreach (var entry in Counts.Where(entry => entry.Key.Parent == Id).ToArray())\n            {\n                if (Interlocked.Increment(ref Records) <= 250000)\n                    File.AppendAllText("DIRECTORY/COMPONENT-" + Environment.ProcessId + ".jsonl", JsonSerializer.Serialize(new { component = "COMPONENT", pid = Environment.ProcessId, stage = entry.Key.Stage, state = "aggregate", parent = Id, count = entry.Value.Count, elapsedTicks = entry.Value.Total, units = entry.Value.Units, minUnits = entry.Value.Min, maxUnits = entry.Value.Max, firstTicks = entry.Value.First, lastTicks = entry.Value.Last, frequency = Stopwatch.Frequency }) + Environment.NewLine);\n                Counts.Remove(entry.Key);\n            }\n        } } catch { }\n        if (Root && Volatile.Read(ref Samples) >= 3)\n        {\n            try { File.Delete("DIRECTORY/collect.enabled"); File.WriteAllText("DIRECTORY/sample-complete", "3"); } catch { }\n        }\n    }\n    private void Write(string state, long elapsed)\n    {\n        try\n        {\n            if (Interlocked.Increment(ref Records) > 250000) return;\n            var activity = Activity.Current;\n            lock (Sync) File.AppendAllText("DIRECTORY/COMPONENT-" + Environment.ProcessId + ".jsonl",\n                JsonSerializer.Serialize(new { component = "COMPONENT", pid = Environment.ProcessId, stage = Stage, state,\n                    ordinal = Id, parent = Parent, ticks = Stopwatch.GetTimestamp(), elapsedTicks = elapsed, frequency = Stopwatch.Frequency,\n                    traceId = activity?.TraceId.ToHexString(), spanId = activity?.SpanId.ToHexString(), parentSpanId = activity?.ParentSpanId.ToHexString() }) + Environment.NewLine);\n        }\n        catch { }\n    }\n}\n'
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
f=file('Hexalith.EventStore','Hexalith.EventStore.DomainService','DelegatedOwnerQueryClient.cs');inject(f,'InvokeAsync','owner.client');statement(f,'using HttpResponseMessage response = await clients.CreateClient().SendAsync','owner.client-send')
f=file('Hexalith.EventStore','Hexalith.EventStore','Controllers/QueriesController.cs','Hexalith.EventStore.Gateway');inject(f,'SubmitBoundOwnerAuthorityAsync','gateway.owner')
f=file('Hexalith.EventStore','Hexalith.EventStore','Queries/DaprDomainQueryInvoker.cs','Hexalith.EventStore.Gateway');inject(f,'InvokeAsync','gateway.invoker');statement(f,'using HttpResponseMessage httpResponse = await httpClient.SendAsync','gateway.owner-send')
f=file('Hexalith.EventStore','Hexalith.EventStore','Controllers/StreamsController.cs','Hexalith.EventStore.Gateway');inject(f,'ReadStreamCoreAsync','gateway.native-stream');statement(f,'metadataPair = await NativeSourceReadDiagnostics.ObservePair','gateway.actor-pair-await')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogAuthority.cs')
for method,label in [('AcquireAsync','catalog.acquire'),('WaitForLocalAdmissionAsync','catalog.admission-wait'),('AcquireCoreAsync','catalog.acquire-core'),('ValidateAsync','catalog.validate'),('ValidateNativeReadLeaseAsync','catalog.native-revalidate'),('GetHeldMetadataDispatchAsync','catalog.held-metadata'),('ReadAdmissionStateAsync','catalog.state-read'),('ReleaseAsync','catalog.release'),('ReleaseCoreAsync','catalog.release-core')]:inject(f,method,label)
for name,label in [('CatalogActivationDigest','catalog.binding-digest'),('CatalogHostCapture','catalog.host-capture'),('CatalogTrust','catalog.trust'),('CatalogCompareExchange','catalog.cas')]:block(f,'using (var stage = diagnostics?.Start(DispatchStageName.'+name+'))',label)
needle='using var imageComputation = BeginPrivateImageScope();';assert f['text'].count(needle)==3
f['text']=f['text'].replace(needle,needle+'\n            '+f['owner']['type']+'.Counter(imageComputation is null ? "authority.image-scope.absent" : "authority.image-scope.present");')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogActivationBinding.cs');inject(f,'ComputeDigest','binding.compute');inject(f,'ComputeDigestCore','binding.core');f['text']=f['text'].replace('.Start("binding.compute")','.Counted("binding.compute")').replace('.Start("binding.core")','.Counted("binding.core")')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogBindingMemo.cs');inject(f,'TryCompute','binding.memo.total');f['text']=f['text'].replace('.Start("binding.memo.total")','.Counted("binding.memo.total")')
t=f['owner']['type'];s=f['text'];s=s.replace('digest = string.Empty;\n            reused = false;','digest = string.Empty;\n            reused = false;\n            '+t+'.Counter("binding.memo.calls");')
s=s.replace('if (_closed || !RouteCatalogBindingInput.TryCapture(activation, out var captured)) return false;','if (_closed || !RouteCatalogBindingInput.TryCapture(activation, out var captured)) { '+t+'.Counter("binding.memo.reject"); return false; }')
s=s.replace('reused = true;\n                return true;','reused = true;\n                '+t+'.Counter("binding.memo.hit");\n                return true;')
s=s.replace('digest = RouteCatalogActivationBinding.ComputeDigestCore(captured!.Activation);',t+'.Counter("binding.memo.miss");\n            digest = RouteCatalogActivationBinding.ComputeDigestCore(captured!.Activation);');f['text']=s
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogImageDigestMemo.cs');inject(f,'TryCompute','image.memo.total');f['text']=f['text'].replace('.Start("image.memo.total")','.Counted("image.memo.total")')
t=f['owner']['type'];s=f['text'];s=s.replace('digest = string.Empty;\n            reused = false;','digest = string.Empty;\n            reused = false;\n            '+t+'.Counter("image.memo.calls", current.Length);')
s=s.replace('if (_closed || current.Length is 0 or > MaximumImageBytes) return false;','if (_closed || current.Length is 0 or > MaximumImageBytes) { '+t+'.Counter("image.memo.reject"); return false; }')
s=s.replace('reused = true;\n                return true;','reused = true;\n                '+t+'.Counter("image.memo.hit");\n                '+t+'.Counter("image.memo.retained-bytes", _retainedBytes);\n                '+t+'.Counter("image.memo.entries", _images.Count);\n                return true;')
needle='if (!present && _images.Count == MaximumEntries || chargedBytes > MaximumRetainedBytes - (_retainedBytes - replacedBytes)) return false;';assert s.count(needle)==1
s=s.replace(needle,needle.replace('return false;','{ '+t+'.Counter("image.memo.reject"); return false; }'))
s=s.replace('byte[] owned = current.ToArray();',t+'.Counter("image.memo.miss");\n            using var __hash = '+t+'.Counted("image.memo.hash");\n            byte[] owned = current.ToArray();')
s=s.replace('digest = Convert.ToHexStringLower(SHA256.HashData(owned));','digest = Convert.ToHexStringLower(SHA256.HashData(owned));\n            __hash?.Dispose();')
s=s.replace('_retainedBytes += chargedBytes - replacedBytes;','_retainedBytes += chargedBytes - replacedBytes;\n            '+t+'.Counter("image.memo.retained-bytes", _retainedBytes);\n            '+t+'.Counter("image.memo.entries", _images.Count);');f['text']=s
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogImageDigestScope.cs');t=f['owner']['type'];s=f['text'];needle='_memo = memo ?? new();';assert s.count(needle)==1;s=s.replace(needle,needle+'\n        '+t+'.Counter(_ownsMemo ? "image.scope.local-memo" : "image.scope.host-memo");');f['text']=s
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/TechnicalHttpActionObservation.cs');t=f['owner']['type'];s=f['text'];needle='internal static string AssemblyIdentity(Assembly assembly, ArrayPool<byte> imageBuffers)';a=s.index(needle);b=s.index('{',a);s=s[:b+1]+'\n        using var __image = '+t+'.Counted("image.identity");'+s[b+1:]
s=s.replace('using (var image = File.OpenRead(assembly.Location))','using var __read = '+t+'.Counted("image.fresh-read");\n            using (var image = File.OpenRead(assembly.Location))')
s=s.replace('using var pe = new PEReader(', '__read?.Dispose();\n            using var __pe = '+t+'.Counted("image.pe");\n            using var pe = new PEReader(')
s=s.replace('string digest = RouteCatalogImageDigestScope.Current', '__pe?.Dispose();\n            '+t+'.Counter(RouteCatalogImageDigestScope.Current is null ? "image.scope.absent" : "image.scope.present");\n            string digest = RouteCatalogImageDigestScope.Current');f['text']=s
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/ObservedRouteCatalogHost.cs');inject(f,'ObserveAsync','host.observe');statement(f,'RouteCatalogHostObservation actual = await capture','host.capture')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/DevelopmentRouteCatalogInputs.cs');inject(f,'CaptureAsync','host.configuration-inputs')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/FiniteRouteCatalogReceiver.cs');inject(f,'Observe','finite.observe')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/TechnicalHttpRouteCatalogReceiver.cs');inject(f,'Observe','technical.observe');inject(f,'InvokeNativeAsync','native.receiver')
f=file('Hexalith.EventStore','Hexalith.EventStore.ServiceDefaults','Routing/RouteCatalogInvocationHandler.cs');inject(f,'SendAsync','transport.guarded-send')
s=f['text'];needle='async Task<HttpResponseMessage> SendTransportAsync(CancellationToken token)';a=s.index(needle);b=s.index('{',a);serial+=1;f['text']=s[:b+1]+'\n            using var __focus'+str(serial)+' = '+f['owner']['type']+'.Start("transport.actual-send");'+s[b+1:]
f=file('Hexalith.EventStore','Hexalith.EventStore.DomainService','DevelopmentDomainRouteCatalog.cs');inject(f,'Targets','domain.targets');inject(f,'ReadExecutableDigest','domain.executable.total');f['text']=f['text'].replace('.Start("domain.executable.total")','.Counted("domain.executable.total")');t=f['owner']['type'];s=f['text'];needle='configuration, async (provider, token) =>\n        {';assert s.count(needle)==1;s=s.replace(needle,needle+'\n            using var __capture = '+t+'.Start("domain.capture");');needle='Assembly[] implementations = [';assert s.count(needle)==1;s=s.replace(needle,'using var __implementations = '+t+'.Start("domain.implementations");\n            '+needle);needle='.. local.GetServices<IAsyncDomainProjectionHandler>().Select(handler => handler.GetType().Assembly)];';assert s.count(needle)==1;s=s.replace(needle,needle+'\n            __implementations?.Dispose();');f['text']=s
f=file('Hexalith.Folders','Hexalith.Folders.Server','Queries/FolderOwnerReadQueryHandler.cs');inject(f,'ExecuteAsync','folders.owner');statement(f,'LayeredFolderAuthorizationResult result = await authorization.AuthorizeCurrentMetadataReadAsync','folders.authorize-first');statement(f,'LayeredFolderAuthorizationResult final = await authorization.AuthorizeCurrentMetadataReadAsync','folders.authorize-final')
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
