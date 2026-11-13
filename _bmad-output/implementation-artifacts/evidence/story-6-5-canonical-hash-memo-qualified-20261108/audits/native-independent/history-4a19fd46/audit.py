"""Closed native qualification audit only. No build, host, signals or source mutation."""
from pathlib import Path
import collections,datetime,hashlib,json,re,xml.etree.ElementTree as ET
ROOT=Path('/home/administrator/projects/hexalith/projects');OWNER=ROOT/'references/Hexalith.EventStore';BASE=Path('/tmp/story65-canonical-hash-native-20261108');FINAL=BASE/'final';OUT=Path(__file__).parent
assert(FINAL/'completed-receipt.json').is_file(),'pipeline-must-close-before-audit'
bound={}
def sha(p):
 with Path(p).open('rb')as f:return hashlib.file_digest(f,'sha256').hexdigest()
def read(p):
 p=Path(p);bound[str(p)]=sha(p);return json.loads(p.read_bytes())
r=read(FINAL/'completed-receipt.json');plan=read(Path('/tmp/story65-canonical-hash-validation-plan-20261108/commands-plan.json'));input_receipt=read(FINAL/'input-receipt.json');controls=read(BASE/'source-controls.json')['sourceFilesSha256'];before=read(FINAL/'source65-before.json');after=read(FINAL/'source65-after.json')
assert r['completed']and not r['featureAcceptance']and r['nativeTotal']==1076 and r['ownerCases']==450 and r['projectsCases']==626 and r['oldOwnerRetained']==422 and r['newCases']==23 and r['unchangedAdditionalCases']==5
assert len(controls)==65 and before==after==controls and all(sha(ROOT/p)==h for p,h in controls.items())
assert input_receipt['runnerSha256']=='0870cb349d8d8112db72d167759739f30678791fcdba29a8ccb926468299b61f'and sha('/tmp/story65-canonical-hash-native-remaining-prep-20261108/run_remaining.py')==input_receipt['runnerSha256']
assert input_receipt['mappingEnvironment']==plan['sourceMappingEnvironmentExactSeven']and len(input_receipt['mappingEnvironment'])==7
commands=r['commands'];assert len(commands)==28
for x in commands:
 assert x['exitCode']==0 and not x['timedOut']and x['processGroupAbsent']and x['pidAbsent']and not x.get('cleanupSignals')
 record=read(FINAL/(x['label']+'.json'));assert all(x[k]==v for k,v in record.items())
 assert sha(FINAL/(x['label']+'.log'))==x['logSha256']
 if x['buildCpuOverride']is not None:
  assert x['buildCpuOverride']=='2';log=(FINAL/(x['label']+'.log')).read_text(errors='replace');assert re.search(r'\b0 Warning\(s\)',log)and re.search(r'\b0 Error\(s\)',log)and not re.search(r'\b[1-9]\d* (Warning|Error)\(s\)',log)
 else:assert'-xml'in x['argv']
assert sum(x['label'].startswith('01-')for x in commands)==4 and sum(x['label'].startswith('02-')for x in commands)==14 and sum(x['label'].startswith('03-')for x in commands)==5
for x in commands[:23]:
 assert '-p:EnablePrivateCatalogInvocationProof=true'in x['argv']and '-p:EnablePrivateOwnerSourceReadProof=true'in x['argv']
for x in commands[4:23]:assert '-p:BuildProjectReferences=false'in x['argv']
def identities(p):
 p=Path(p);raw=p.read_bytes();assert b'<!DOCTYPE'not in raw and b'<!ENTITY'not in raw;bound[str(p)]=sha(p);xml=ET.fromstring(raw);ts=list(xml.iter('test'));assert ts and all(t.get('result')=='Pass'for t in ts)
 for a in xml.iter('assembly'):assert all(int(a.get(k,'0'))==0 for k in ['failed','errors','skipped','not-run'])
 return collections.Counter((t.get('type'),t.get('method'),t.get('name'))for t in ts)
reference=read(FINAL/'reference-xml-pins.json');assert all(sha(p)==h for p,h in reference.items())
expected_owner=collections.Counter()
for p in [Path('/tmp/story65-memo-rw-owner-native-20261107/new-focused.xml'),Path('/tmp/story65-memo-rw-owner-native-20261107/regression412.xml'),BASE/'smoke/new23.xml',BASE/'smoke/existing5.xml']:expected_owner+=identities(p)
assert sum(expected_owner.values())==450 and identities(FINAL/'owner450.xml')==expected_owner
xmls=[]
for x in commands[23:]:
 actual=identities(x['xml']);assert sum(actual.values())==x['passingCases']and sha(x['xml'])==x['xmlSha256']and x['exactExpectedCaseNames']
 if x['label']!='04-owner450':assert actual==identities(Path('/tmp/story65-memo-rw-native-20261107')/Path(x['xml']).name)
 xmls.append({'label':x['label'],'xmlSha256':x['xmlSha256'],'executions':sum(actual.values()),'distinctRenderedIdentities':len(actual),'maximumRenderedMultiplicity':max(actual.values())})
