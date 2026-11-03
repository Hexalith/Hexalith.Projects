import pathlib,json,subprocess,hashlib,os
p=pathlib.Path('/tmp/story61-opt-debug-profile');inputs=pathlib.Path('/tmp/story61-owned-diagnostics-live40-aqih1hj6');args=[str(inputs/name) for name in ['input-catalog.json','input-activation.json','input-eventstore.observation.json','input-folders.observation.json']];env=os.environ.copy();env['DOTNET_PROCESSOR_COUNT']='2';rows=[]
for trial,label in enumerate(['ordinary','optimized','optimized','ordinary','ordinary','optimized'],1):
 command=['dotnet',str(p/('profiler-'+label)/'bin/Profile/debug/Profile.dll'),*args]
 done=subprocess.run(command,env=env,text=True,capture_output=True);(p/('trial-'+str(trial)+'-'+label+'.log')).write_text(done.stdout+done.stderr)
 if done.returncode:raise RuntimeError('Profile failed: '+done.stderr[-1200:])
 result=json.loads(done.stdout);(p/('trial-'+str(trial)+'-'+label+'.json')).write_text(json.dumps(result,indent=2)+'\n');rows.append({'trial':trial,'label':label,'command':command,'exitCode':done.returncode,'result':result});print('Measured',trial,label,flush=True)
base=rows[0]['result']['proof']
if any(x['result']['proof']!=base for x in rows):raise RuntimeError('Semantic evidence differs')
(p/'measurements.json').write_text(json.dumps({'environment':{'DOTNET_PROCESSOR_COUNT':'2'},'inputs':{str(pathlib.Path(x)):hashlib.sha256(pathlib.Path(x).read_bytes()).hexdigest() for x in args},'allCanonicalIdentityAndNormalizedQueryProofsEqual':True,'rows':rows},indent=2)+'\n')
for label in ['ordinary','optimized']:
 print(label,[{'work':item['name'],'median':round(item['medianMilliseconds'],3)} for item in next(x['result']['results'] for x in rows if x['label']==label)])
