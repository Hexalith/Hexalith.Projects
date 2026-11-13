from pathlib import Path
import argparse,datetime,json,os,re,subprocess,tempfile,time,xml.etree.ElementTree as ET
from ops_common import *
parser=argparse.ArgumentParser();parser.add_argument('--execute',action='store_true');parser.add_argument('--pins-sha256');args=parser.parse_args()
project=ROOT/'references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj';out=project.parent/'bin/Debug/net10.0';testdll=out/'Hexalith.EventStore.Server.Tests.dll';reference=json.loads((PREP/'native-reference-case-set.json').read_text())
assert sha(reference['priorActual19CaseXml'])==reference['priorActual19CaseXmlSha256']
if not args.execute:print(json.dumps({'preparationOnly':True,'sourceDerivedNativeCases':40,'processSelections':reference['selections'],'notClaimedHistorical40OrAdditionalOrdinaryCases':True,'mandatoryPrivateGatewayMapEveryProcess':True}));raise SystemExit()
operational_pins(args.pins_sha256);sources();private_directory(require_empty=True);ordinary_sdk_proof();ordinary_domain_proof();ordinary_server_proof()
work=Path(tempfile.mkdtemp(prefix='native-private-40-',dir=PREP));save(PREP/'latest-native-attempt.json',{'attemptDirectory':str(work)})
receipt={'diagnosticOnly':True,'featureAcceptance':False,'passed':False,'nativeStarted':False,'nativeProcessAndDescendantsAbsent':True,'counterDirectoryEmpty':True,'caseCount':0,'sourceDerivedExpectedCases':40,'historical40RetentionClaimed':False,'ordinaryUniqueNativeCountUnchanged':1038,'processes':[]};env=clean_environment();env.update(DOTNET_PROCESSOR_COUNT='2',HexalithBuildsRoot=str(ROOT/'references/Hexalith.Builds'))
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
 typ,method,name=t.get('type'),t.get('method'),t.get('name').replace(chr(92)+chr(34),chr(34));matches=[x for x in reference['expectedCanonicalCases']if x[0]==typ and x[1]==method];assert matches
 if all(x[2]is None for x in matches):assert name==typ+'.'+method;return(typ,method,None)
 arg=re.search(r'\((?:fault|scenario): "([^"]+)"\)$',name or'');assert arg;return(typ,method,arg.group(1))
def one(selection,producer,copy):
 child=None;ticks=None;record={'selection':selection,'nativeStarted':False,'processAndDescendantsAbsent':True,'mappedPrivateGateway':False,'passed':False};receipt['processes'].append(record)
 try:
  xml=work/(selection['alias']+'.xml');command=['dotnet',str(testdll),selection['flag'],selection['filter'],'-xml',str(xml)];record.update(command=command,cwd=str(project.parent),startedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat())
  with(work/(selection['alias']+'.log')).open('wb')as stream:
   child=subprocess.Popen(command,cwd=project.parent,env=env,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
   # Set absence false immediately after spawn, before any fallible /proc read or receipt write.
   record.update(nativeStarted=True,processAndDescendantsAbsent=False,pid=child.pid,processGroup=child.pid,startTicks=None);receipt['nativeStarted']=True;receipt['nativeProcessAndDescendantsAbsent']=False;save(work/'receipt.json',receipt)
   ticks=Path('/proc',str(child.pid),'stat').read_text().rsplit(')',1)[1].split()[19];record['startTicks']=ticks;save(work/'receipt.json',receipt)
   while child.poll()is None:
    try:
     state=Path('/proc',str(child.pid),'stat').read_text().rsplit(')',1)[1].split()
     if state[19]==ticks and state[0]!='Z':
      paths={l.split(maxsplit=5)[-1]for l in Path('/proc',str(child.pid),'maps').read_text().splitlines()if len(l.split(maxsplit=5))==6}
      for alias,file in [('test-copy',copy),('producer',producer)]:
       if str(file)in paths and sha(file)==receipt['privateGatewayBefore']['producerSha256']and marker(file)and same(child.pid,ticks):record['mappedPrivateGateway']=True;record['acceptedMappedAlias']=alias
    except(FileNotFoundError,PermissionError,ProcessLookupError):pass
    time.sleep(.002)
   code=child.wait()
  record.update(exitCode=code,finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),xmlSha256=sha(xml)if xml.exists()else None,logSha256=sha(work/(selection['alias']+'.log')))
  assert code==0 and record['mappedPrivateGateway']and xml.exists()
  root=ET.parse(xml).getroot();tests=list(root.iter('test'));actual=[canonical(t)for t in tests];expected={tuple(x)for x in selection['expectedCases']};assemblies=root.findall('assembly')
  assert len(actual)==len(set(actual))==len(expected)and set(actual)==expected and all(t.get('result')=='Pass'for t in tests)and len(assemblies)==1
  assert all(int(assemblies[0].get(k,'0'))==0 for k in ['errors','failed','skipped','not-run'])
  record.update(passed=True,caseCount=len(tests),actualCanonicalCases=[list(x)for x in actual],actualRawCaseNames=[t.get('name')for t in tests])
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
 return record['actualCanonicalCases']
