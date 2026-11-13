from pathlib import Path
import argparse,datetime,json,os,subprocess,tempfile,time,xml.etree.ElementTree as ET
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
project=ROOT/'tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj';out=project.parent/'bin/Debug/net10.0';testdll=out/'Hexalith.Projects.Server.Tests.dll'
reference=json.loads((PREP/'native-reference-case-set.json').read_text());assert sha(reference['referenceXml'])==reference['referenceXmlSha256']
if not args.execute:
 print(json.dumps({'preparationOnly':True,'nativeClass':reference['class'],'exactRetainedCases':139,'buildProject':str(project),'BuildProjectReferences':False,'sameSevenSourceMappingAndTwoProofFlags':True,'nativeFailureSkipsManagedAndStillRestoresOrdinary':True}));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True);ordinary_sdk_proof()
work=Path(tempfile.mkdtemp(prefix='native-private-139-',dir=PREP));save(PREP/'latest-native-attempt.json',{'attemptDirectory':str(work)})
receipt={'diagnosticOnly':True,'featureAcceptance':False,'notAdditionalUniqueCasesBeyondExisting1038':True,'passed':False,'buildStarted':False,'nativeStarted':False,'nativeProcessAndDescendantsAbsent':True,'counterDirectoryEmpty':True};process=None
env=clean_environment();env['DOTNET_PROCESSOR_COUNT']='2';env['HexalithBuildsRoot']=str(ROOT/'references/Hexalith.Builds')
mapping=json.loads((PREP/'mapping-pins.json').read_text());assert sha(mapping['mappingLog'])==mapping['mappingLogSha256']and all(sha(path)==digest for path,digest in mapping['files'].items());assert set(mapping['environment'])==MAPPING;env.update(mapping['environment'])
def members(group):
 found=[]
 for entry in Path('/proc').iterdir():
  if not entry.name.isdigit():continue
  try:
   fields=(entry/'stat').read_text().rsplit(')',1)[1].split()
   if int(fields[2])==group and fields[0]!='Z':found.append({'pid':int(entry.name),'startTicks':fields[19]})
  except(FileNotFoundError,PermissionError,ProcessLookupError):pass
 return found
def same(pid,ticks):
 try:
  fields=Path('/proc',str(pid),'stat').read_text().rsplit(')',1)[1].split();return fields[0]!='Z'and fields[19]==ticks
 except FileNotFoundError:return False
