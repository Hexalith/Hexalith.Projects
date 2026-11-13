from pathlib import Path
import hashlib,json,re,datetime,xml.etree.ElementTree as ET
BASE=Path('/tmp/story65-memo-rw-ordinary-evidence-20261107');LIVE=Path('/tmp/story65-memo-rw-acceptance-live-20261107');sha=lambda b:hashlib.sha256(b).hexdigest()
def unique(pairs):
 d={}
 for k,v in pairs:
  assert k not in d;d[k]=v
 return d
def load(p):return json.loads(Path(p).read_text(),object_pairs_hook=unique)
m=BASE/'SHA256SUMS';assert sha(m.read_bytes())=='cb676d4621e8da55e679db5e73b3397458ba8299041b74d56bb4ca5c518bdf94';entries={}
for line in m.read_text().splitlines():
 d,n=line.split('  ',1);assert n not in entries and re.fullmatch('[a-f0-9]{64}',d) and not Path(n).is_absolute() and '..'not in Path(n).parts;entries[n]=d
assert len(entries)==32 and {str(p.relative_to(BASE))for p in BASE.rglob('*')if p.is_file()}==set(entries)|{'SHA256SUMS'}
privacy=0
for n,d in entries.items():
 p=BASE/n;b=p.read_bytes();assert sha(b)==d and not p.is_symlink();assert p.suffix.lower()not in {'.dll','.pdb','.exe','.nettrace','.etlx','.pem','.key','.pyc'};privacy+=len(re.findall(rb'eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',b))
assert privacy==0
copies=load(BASE/'byte-copy-provenance.json')+load(BASE/'final-added-original-provenance.json');assert len(copies)==22
for r in copies:
 b=(BASE/r['packetPath']).read_bytes();assert sha(b)==r['sha256'] and len(b)==r['bytes'] and b==Path(r['source']).read_bytes()
raw=(LIVE/'browser-advisory-seed-readiness.jsonl').read_bytes();p=load(BASE/'ordinary-run/readiness-provenance.json');assert len(raw)==p['originalBytes'] and sha(raw)==p['originalSha256'];original=[json.loads(l,object_pairs_hook=unique)for l in raw.splitlines()];rows=[json.loads(l,object_pairs_hook=unique)for l in (BASE/'ordinary-run/artifacts/advisory-readiness-scalars.jsonl').read_bytes().splitlines()];assert len(rows)==len(original)==31
assert rows==[{k:r[k]for k in p['retainedClosedFields']}for r in original]
assert all(not isinstance(v,(dict,list))for r in rows for v in r.values())
single=[r for r in rows if not r['multiple']];multi=[r for r in rows if r['multiple']];assert len(single)==1 and single[0]['positiveCandidatesVerified'] and len(multi)==30
assert all(r['httpStatus']==200 and r['result']is None and r['incomplete'] and r['candidateCount']==0 and not r['positiveCandidatesVerified']for r in multi)
cleanup=load(BASE/'ordinary-run/story65-memo-rw-acceptance-live-20261107-independent-cleanup-source-audit.json');assert len(cleanup['identities'])==21 and len(cleanup['containers'])==5
assert all(cleanup[k]for k in ['allIndependentRecordedProcessAndContainerAbsences','allOriginalCleanupAbsenceVerified','namespaceContainersAbsent','privateRuntimeDirectoryAbsent','preexistingResourcesPreserved','ownedSidecarsAbsent','privateMaterialRemoved'])
assert cleanup['fullLiveManifestSourceCount']==17341 and cleanup['fullLiveManifestHashMismatches']==0
xml=ET.parse(BASE/'ordinary-run/artifacts/browser-seed.xml');assemblies=xml.getroot().findall('assembly');assert len(assemblies)==1;a=assemblies[0].attrib;assert (int(a['total']),int(a['passed']),int(a['failed']),int(a['errors']),int(a['skipped']))==(1,0,1,0,0)
limits=load(BASE/'actual-result-and-proof-limits.json');assert not limits['browserXmlPresent'] and not limits['browserProofPresent'] and not limits['browserPrivacyTelemetryPresent'];assert len(limits['unresolvedServerGatewayGenerationPaths'])==10
out={'auditUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'passed':True,'candidateArtifacts':32,'candidateManifestSha256':sha(m.read_bytes()),'originalCopiesRechecked':22,'originalReadinessBytes':len(raw),'exactClosedProjectedRows':31,'singleVerifiedPositives':1,'multipleVerifiedPositives':0,'multipleAttempts':30,'actualManagedExitCode':1,'browserCases':0,'seedFailures':1,'cleanup21Identities5Containers6ReceiptsAnd17341SourceAuditReviewed':True,'tenUnknownDependencyGenerationsExplicitlyRetained':True,'fullDependencyGenerationCoherenceClaimed':False,'jwtPrivateKeyPatternMatches':privacy,'featureAcceptance':False,'auditScriptSha256':sha(Path(__file__).read_bytes())};dest=Path(__file__).with_suffix('.json');dest.write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({'passed':True,'artifacts':32,'copies':22,'rows':31,'privacyMatches':privacy,'auditSha256':sha(dest.read_bytes())}))