try:
 command=['dotnet','build',str(project),'-c','Debug','-t:Rebuild','-m:1','--no-restore','--disable-build-servers','-p:NuGetAudit=false','-p:BuildProjectReferences=false','-p:EnablePrivateCatalogInvocationProof=true','-p:EnablePrivateOwnerSourceReadProof=true']
 with(work/'build.log').open('wb')as stream:built=subprocess.run(command,cwd=ROOT,env=env,stdout=stream,stderr=subprocess.STDOUT)
 receipt['build']={'command':command,'cwd':str(ROOT),'exitCode':built.returncode,'logSha256':sha(work/'build.log'),'allSevenEmittedMappingEnvironmentValues':mapping['environment']};assert built.returncode==0
 producer=PRODUCER/'bin/Debug/net10.0'/(ASSEMBLY+'.dll');copy=out/(ASSEMBLY+'.dll');assert marker(producer)and marker(copy)and sha(copy)==sha(producer)and sha(copy.with_suffix('.pdb'))==sha(producer.with_suffix('.pdb'))
 receipt['privateGatewayBefore']={'producerSha256':sha(producer),'producerPdbSha256':sha(producer.with_suffix('.pdb')),'testCopySha256':sha(copy),'testCopyPdbSha256':sha(copy.with_suffix('.pdb')),'testAssemblySha256':sha(testdll)};receipt['ordinarySdkBefore']=native_sdk_proof();receipt['domain98Before']=ordinary_domain_proof();receipt['ordinaryRootServerBefore']=ordinary_server_proof();env.pop('DOTNET_PROCESSOR_COUNT',None)
 allcases=[]
 for selection in reference['selections']:allcases+=one(selection,producer,copy);private_directory(require_empty=True)
 assert len(allcases)==len({tuple(x)for x in allcases})==40 and{tuple(x)for x in allcases}=={tuple(x)for x in reference['expectedCanonicalCases']}
 sources();ordinary_domain_proof();ordinary_sdk_proof();ordinary_server_proof();assert sha(producer)==receipt['privateGatewayBefore']['producerSha256']and sha(copy)==sha(producer)
 assert sha(producer.with_suffix('.pdb'))==receipt['privateGatewayBefore']['producerPdbSha256']and sha(copy.with_suffix('.pdb'))==sha(producer.with_suffix('.pdb'))
 receipt.update(passed=True,caseCount=40,source52Match=True,allFiveMappedPrivateGateway=True,actualCanonicalCases=allcases)
except Exception as failure:receipt['failureType']=type(failure).__name__
finally:
 receipt['nativeProcessAndDescendantsAbsent']=all(p['processAndDescendantsAbsent']for p in receipt['processes'])
 try:private_directory(require_empty=True);receipt['counterDirectoryEmpty']=True
 except Exception:receipt['counterDirectoryEmpty']=False
 if not receipt['nativeProcessAndDescendantsAbsent']or not receipt['counterDirectoryEmpty']:receipt['passed']=False
 save(work/'receipt.json',receipt);print(json.dumps({'diagnosticOnly':True,'passed':receipt['passed'],'caseCount':receipt['caseCount'],'attemptDirectory':str(work),'nativeProcessAndDescendantsAbsent':receipt['nativeProcessAndDescendantsAbsent']}))
raise SystemExit(0 if receipt['passed']else 1)