try:
 command=['dotnet','build',str(project),'-c','Debug','-t:Rebuild','-m:1','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-p:BuildProjectReferences=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true']
 receipt['buildStarted']=True;started=datetime.datetime.now(datetime.timezone.utc).isoformat()
 with(work/'build.log').open('wb')as stream:built=subprocess.run(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 receipt['build']={'command':command,'cwd':str(ROOT),'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'exitCode':built.returncode,'logSha256':sha(work/'build.log'),'allSevenMappingValues':mapping['environment'],'sameTwoProofFlags':True,'noOverlayHookOnTestBuild':True}
 assert built.returncode==0
 producer=PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');symbols=producer.with_suffix('.pdb');copy=out/(ASSEMBLY+'.dll');pdb=copy.with_suffix('.pdb')
 assert marker(producer)and marker(copy)and sha(copy)==sha(producer)and sha(pdb)==sha(symbols)
 receipt['privateServerBefore']={'producerSha256':sha(producer),'producerPdbSha256':sha(symbols),'testCopySha256':sha(copy),'testCopyPdbSha256':sha(pdb),'privateMarkersPresent':True,'testAssemblySha256':sha(testdll)};receipt['ordinarySdkBefore']=ordinary_sdk_proof()
 # Natural runtime CPU; only the preceding build uses the two-core build limiter.
 env.pop('DOTNET_PROCESSOR_COUNT',None)
 xml=work/'native-139.xml';command=['dotnet',str(testdll),'-class',reference['class'],'-xml',str(xml)];started=datetime.datetime.now(datetime.timezone.utc).isoformat();mapped=False;mapped_aliases=set();identity=None;observed_group=[]
 with(work/'native.log').open('wb')as stream:
  process=subprocess.Popen(command,cwd=project.parent,env=env,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
  receipt['nativeStarted']=True;receipt['nativeProcessAndDescendantsAbsent']=False
  identity={'pid':process.pid,'startTicks':None,'processGroup':process.pid};save(work/'native-identity.json',identity)
  ticks=Path('/proc',str(process.pid),'stat').read_text().rsplit(')',1)[1].split()[19];identity['startTicks']=ticks;save(work/'native-identity.json',identity)
  while process.poll()is None:
   if same(process.pid,ticks):
    try:
     lines=Path('/proc',str(process.pid),'maps').read_text().splitlines();paths={line.split(maxsplit=5)[-1]for line in lines if len(line.split(maxsplit=5))==6}
     for alias,candidate in[('test-copy',copy),('producer',producer)]:
      if str(candidate)in paths and sha(candidate)==receipt['privateServerBefore']['producerSha256']and marker(candidate)and same(process.pid,ticks):mapped=True;mapped_aliases.add(alias)
    except(FileNotFoundError,ProcessLookupError,PermissionError):pass
   observed_group=members(process.pid);time.sleep(.01)
  exitcode=process.wait()
 deadline=time.monotonic()+10
 while members(process.pid)and time.monotonic()<deadline:time.sleep(.1)
 remaining=members(process.pid);gone=not same(process.pid,ticks)and not remaining;receipt['nativeProcessAndDescendantsAbsent']=gone
 receipt['native']={'command':command,'cwd':str(project.parent),'startedUtc':started,'finishedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'identity':identity,'exitCode':exitcode,'logSha256':sha(work/'native.log'),'xmlSha256':sha(xml)if xml.exists()else None,'privateServerObservedMapped':mapped,'acceptedMappedAliases':sorted(mapped_aliases),'mappedObservationRequired':True,'processAndDescendantsAbsent':gone,'remainingOwnedGroupIdentities':remaining,'targetOrGroupSignalsSent':False}
 assert gone and exitcode==0 and xml.exists()and mapped
 root=ET.parse(xml).getroot();assemblies=root.findall('assembly');tests=list(root.iter('test'));actual=[(item.get('type'),item.get('method'),item.get('name'))for item in tests]
 assert len(tests)==139 and len(set(actual))==139 and set(actual)=={tuple(item)for item in reference['canonicalCaseSet']}
 assert all(item.get('result')=='Pass'for item in tests)and len(assemblies)==1
 assert all(int(assemblies[0].get(key,'0'))==0 for key in['errors','failed','skipped','not-run'])and int(assemblies[0].get('passed','0'))==139
 sources();receipt['ordinarySdkAfter']=ordinary_sdk_proof();private_directory(require_empty=True)
 assert sha(producer)==receipt['privateServerBefore']['producerSha256']and sha(symbols)==receipt['privateServerBefore']['producerPdbSha256']and sha(copy)==sha(producer)and sha(pdb)==sha(symbols)
 receipt.update(passed=True,caseCount=139,exactHistoricalCaseSetRetained=True,source50Match=True,counterDirectoryEmpty=True)
except Exception as failure:
 receipt['failureType']=type(failure).__name__
 receipt['counterDirectoryEmpty']=not any(PRIVATE.iterdir())
finally:
 if process is not None:
  # Independent cleanup proof on every post-spawn path; never abandon or signal this owned process.
  receipt['nativeProcessAndDescendantsAbsent']=False
  try:
   process.wait();deadline=time.monotonic()+10
   while members(process.pid)and time.monotonic()<deadline:time.sleep(.1)
   receipt['nativeProcessAndDescendantsAbsent']=process.returncode is not None and not members(process.pid)
   receipt['ownedNativeExitCode']=process.returncode
  except Exception as failure:receipt['cleanupFailureType']=type(failure).__name__
  if not receipt['nativeProcessAndDescendantsAbsent']:receipt['passed']=False
 try:private_directory(require_empty=True);receipt['counterDirectoryEmpty']=True
 except Exception:receipt['counterDirectoryEmpty']=False;receipt['passed']=False
 save(work/'receipt.json',receipt);print(json.dumps({'diagnosticOnly':True,'passed':receipt['passed'],'attemptDirectory':str(work),'nativeProcessAndDescendantsAbsent':receipt['nativeProcessAndDescendantsAbsent'],'counterDirectoryEmpty':receipt['counterDirectoryEmpty']}))
raise SystemExit(0 if receipt['passed']else 1)
