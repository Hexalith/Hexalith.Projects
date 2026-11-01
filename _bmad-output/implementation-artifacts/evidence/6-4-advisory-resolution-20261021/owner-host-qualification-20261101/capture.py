from record import ROOT,EVIDENCE,WORK,write,state,sources,digest,git
from pathlib import Path
import gzip,json
write('git-before.json',state())
with gzip.open(EVIDENCE/'sources-before.json.gz','wt') as f:json.dump(sources(),f,sort_keys=True)
historical=ROOT/'_bmad-output/implementation-artifacts/evidence/6-4-advisory-resolution-20261021/current-advisory-source-20261031'
write('historical-evidence-before.json',{str(p.relative_to(historical)):digest(p) for p in historical.rglob('*') if p.is_file() and '__pycache__' not in p.parts})
parties=ROOT/'references/Hexalith.Parties'
write('shared-parties-before.json',{str(p.relative_to(parties)):digest(p) for p in parties.rglob('*') if p.is_file() and '.git' not in p.parts})
write('canonical-before.json',{name:digest(ROOT/name) for name in ['_bmad-output/implementation-artifacts/6-4-resolve-projects-with-transient-current-explanations.md','_bmad-output/implementation-artifacts/sprint-status.yaml']})
source=Path('/tmp/story64-approved-repair-kvtivzn1/source/projects')
repos=['.',*[line.split(' ',1)[1] for line in git(ROOT,'config','-f','.gitmodules','--get-regexp',r'^submodule\..*\.path$').decode().splitlines()]]
identities=[]
for name in repos:
 actual=ROOT/name;scratch=source/name
 identities.append({'repository':name,'real_head':git(actual,'rev-parse','HEAD').decode().strip(),'scratch_head':git(scratch,'rev-parse','HEAD').decode().strip()})
 assert identities[-1]['real_head']==identities[-1]['scratch_head']
write('scratch-provenance.json',identities)
real=sources();matches={relative:(source/relative).is_file() and digest(source/relative)==expected for relative,expected in real.items()}
write('scratch-source-before.json',{'files':matches,'all_current_source_matches':all(matches.values())})
print('Baseline preservation and disposable clone identities captured.',flush=True)