assert [v['executions']for v in xmls]==[450,211,324,46,45]
producer=r['producerFilesSha256'];assert len(producer)==8 and all(sha(p)==h for p,h in producer.items())and producer==read(FINAL/'final-producers-before-test-refresh.json')
outputs=read(FINAL/'native-outputs-before-tests.json');assert len(outputs)==20 and outputs==r['nativeTestOutputsSha256']and all(sha(p)==h for p,h in outputs.items())
copyproof=read(FINAL/'native-producer-copy-proof.json');assert copyproof['producerFilesSha256']==producer and copyproof['unchangedByTestRefresh']and copyproof['noManualDllCopies']and copyproof['copies']==r['copies']
for c in r['copies']:assert c['matches']and sha(c['path'])==c['sha256']==sha(c['producerPath'])
manifest=read(Path('/tmp/story65-canonical-hash-memo-prep-20261108/preparation-receipt.json'));app={}
markers=['Hexalith.PrivateCaptureCounter','TimingHexalith','PrivateFocusedTiming','Story65Timing']
def clean(p):
 data=Path(p).read_bytes();assert all(m.encode()not in data and m.encode('utf-16-le')not in data for m in markers)
for rel in manifest['app147CurrentSha256']:
 p=ROOT/rel;clean(p);app[str(p)]=sha(p)
assert len(app)==147
active=[]
for rel in manifest['app147CurrentSha256']:
 if not rel.endswith('/Hexalith.EventStore.ServiceDefaults.dll'):continue
 base=(ROOT/rel).parent;row={'directory':str(base),'filesSha256':{}}
 for name in ['Hexalith.EventStore.Contracts','Hexalith.EventStore.ServiceDefaults']:
  for ext in ['dll','pdb']:
   p=base/(name+'.'+ext);origin=OWNER/'src'/name/'bin/Debug/net10.0'/p.name;assert sha(p)==producer[str(origin)];row['filesSha256'][str(p)]=sha(p)
 active.append(row)
assert len(active)==8
inventory=read(Path('/tmp/story65-domain-receiver-phase-ops-prep-20261108/affected-inventory.json'));sdk=[]
for x in inventory['allSdkBaseline']:
 p=ROOT/x['path'];clean(p);sdk.append({'path':str(p),'sha256':sha(p),'historicalBaselineSha256':x['sha256'],'historicalByteEqualityClaimed':False})
assert len(sdk)==137
remaining=[];perms=0;expected_ids={x['pid']:str(x['startTicks'])for x in commands};groups=set(expected_ids)
for p in Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:
  f=(p/'stat').read_text().rsplit(')',1)[1].split();pid=int(p.name)
  if pid in expected_ids and f[19]==expected_ids[pid]or int(f[2])in groups or int(f[3])in groups:remaining.append(pid)
 except(FileNotFoundError,ProcessLookupError):pass
 except PermissionError:perms+=1
assert not remaining and not perms
report={'passed':True,'diagnosticOnly':False,'featureAcceptance':False,'createdUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'auditSourceSha256':sha(__file__),'bindingsSha256':bound,'nativeExecutions':1076,'owner450':450,'projects626':626,'newNativeCases':23,'retainedOwner422':422,'unchangedAdditionalHostByteMemoCases':5,'xmlCaseMultisetsExactlyMatchPinnedReferences':True,'xmlCounts':xmls,'zeroErrorsFailuresSkipsNotRun':True,'source65BeforeAfterCurrentAllMatch':True,'actualCommands':28,'projectsBuilds':4,'ordinaryCarrierBuilds':14,'nativeRefsFalseRefreshes':5,'all23BuildsZeroWarningsErrors':True,'allNativeNaturalProcessorNoOverride':True,'mappingExactSeven':input_receipt['mappingEnvironment'],'bothExistingProofFlagsAllBuilds':True,'all28PidStartGroupAndSessionIdentitiesFreshlyAbsentIncludingZombieCheck':True,'freshProcPermissionErrors':0,'noCleanupSignals':True,'finalProducer8DllPdbRefRefintSha256':producer,'native20TestOutputsUnchangedSha256':outputs,'nativeProducerCopyProofCount':len(r['copies']),'allNativeProducerCopiesExact':True,'eightActiveEntryAndTestCopiesContractsSdkDllPdbCoherent':active,'markerStringsChecked':markers,'markerEncodingsChecked':['UTF-8/ASCII','UTF-16LE'],'sdk137MarkerFreeCurrentCensus':sdk,'app147MarkerFreeCurrentSha256':app,'historicalGenerationLimitsRetained':r['historicalGenerationLimits'],'notOld1048HistoricalBinaryEquality':True,'notRetrospective10Or17GenerationProvenanceWaiver':True,'native1076ExcludesScratch178AndNoLiveAcceptanceCount':True,'limits':['These native executions qualify the exact current compiled source/profile and selected test cases; they do not establish Story6.5 browser/MULTI acceptance or host deadline benefit.','The current8SDK/Contracts active-copy scope and20nativecopy rows are exact current-producer coherence; inactive137union rows are marker-free/current-hashed only, without all-generation historical byte/source equivalence.','No new apphost or profiling execution or repository edits were performed by this audit.']}
p=OUT/'independent-native-audit.json';p.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'path':str(p),'sha256':sha(p),'nativeExecutions':1076,'source65':65,'commands':28,'coreProducerArtifacts':8,'nativeCopyRows':len(r['copies']),'sdkRows':137,'appRows':147},indent=2))
