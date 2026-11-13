from pathlib import Path
import json,hashlib,gzip,re,collections
B=Path('/tmp/story65-invocation-transport-evidence-complete-20261107');sha=lambda b:hashlib.sha256(b).hexdigest();manifest=B/'SHA256SUMS';assert sha(manifest.read_bytes())=='2af609278ca3e914eac63a56352f4ad66ce85c1edb4ead0c58feefb6dfeae3b3';entries={}
for line in manifest.read_text().splitlines():
 h,n=line.split('  ',1);assert n not in entries and not Path(n).is_absolute() and '..'not in Path(n).parts;f=B/n;assert f.is_file() and not f.is_symlink() and sha(f.read_bytes())==h;entries[n]=h
assert len(entries)==637 and {str(f.relative_to(B))for f in B.rglob('*')if f.is_file()}==set(entries)|{'SHA256SUMS'}
partial=json.loads((B/'qualified-scratch/staging-manifest.json').read_text());origins={str(Path('qualified-scratch')/n):e for n,e in partial['compressedSyntheticRows'].items()};actual=json.loads((B/'actual-sdk/raw-compression-receipt.json').read_text());origins['actual-sdk/gateway.jsonl.gz']={'originalSha256':actual['originalRawSha256'],'originalBytes':actual['originalRawBytes'],'source':'/tmp/story65-invocation-transport-diagnostic-20261107/counter-archive/gateway.jsonl'}
zero={'allocationMode','allocatedBytes','yielded','nullValue','authenticationAccepted','nonAuthenticationDiscarded','sensitiveSegmentExcluded','authenticationOtherExcluded'};fields=zero|{'role','runOrdinal','captureOrdinal','stage','elapsedTicks','frequency'};total=0;counts={};privacy=[]
def pairs(xs):
 d={}
 for k,v in xs:assert k not in d;d[k]=v
 return d
for n in entries:
 f=B/n;assert f.suffix.lower()not in ['.dll','.pdb','.pfx','.pem','.key','.der','.nettrace','.etlx','.cache'] and f.name not in ['window.control','fixture.json','endpoints.json','apphost-session.json'];raw=f.read_bytes()
 if f.suffix=='.gz':
  raw=gzip.decompress(raw);e=origins[n];assert sha(raw)==e['originalSha256'] and len(raw)==e['originalBytes'] and raw==Path(e['source']).read_bytes();count=0
  for line in raw.splitlines():
   d=json.loads(line,object_pairs_hook=pairs);assert set(d)==fields and d['role']=='gateway' and d['stage']in ['Whole','ComponentFile','ComponentPolicy'];assert all(type(d[k])is int for k in fields-{'role','stage'}) and all(d[k]==0 for k in zero) and d['captureOrdinal']>0 and d['frequency']>0 and d['elapsedTicks']>=0;count+=1
  counts[n]=count;total+=count
 if re.search(rb'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}',raw) or b'-----BEGIN PRIVATE KEY-----'in raw or b'-----BEGIN RSA PRIVATE KEY-----'in raw:privacy.append(n)
assert len(counts)==12 and total==85246 and counts['actual-sdk/gateway.jsonl.gz']==1584 and not privacy
p={'diagnosticOnly':True,'featureAcceptance':False,'candidateManifestSha256':sha(manifest.read_bytes()),'checksumEntries':637,'exactFileSet':True,'allHashesMatch':True,'gzipOriginals':12,'allOriginalGzipBytesAndHashesMatch':True,'strictRows':total,'actualRows':1584,'syntheticRows':83662,'allThreeStageZeroCountAllocationSchemasPass':True,'protectedPatternMatches':0,'READMEFullyReadByParent':True,'countsByGzip':counts};Path('/tmp/story65-invocation-transport-parent-packet-audit-20261107.json').write_text(json.dumps(p,indent=2)+'\n');print(json.dumps({k:p[k]for k in ['checksumEntries','gzipOriginals','strictRows','actualRows','syntheticRows','protectedPatternMatches']}))
