"""Closed-only validation; no mutation/archive and no request or downstream joins."""
from pathlib import Path
import importlib.util,json,os,stat
from ops_common import *

def validate(supervisor):
 assert supervisor['controlPublished']is True
 fd=os.open(PRIVATE,os.O_RDONLY|os.O_DIRECTORY|os.O_NOFOLLOW|os.O_CLOEXEC)
 try:
  s=os.fstat(fd);assert(s.st_dev,s.st_ino,s.st_uid,stat.S_IMODE(s.st_mode))==(2096,8752986,1000,0o700)
  assert set(os.listdir(fd))=={'window.control','projects.jsonl','projects.closed.json'}
  raw={};identities={}
  for name in sorted(os.listdir(fd)):
   handle=os.open(name,os.O_RDONLY|os.O_NOFOLLOW|os.O_NONBLOCK|os.O_CLOEXEC,dir_fd=fd)
   try:
    st=os.fstat(handle);limit=8*1024*1024 if name.endswith('.jsonl')else 4096
    assert stat.S_ISREG(st.st_mode)and st.st_uid==1000 and stat.S_IMODE(st.st_mode)==0o600 and st.st_size<=limit
    parts=[];total=0
    while True:
     data=os.read(handle,min(65536,limit+1-total))
     if not data:break
     parts.append(data);total+=len(data);assert total<=limit
    again=os.fstat(handle);assert(st.st_dev,st.st_ino,st.st_size,st.st_mtime_ns)==(again.st_dev,again.st_ino,again.st_size,again.st_mtime_ns)
    raw[name]=b''.join(parts);identities[name]={'device':st.st_dev,'inode':st.st_ino,'bytes':st.st_size,'sha256':__import__('hashlib').sha256(raw[name]).hexdigest()}
   finally:os.close(handle)
  control=identities['window.control'];assert all(control[k]==supervisor['controlIdentity'][k]for k in ['device','inode','bytes'])
  assert all(type(supervisor[k])is int and supervisor[k]>0 for k in ['runOrdinal','frequency','admissionStartTicks','admissionEndTicks'])
  expected=f"{supervisor['runOrdinal']}\n{supervisor['frequency']}\n{supervisor['admissionStartTicks']}\n{supervisor['admissionEndTicks']}\n".encode('ascii')
  assert raw['window.control']==expected
  pairs=json.loads(raw['projects.closed.json'],object_pairs_hook=lambda pairs:pairs)
  assert isinstance(pairs,list)and len(pairs)==len(dict(pairs));closed=dict(pairs)
  fields={'diagnosticOnly','featureAcceptance','role','runOrdinal','admitted','disposedOrUnwound','activeAtDrainEnd','queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','writtenRows','writtenBytes','queuedAtDrainEnd','incompleteHardDrain','countersFinal','lateDisposedOrDropCountersMayIncrease','maximumRows','maximumBytes','queueCapacity','windowStartTicks','windowEndTicks','frequency'}
  assert set(closed)==fields and closed['role']=='projects'and closed['runOrdinal']==supervisor['runOrdinal']
  assert all(type(v)is int and v>=0 for k,v in closed.items()if k!='role')
  assert closed['diagnosticOnly']==1 and closed['featureAcceptance']==0
  assert closed['maximumRows']==65536 and closed['maximumBytes']==8388608 and closed['queueCapacity']==4096
  assert closed['frequency']==1000000000 and closed['windowEndTicks']-closed['windowStartTicks']==8*closed['frequency']
  assert closed['writtenRows']<=closed['maximumRows']and closed['writtenBytes']<=closed['maximumBytes']
  assert closed['admitted']==closed['disposedOrUnwound']and closed['activeAtDrainEnd']==0 and closed['countersFinal']==1
  assert all(closed[k]==0 for k in ['queuedRowsDropped','closedRowsDropped','capRowsDropped','ioFailures','queuedAtDrainEnd','incompleteHardDrain','lateDisposedOrDropCountersMayIncrease'])
  assert closed['windowStartTicks']==supervisor['admissionStartTicks']and closed['windowEndTicks']==supervisor['admissionEndTicks']and closed['frequency']==supervisor['frequency']
  source=SOURCE_PREP/'run_differential.py';spec=importlib.util.spec_from_file_location('qualified_terminal_row_schema',source);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
  count=module.validate_rows(raw['projects.jsonl'],closed,supervisor['runOrdinal'],supervisor['frequency'],'active',{})
  physical=supervisor.get('physicalWriterProof',{});observed=physical.get('observed')is True
  if observed:assert all(physical[k]==identities['projects.jsonl'][k]for k in ['device','inode'])
  return {'diagnosticOnly':True,'featureAcceptance':False,'complete':True,'captures':closed['admitted'],'rows':count,'closed':closed,'artifacts':identities,'physicalWriterObservedAtOnePoint':observed,'physicalAttributionUnproven':not observed,'physicalProofNotAllRowExclusivity':True,'noTimerCauseOrSerializedResultOrDownstreamInference':True,'maximumRowsPerCapture':8,'boundarySpanStageAliases':{'ComponentFile':'InitialScope','ComponentPolicy':'InitialInputs','AclFile':'Discovery','SettingsEnumerationSortDictionary':'TerminalEncoding','AuthenticationSerialization':'FinalInputs','ActorBoundary':'FinalCandidates','ResultTail':'Index'},'selectedMarkerIntervalsNotCallbacksOrExhaustivePartition':True}
 finally:os.close(fd)
