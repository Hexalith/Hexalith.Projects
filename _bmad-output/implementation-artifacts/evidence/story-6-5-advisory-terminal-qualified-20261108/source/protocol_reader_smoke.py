"""Offline own-pipe checks only; no child, .NET process, application or counter sink."""
from pathlib import Path
import ast,hashlib,json,os,selectors,time,types
P=Path(__file__).resolve().parent
source=P/'run_differential.py'
node=next(n for n in ast.parse(source.read_text()).body if isinstance(n,ast.FunctionDef) and n.name=='ready_line')
scope={'os':os,'selectors':selectors,'time':time}
exec(compile(ast.fix_missing_locations(ast.Module(body=[node],type_ignores=[])),str(source), 'exec'),scope)
read=scope['ready_line'];checks=[]
def pipe(payload=b'',eof=False):
 r,w=os.pipe();p=types.SimpleNamespace(stdout=os.fdopen(r,'rb',buffering=0));os.write(w,payload)
 if eof:os.close(w);w=None
 return p,w
def close(p,w):
 p.stdout.close()
 if w is not None:os.close(w)
def raises(payload,kind,code=None,eof=False,seconds=.1):
 p,w=pipe(payload,eof)
 try:
  try:read(p,time.monotonic()+seconds)
  except kind as exc:
   if code is not None:assert str(exc)==code
  else:raise AssertionError('ExpectedClosedReaderFailure')
 finally:close(p,w)
p,w=pipe(b'ONE\nTWO\n')
try:
 assert read(p,time.monotonic()+.1)=='ONE'
 assert read(p,time.monotonic()-1)=='TWO' # Pending line consumed without waiting/select/deadline.
 checks.append('two-coalesced-lines-pending-first')
finally:close(p,w)
raises(b'PARTIAL',TimeoutError,'ChildProtocolTimeout',seconds=.02);checks.append('open-partial-line-deadline')
raises(b'',RuntimeError,'ChildExitedBeforeProtocol',eof=True);checks.append('empty-eof')
raises(b'PARTIAL',RuntimeError,'ChildProtocolPartialLineAtEof',eof=True);checks.append('partial-eof')
p,w=pipe(b'A'*4095+b'\n')
try:assert len(read(p,time.monotonic()+.1))==4095;checks.append('maximum4096-including-newline')
finally:close(p,w)
raises(b'A'*4096,RuntimeError,'ClosedProtocolBoundExceeded');checks.append('unterminated4096-bound')
raises(bytes([255,10]),UnicodeDecodeError);checks.append('non-ascii-rejected')
p,w=pipe(b'FIRST\nPENDING\n');q,x=pipe(b'OTHER\n')
try:
 assert read(p,time.monotonic()+.1)=='FIRST'
 assert read(q,time.monotonic()+.1)=='OTHER'
 assert read(p,time.monotonic()-1)=='PENDING';checks.append('per-child-buffer-isolation')
finally:close(p,w);close(q,x)
receipt={'offlineOwnPipeOnly':True,'passed':len(checks),'checks':checks,'controllerSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'childProcessesStarted':0,'dotnetRuns':0,'counterDirectoryAccessed':False,'signalsSent':0}
(P/'protocol-reader-smoke.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps({'passed':len(checks),'childProcessesStarted':0}))
