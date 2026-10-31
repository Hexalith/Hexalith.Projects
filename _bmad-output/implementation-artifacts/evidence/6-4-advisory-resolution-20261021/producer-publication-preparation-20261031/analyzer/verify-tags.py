#!/usr/bin/env python3
"""Replay verification of captured official stable tag targets without updating refs."""
import datetime,hashlib,json,pathlib,re,subprocess,sys
repo,observation,output=sys.argv[1:]
selected='2186b9a4bc507a116a457c590c80945c2e232cbd'
def git(*args):return subprocess.check_output(['git','--no-replace-objects','-C',repo,*args],text=True).strip()
assert git('rev-parse','HEAD')==selected
assert git('rev-parse','--is-shallow-repository')=='false'
refs=dict(line.split('\t')[::-1] for line in pathlib.Path(observation).read_text().splitlines())
ancestors=set(git('rev-list',selected).splitlines()); tags=[]
for ref,oid in refs.items():
 m=re.fullmatch(r'refs/tags/v(\d+\.\d+\.\d+)',ref)
 if m:
  target=refs.get(ref+'^{}',oid)
  tags.append({'gitTag':ref.removeprefix('refs/tags/'),'version':m[1],'officialObject':oid,'officialCommit':target,'reachableFromSelected':target in ancestors,'channels':[None]})
reachable=sorted([t for t in tags if t['reachableFromSelected']],key=lambda t:tuple(map(int,t['version'].split('.'))),reverse=True)
for tag in reachable:
 local=git('rev-parse',tag['gitTag']+'^{commit}')
 assert local==tag['officialCommit'],tag
 tag['localCommitVerified']=local
result={'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'selectedRevision':selected,'officialRemote':git('remote','get-url','origin'),'tagPattern':'refs/tags/v<major>.<minor>.<patch>','stableTags':tags,'reachableStableTags':reachable,'previousRelease':reachable[0],'officialTagsSha256':hashlib.sha256(pathlib.Path(observation).read_bytes()).hexdigest(),'selectedCommit':git('show','--no-patch','--format=%H%n%T%n%P%n%cI%n%B',selected),'previousTagSource':git('show','--no-patch','--format=%H%n%T%n%P%n%cI%n%B',reachable[0]['officialCommit'])}
with pathlib.Path(output).open('x') as f:json.dump(result,f,indent=2);f.write('\n')
print(json.dumps({'officialStableTags':len(tags),'reachableStableTags':len(reachable),'previousRelease':reachable[0]}))
