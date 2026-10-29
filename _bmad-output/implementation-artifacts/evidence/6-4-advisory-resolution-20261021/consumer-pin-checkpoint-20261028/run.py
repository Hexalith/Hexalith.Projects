import pathlib,json,subprocess,datetime,os,sys
work=pathlib.Path(__file__).resolve().parent
meta=json.loads((work/'commands.json').read_text())
name=sys.argv[1]; cwd=pathlib.Path(sys.argv[2]); args=sys.argv[3:]
env=os.environ.copy(); explicit={}
if name.startswith('mapped-'):
    prep='isolated-projects-prepare-final' if name.startswith('mapped-projects-') else 'source-prepare'
    explicit=json.loads((work/'logs'/f'{prep}.stdout').read_text())['buildPreparation']['environment'];env.update(explicit)
started=datetime.datetime.now(datetime.timezone.utc).isoformat()
with (work/'logs'/f'{name}.stdout').open('wb') as out,(work/'logs'/f'{name}.stderr').open('wb') as err:
    p=subprocess.run(args,cwd=cwd,env=env,stdout=out,stderr=err)
meta=json.loads((work/'commands.json').read_text())
meta['commands'].append({'name':name,'argv':args,'cwd':str(cwd),'environment':explicit,'exit_code':p.returncode,'started':started,'finished':datetime.datetime.now(datetime.timezone.utc).isoformat()})
(work/'commands.json').write_text(json.dumps(meta,indent=2)+'\n')
print(name,'exit',p.returncode);print((work/'logs'/f'{name}.stdout').read_text()[-7000:]);print((work/'logs'/f'{name}.stderr').read_text()[-2000:])
sys.exit(p.returncode)
