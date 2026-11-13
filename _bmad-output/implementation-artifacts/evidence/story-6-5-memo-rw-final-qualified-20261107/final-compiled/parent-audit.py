from pathlib import Path
import hashlib,json,math,os
BASE=Path('/tmp/story65-image-memo-rw-final-compiled-prep-20261107')
RUN=Path('/tmp/story65-image-memo-rw-final-compiled-execution-20261107/attempt-1')
RESULT=Path('/tmp/story65-image-memo-rw-final-compiled-results-20261107')
def sha(p):
 with Path(p).open('rb') as s:return hashlib.file_digest(s,'sha256').hexdigest()
def strict(p):
 def unique(pairs):
  d={}
  for k,v in pairs:
   assert k not in d,k
   d[k]=v
  return d
 return json.loads(Path(p).read_text(),object_pairs_hook=unique)
prep=strict(BASE/'preparation-receipt.json');assert sha(BASE/'preparation-receipt.json')=='7f7d51907f9142d02b00fbc46cf394f820c89812f95234c36d1d74edc0e07464'
expected={**prep['currentExternalInputsSha256'],**{str(BASE/k):v for k,v in prep['filesSha256'].items()},str(BASE/'preparation-receipt.json'):sha(BASE/'preparation-receipt.json')}
assert len(expected)==653
assert strict(RUN/'inputs-before.json')==strict(RUN/'inputs-after.json')==expected
assert all(sha(k)==v for k,v in expected.items())
receipt=strict(RUN/'command-receipt.json');assert receipt['passed'] and len(receipt['steps'])==3
for i,s in enumerate(receipt['steps']):
 assert s['exitCode']==0 and s['ownedDiagnosticGroupAbsent'] and not s['remainingOwnedDiagnosticGroup'] and s['guardReason'] is None
 for t in ['stdout','stderr']:assert sha(RUN/(f'{i+1:02d}-'+['restore','build','benchmark'][i]+f'.{t}.log'))==s[t+'Sha256']
 assert not Path('/proc',str(s['pid'])).exists()
for p in Path('/proc').iterdir():
 if not p.name.isdigit():continue
 try:f=(p/'stat').read_text().rsplit(')',1)[1].split()
 except FileNotFoundError:continue
 assert not(f[0]!='Z' and int(f[2]) in [s['pid'] for s in receipt['steps']])
assert '0 Warning(s)' in (RUN/'02-build.stdout.log').read_text()
assert '0 Error(s)' in (RUN/'02-build.stdout.log').read_text()
result=strict(RESULT/'result.json');assert result['Completed'] and result['AllPassed'] and result['Checks']==result['Passed']==621
assert strict(RESULT/'parity.json')=={'Checks':621,'Passed':621,'AllPassed':True}
raw=[json.loads(s) for s in (RESULT/'benchmark-samples.jsonl').read_text().splitlines()]
assert len(raw)==324
obs=[];hits=nonhits=0
for row in raw:
 assert row['ImagesPerObservation']==66 and row['MemoCountAfter']==66 and row['ChargedBytesAfter']==27690296
 assert row['CompleteObservations']==row['Readers']*row['IterationsPerReader']
 assert row['EntireCohortFitsOriginalBounds'] and row['NonHitsDistinguishedFromRejections']
 current=[o for w in row['Workers'] for o in w['Observations']]
 assert len(current)==row['CompleteObservations'] and len(row['Workers'])==row['Readers']
 assert all(w['FailureType'] is None for w in row['Workers'])
 assert sum(o['ReportedMemoHits'] for o in current)==row['ReportedMemoHits']
 assert sum(o['FreshResultsWithoutReportedReuse'] for o in current)==row['FreshResultsWithoutReportedReuse']
 assert all(o['ReportedMemoHits']+o['FreshResultsWithoutReportedReuse']==66 for o in current)
 obs.extend(current);hits+=row['ReportedMemoHits'];nonhits+=row['FreshResultsWithoutReportedReuse']
assert len(obs)==4032 and hits==258984 and nonhits==7128 and (hits+nonhits)==266112
inventory=strict(RESULT/'inventory.json')['Files'];assert len(inventory)==66
assert all(expected[r['Path']]==r['Sha256']==sha(r['Path']) for r in inventory)
runtime=strict(RESULT/'runtime-facts.json');assert runtime['ProcessorCount']==14 and runtime['ProcessorCountOverrideAbsent'] and runtime['IsServerGC'] and runtime['ForcedGcCalls']==0 and runtime['Harness']['IsJITOptimizerDisabled']
proof=strict(RESULT/'selected-variant-path-proofs.json');assert [p['Name'] for p in proof]==['production','monitor-control','rw-copy']
assert proof[0]['Assembly']['Sha256']==prep['actualCompiledOracleSdkSha256']=='6947e843daa27e9567c9f5ece4f884af75021240c5fbb6baee09bcd6cbfd3542'
assert all(p['ScopeTypeProvenByActualObservation'] and p['DomainStreamAndTechnicalSelectedPathProvenByActualHitsAndOccupancy'] for p in proof)
output={'passed':True,'sourceNativeQualifiedUnique':1048,'diagnosticChecks':621,'rawBatches':len(raw),'observations':len(obs),'imageCalls':hits+nonhits,'reportedHits':hits,'freshWithoutReuse':nonhits,'chargedBytes':27690296,'all653InputsBeforeAfterCurrentExact':True,'all3CommandsZeroAndOwnedGroupsAbsent':True,'natural14ServerGcDebug':True,'original66ImageSourceCohortOnly':True,'applicationAcceptance':False,'measurementInterpretation':'Independent reader audit required; no whole-host or deadline benefit inferred','artifactsSha256':{str(p):sha(p) for d in [RESULT,RUN] for p in d.rglob('*') if p.is_file()},'auditScriptSha256':sha(__file__)}
out=RUN/'parent-final-compiled-audit.json';out.write_text(json.dumps(output,indent=2)+'\n');print(json.dumps({'passed':True,'audit':str(out),'sha256':sha(out),'rawBatches':324,'observations':4032,'diagnosticChecks':621,'inputPaths':653}))
