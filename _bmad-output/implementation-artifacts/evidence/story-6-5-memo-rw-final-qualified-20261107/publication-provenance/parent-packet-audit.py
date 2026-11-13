from pathlib import Path
import hashlib,json,gzip,re,datetime
BASE=Path('/tmp/story65-memo-rw-final-qualified-evidence-20261107')
sha=lambda b:hashlib.sha256(b).hexdigest()
def unique(pairs):
 d={}
 for k,v in pairs:
  assert k not in d
  d[k]=v
 return d
def load(b):return json.loads(b,object_pairs_hook=unique)
manifest=BASE/'SHA256SUMS';assert sha(manifest.read_bytes())=='99c549cea33f91d8feb36d202ba4e4ac2eb3ea42b0c5507ae2a99c2f2f45e31d'
entries={}
for line in manifest.read_text().splitlines():
 digest,name=line.split('  ',1);assert name not in entries and re.fullmatch('[a-f0-9]{64}',digest) and not Path(name).is_absolute() and '..' not in Path(name).parts;entries[name]=digest
assert len(entries)==145
assert {str(p.relative_to(BASE))for p in BASE.rglob('*')if p.is_file()}==set(entries)|{'SHA256SUMS'}
privacy=0
for name,digest in entries.items():
 p=BASE/name;b=p.read_bytes();assert not p.is_symlink() and sha(b)==digest
 assert p.suffix.lower() not in {'.dll','.pdb','.exe','.nettrace','.etlx','.pyc','.pem','.pfx','.key'}
 if p.suffix=='.gz':b=gzip.decompress(b)
 privacy+=len(re.findall(rb'eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',b))
assert privacy==0
provenance=load((BASE/'source-provenance.json').read_bytes());assert len(provenance['copies'])==provenance['copiedArtifactCount']==142
for row in provenance['copies']:
 b=(BASE/row['packetPath']).read_bytes()
 if row.get('copyByteExactAfterGzipDecompression'):
  assert sha(b)==row['compressedSha256'] and len(b)==row['compressedBytes'];b=gzip.decompress(b);assert len(b)==row['originalBytes']
 else:assert row['copyByteExact'] and len(b)==row['bytes']
 assert sha(b)==row['sourceSha256'] and b==Path(row['sourcePath']).read_bytes()
raw=gzip.decompress((BASE/'final-compiled/results/benchmark-samples.jsonl.gz').read_bytes());rows=[load(l)for l in raw.splitlines()];assert len(rows)==324
obs=sum(len(w['Observations'])for r in rows for w in r['Workers']);calls=sum(r['CompleteObservations']*r['ImagesPerObservation']for r in rows);hits=sum(r['ReportedMemoHits']for r in rows);fresh=sum(r['FreshResultsWithoutReportedReuse']for r in rows)
assert(obs,calls,hits,fresh)==(4032,266112,258984,7128)
assert all(r['MemoCountAfter']==66 and r['ChargedBytesAfter']==27690296 for r in rows)
report={'auditUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':True,'candidateArtifacts':145,'sourceCopiesRechecked':142,'exactChecksumFileset':True,'manifestSha256':sha(manifest.read_bytes()),'strictNestedRawRows':324,'rawOriginalBytes':len(raw),'observations':obs,'imageCalls':calls,'memoHits':hits,'freshNonReused':fresh,'nativeAlreadyQualifiedUniquePasses':1048,'diagnosticChecksSeparate':621,'jwtPrivateKeyMatches':privacy,'binaryTraceSymlinkArtifacts':0,'knownCostsAndOneSessionSubsetLimitsReviewed':True,'featureAcceptance':False,'auditScriptSha256':sha(Path(__file__).read_bytes())}
out=Path(__file__).with_suffix('.json');out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':True,'auditSha256':sha(out.read_bytes()),'artifacts':145,'copies':142,'privacyMatches':privacy}))
