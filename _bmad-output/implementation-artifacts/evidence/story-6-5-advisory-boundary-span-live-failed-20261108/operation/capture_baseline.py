"""Read-only workspace binary/source metadata capture; never inspects the counter directory."""
from pathlib import Path
import hashlib,json,sys
sys.dont_write_bytecode=True
from ops_common import ROOT,PREP,census,sha
old=Path('/tmp/story65-advisory-wave-ops-prep-20261107/affected-inventory.json');prior=json.loads(old.read_text())
plan=json.loads((PREP/'operational-plan.json').read_text());ordinary=json.loads(Path('/tmp/story65-canonical-current-gateway-cpu-prep-20261108/watcher/ordinary-binding.json').read_text())
app={p:sha(ROOT/p if not Path(p).is_absolute()else p)for p in ordinary['currentApp147Sha256']};assert len(app)==147
found={str(f)for directory in ordinary['applicationBinaryDirectories']for f in Path(directory).glob('Hexalith.*.dll')}
known={str((ROOT/p).resolve())if not Path(p).is_absolute()else p for p in app};extra=found-known;assert len(extra)==2
modules={}
for name,producer in [('Hexalith.EventStore.DomainService','references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService'),('Hexalith.EventStore.Server','references/Hexalith.EventStore/src/Hexalith.EventStore.Server'),('Hexalith.EventStore.Gateway','references/Hexalith.EventStore/src/Hexalith.EventStore.Gateway')]:
 modules[name]={'producer':producer,'rows':census(name)}
d={'projects':plan['projects'],'knownServerCopies':census(),'knownServiceDefaultsCopies':prior['knownServiceDefaultsCopies'],'sdk137':census('Hexalith.EventStore.ServiceDefaults'),'independentModules':modules,'app147Sha256':app,'additional2Sha256':{p:sha(p)for p in sorted(extra)},'core8Sha256':ordinary['producerFilesSha256'],'sourceProjectSha256':{p:sha(ROOT/p)for p in plan['projects'].values()},'historical10And17GenerationProvenanceLimitsRemain':True,'baselineIsFreshBeforeOwnPrivateBuildNotClaimedNative1076BinaryGenerationForEveryCopy':True}
assert len(d['knownServerCopies'])==10 and len(d['sdk137'])==137
assert all(sha(p)==h for p,h in d['core8Sha256'].items())
assert all(sha(ROOT/p if not Path(p).is_absolute()else p)==h for p,h in app.items())
(PREP/'affected-inventory.json').write_text(json.dumps(d,indent=2)+'\n')
print(json.dumps({'projectsServer':len(d['knownServerCopies']),'sdk':len(d['sdk137']),'independent':{n:len(v['rows'])for n,v in modules.items()},'app':len(app),'extra':len(extra)}))
