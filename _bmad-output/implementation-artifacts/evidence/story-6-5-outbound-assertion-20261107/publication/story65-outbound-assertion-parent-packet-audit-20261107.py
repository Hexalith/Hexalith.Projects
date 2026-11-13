from pathlib import Path
import json,hashlib,gzip,re,collections
B=Path('/tmp/story65-outbound-assertion-evidence-complete-20261107')
sha=lambda b:hashlib.sha256(b).hexdigest()
m=B/'SHA256SUMS';assert sha(m.read_bytes())=='f9401441172240f5b9120bbb54be9ce86dfcacae68c06cda7b224b1fc3418c59'
entries={}
for line in m.read_text().splitlines():
 h,n=line.split('  ',1);assert n not in entries and not Path(n).is_absolute() and '..'not in Path(n).parts;f=B/n;assert f.is_file() and not f.is_symlink() and sha(f.read_bytes())==h;entries[n]=h
assert len(entries)==374 and {str(f.relative_to(B)) for f in B.rglob('*') if f.is_file()}==set(entries)|{'SHA256SUMS'}
provenance=json.loads((B/'qualified-preparation/provenance.json').read_text());origins={str(Path('qualified-preparation')/x['destination']):x for x in provenance['gzipCopies']};actual=json.loads((B/'actual-handler/raw-compression-receipt.json').read_text());origins['actual-handler/gateway.jsonl.gz']={'originalSha256':actual['sourceArchivedRawSha256'],'originalBytes':actual['rawBytes'],'source':'/tmp/story65-outbound-assertion-diagnostic-20261107/counter-archive/gateway.jsonl'}
zero={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'};fields=zero|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'};total=0;counts={};stages=collections.Counter();privacy=[]
def pairs(xs):
 d={}
 for k,v in xs:assert k not in d;d[k]=v
 return d
for n in entries:
 f=B/n;assert f.suffix.lower()not in ['.dll','.pdb','.pfx','.pem','.key','.der','.nettrace','.etlx','.cache'] and f.name not in ['window.control','fixture.json','endpoints.json','apphost-session.json'];raw=f.read_bytes()
 if f.suffix=='.gz':
  raw=gzip.decompress(raw);o=origins[n];assert sha(raw)==o['originalSha256'] and len(raw)==o['originalBytes'] and raw==Path(o['source']).read_bytes();c=0
  for line in raw.splitlines():
   r=json.loads(line,object_pairs_hook=pairs);assert set(r)==fields and r['role']=='gateway' and r['stage']in ['Whole','ComponentFile','ComponentPolicy'];assert all(type(r[k])is int for k in fields-{'role','stage'}) and all(r[k]==0 for k in zero);assert r['captureOrdinal']>0 and r['elapsedTicks']>=0 and r['frequency']>0;c+=1;stages[r['stage']]+=1
  counts[n]=c;total+=c
 if re.search(rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}',raw) or b'-----BEGIN PRIVATE KEY-----' in raw or b'-----BEGIN RSA PRIVATE KEY-----' in raw:privacy.append(n)
assert len(counts)==12 and total==86713 and counts['actual-handler/gateway.jsonl.gz']==3025 and not privacy
r={'diagnosticOnly':True,'featureAcceptance':False,'candidateManifestSha256':sha(m.read_bytes()),'checksumEntries':len(entries),'exactFileSet':True,'allHashesMatch':True,'gzipOriginals':len(counts),'allOriginalGzipBytesAndHashesMatch':True,'strictRows':total,'actualRows':3025,'syntheticRows':83688,'allThreeStageZeroCountAllocationSchemasPass':True,'protectedPatternMatches':0,'countsByGzip':counts,'READMEFullyReadByParent':True}
Path('/tmp/story65-outbound-assertion-parent-packet-audit-20261107.json').write_text(json.dumps(r,indent=2)+'\n');print(json.dumps({k:r[k] for k in ['checksumEntries','gzipOriginals','strictRows','actualRows','syntheticRows','protectedPatternMatches']}))
