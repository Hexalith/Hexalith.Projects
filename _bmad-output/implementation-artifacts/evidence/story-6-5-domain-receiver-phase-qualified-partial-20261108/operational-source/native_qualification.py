from pathlib import Path
import argparse,collections,datetime,json,os,re,subprocess,tempfile,time,xml.etree.ElementTree as ET
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
project=ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj';out=project.parent/'bin/Debug/net10.0';testdll=out/'Hexalith.EventStore.Server.Tests.dll';reference=json.loads((PREP/'native-reference-case-set.json').read_text());expected_total=reference['expectedTotal']
assert all(sha(ROOT/path)==digest for path,digest in reference['fixtureSourceSha256'].items())
assert all(sha(ROOT/row['path'])==row['sha256']for row in reference['historicalCorroboration'])
if not args.execute:print(json.dumps({'preparationOnly':True,'sourceDerivedNativeCases':expected_total,'processSelections':reference['selections'],'notClaimedHistoricalOrAdditionalOrdinaryCases':True,'mandatoryPrivateDomainMapEveryProcess':True}));raise SystemExit()
operational_pins(args.pins_sha256);assert reference['metadataPreparationComplete']is True and expected_total==103;sources();private_directory(require_empty=True);domain_proof('private-primary');sdk_proof('private-primary');independent_dependencies('rebuilt-primary')
work=Path(tempfile.mkdtemp(prefix='native-private-domain-receiver-',dir=PREP));save(PREP/'latest-native-attempt.json',{'attemptDirectory':str(work)})
receipt={'diagnosticOnly':True,'featureAcceptance':False,'passed':False,'nativeStarted':False,'nativeProcessAndDescendantsAbsent':True,'counterDirectoryEmpty':True,'caseCount':0,'sourceDerivedExpectedCases':expected_total,'historicalAllSelectionRetentionClaimed':False,'ordinaryUniqueNativeCountUnchanged':1048,'processes':[]};env=clean_environment();env.update(DOTNET_PROCESSOR_COUNT='2',HexalithBuildsRoot=str(ROOT/'references/Hexalith.Builds'))
mapping=json.loads((PREP/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(n)==h for n,h in mapping['files'].items());assert set(mapping['environment'])==MAPPING;env.update(mapping['environment'])
def members(group):
 found=[]
 for p in Path('/proc').iterdir():
  if not p.name.isdigit():continue
  try:
   x=(p/'stat').read_text().rsplit(')',1)[1].split()
   if int(x[2])==group and x[0]!='Z':found.append({'pid':int(p.name),'startTicks':x[19]})
  except(FileNotFoundError,PermissionError,ProcessLookupError):pass
 return found

def same(pid,ticks):
 try:
  x=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return x[0]!='Z'and x[19]==ticks
 except(FileNotFoundError,ProcessLookupError):return False

def canonical(t):
 return(t.get('type'),t.get('method'),hashlib.sha256((t.get('name')or'').encode()).hexdigest())
def expected_counter(selection):
 result=collections.Counter()
 for entry in selection['expectedDisplayMultiset']:
  key=(entry['type'],entry['method'],entry['parsedXmlNameSha256']);assert key not in result and type(entry['multiplicity'])is int and entry['multiplicity']>0;result[key]=entry['multiplicity']
 assert sum(result.values())==selection['expectedExecutions'];return result
def formatter_default_guard():
 assert os.environ.get('XUNIT_PRINT_MAX_STRING_LENGTH')is None and env.get('XUNIT_PRINT_MAX_STRING_LENGTH')is None
 for folder in [project.parent,out,project.parent.parent,project.parent.parent.parent,ROOT]:
  for pattern in ['*xunit*.json','*testconfig*.json','*.runsettings']:
   for file in folder.glob(pattern):
    content=file.read_text(errors='strict');assert'printMaxStringLength'not in content and'PrintMaxStringLength'not in content
 return {'specificMaxStringEnvironmentOverrideAbsent':True,'runnerConfigurationStringLimitOverrideAbsent':True,'unchangedDefaultStringLimit':50}
def one(selection,producer,copy):
 child=None;ticks=None;record={'selection':selection,'nativeStarted':False,'processAndDescendantsAbsent':True,'mappedPrivateDomain':False,'passed':False};receipt['processes'].append(record)
 try:
  record['formatterGuard']=formatter_default_guard()
  runtime_env=env.copy();runtime_env.pop('DOTNET_PROCESSOR_COUNT',None);assert'DOTNET_PROCESSOR_COUNT'not in runtime_env
  record.update(nativeRuntimeProcessorCountOverrideAbsent=True,nativeRuntimeCpuUnmodified=True,buildOnlyProcessorCount='2')
  xml=work/(selection['alias']+'.xml');command=['dotnet',str(testdll),selection['flag'],selection['filter'],'-xml',str(xml)];record.update(command=command,cwd=str(project.parent),startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
  with(work/(selection['alias']+'.log')).open('wb')as stream:
   child=subprocess.Popen(command,cwd=project.parent,env=runtime_env,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
   # Set absence false immediately after spawn, before any fallible /proc read or receipt write.
   record.update(nativeStarted=True,processAndDescendantsAbsent=False,pid=child.pid,processGroup=child.pid,startTicks=None);receipt['nativeStarted']=True;receipt['nativeProcessAndDescendantsAbsent']=False;save(work/'receipt.json',receipt)
   ticks=Path('/proc',str(child.pid),'stat').read_text().rsplit(')',1)[1].split()[19];record['startTicks']=ticks;save(work/'receipt.json',receipt)
   while child.poll()is None:
    try:
     state=Path('/proc',str(child.pid),'stat').read_text().rsplit(')',1)[1].split()
     if state[19]==ticks and state[0]!='Z':
      paths={l.split(maxsplit=5)[-1]for l in Path('/proc',str(child.pid),'maps').read_text().splitlines()if len(l.split(maxsplit=5))==6}
      for alias,file in [('test-copy',copy),('producer',producer)]:
       if str(file)in paths and sha(file)==receipt['privateDomainBefore']['producerSha256']and marker(file)and same(child.pid,ticks):record['mappedPrivateDomain']=True;record['acceptedMappedAlias']=alias
    except(FileNotFoundError,PermissionError,ProcessLookupError):pass
    time.sleep(.002)
   code=child.wait()
  record.update(exitCode=code,finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),xmlSha256=sha(xml)if xml.exists()else None,logSha256=sha(work/(selection['alias']+'.log')))
  assert code==0 and record['mappedPrivateDomain']and xml.exists()
  root=ET.parse(xml).getroot();tests=list(root.iter('test'));actual=[canonical(t)for t in tests];expected=expected_counter(selection);assemblies=root.findall('assembly')
  assert len(actual)==selection['expectedExecutions']==sum(expected.values())and collections.Counter(actual)==expected and all(t.get('result')=='Pass'for t in tests)and len(assemblies)==1
  assert all(int(assemblies[0].get(k,'0'))==0 for k in ['errors','failed','skipped','not-run'])
  record.update(passed=True,caseCount=len(tests),actualParsedDisplayIdentityShaTuples=[list(x)for x in actual],exactRenderedNameMultiplicityCounterAccepted=True,noFuzzyNormalization=True,failureBodyValuesPersistedInDerivedReceipt=False,rawXunitXmlAndLogsMayContainSyntheticSourceLiterals=True)
 finally:
  if child is not None:
   record['processAndDescendantsAbsent']=False
   try:
    child.wait();deadline=time.monotonic()+10
    while members(child.pid)and time.monotonic()<deadline:time.sleep(.05)
    record['remainingOwnedGroupIdentities']=members(child.pid);record['processAndDescendantsAbsent']=child.returncode is not None and not record['remainingOwnedGroupIdentities'];record['ownedExitCode']=child.returncode
   except Exception as failure:record['cleanupFailureType']=type(failure).__name__
  if not record['processAndDescendantsAbsent']:record['passed']=False
  save(work/'receipt.json',receipt)
 assert record['passed']and record['processAndDescendantsAbsent']
 return record['actualParsedDisplayIdentityShaTuples']
try:
 command=['dotnet','build',str(project),'-c','Debug','-t:Rebuild','-m:1','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-p:BuildProjectReferences=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true']
 with(work/'build.log').open('wb')as stream:built=subprocess.run(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 receipt['build']={'command':command,'cwd':str(ROOT),'exitCode':built.returncode,'logSha256':sha(work/'build.log'),'allSevenEmittedMappingEnvironmentValues':mapping['environment']};assert built.returncode==0
 producer=PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');copy=out/(ASSEMBLY+'.dll');assert marker(producer)and marker(copy)and sha(copy)==sha(producer)and sha(copy.with_suffix('.pdb'))==sha(producer.with_suffix('.pdb'))
 receipt['privateDomainBefore']={'producerSha256':sha(producer),'producerPdbSha256':sha(producer.with_suffix('.pdb')),'testCopySha256':sha(copy),'testCopyPdbSha256':sha(copy.with_suffix('.pdb')),'testAssemblySha256':sha(testdll)};receipt['domainBefore']=domain_proof('private-with-native');receipt['sdkBefore']=sdk_proof('private-with-native');receipt['dependencyGenerationsBefore']=independent_dependencies();receipt['native147Before']=native147_proof('private-with-native');env.pop('DOTNET_PROCESSOR_COUNT',None)
 allcases=[]
 for selection in reference['selections']:allcases+=one(selection,producer,copy);private_directory(require_empty=True)
 expected_all=collections.Counter()
 for selection in reference['selections']:expected_all.update(expected_counter(selection))
 assert len(allcases)==sum(expected_all.values())==expected_total and collections.Counter(tuple(x)for x in allcases)==expected_all
 sources();assert all(sha(ROOT/path)==digest for path,digest in reference['fixtureSourceSha256'].items());independent_dependencies();domain_proof('private-with-native');sdk_proof('private-with-native');native147_proof('private-with-native');assert sha(producer)==receipt['privateDomainBefore']['producerSha256']and sha(copy)==sha(producer)
 assert sha(producer.with_suffix('.pdb'))==receipt['privateDomainBefore']['producerPdbSha256']and sha(copy.with_suffix('.pdb'))==sha(producer.with_suffix('.pdb'))
 receipt.update(passed=True,caseCount=expected_total,source59Match=True,allSelectedProcessesMappedPrivateDomain=True,actualParsedDisplayIdentityShaTuples=allcases,exactRenderedNameMultiset=True,exactTypeMethodRenderedNameMultiplicityRequired=True)
except Exception as failure:receipt['failureType']=type(failure).__name__
finally:
 receipt['nativeProcessAndDescendantsAbsent']=all(p['processAndDescendantsAbsent']for p in receipt['processes'])
 try:private_directory(require_empty=True);receipt['counterDirectoryEmpty']=True
 except Exception:receipt['counterDirectoryEmpty']=False
 if not receipt['nativeProcessAndDescendantsAbsent']or not receipt['counterDirectoryEmpty']:receipt['passed']=False
 save(work/'receipt.json',receipt);print(json.dumps({'diagnosticOnly':True,'passed':receipt['passed'],'caseCount':receipt['caseCount'],'attemptDirectory':str(work),'nativeProcessAndDescendantsAbsent':receipt['nativeProcessAndDescendantsAbsent']}))
raise SystemExit(0 if receipt['passed']else 1)
