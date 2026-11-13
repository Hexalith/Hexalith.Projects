import importlib.util,sys,json,re,os
from pathlib import Path
source=Path('/home/administrator/projects/hexalith/projects/tools/qualification/run_supported_project_reads.py')
sys.path.insert(0,str(source.parent))
spec=importlib.util.spec_from_file_location('focused_managed_launcher',source)
module=importlib.util.module_from_spec(spec);sys.modules[spec.name]=module;spec.loader.exec_module(module)
original=module.execute
def observed(command,*args,**kwargs):
 try:return original(command,*args,**kwargs)
 except RuntimeError as failure:
  if command[:2]==['docker','start']:
   value=getattr(failure,'command_output','')
   value=re.sub(r'\x1b\[[0-9;]*m','',value)
   value=re.sub(r'(?<![\w.])(?:[0-9]{1,3}\.){3}[0-9]{1,3}(?![\w.])','[address]',value)
   value=re.sub(r'/tmp/[^\s\"\']+','[temporary-path]',value)
   target=Path(os.environ['FOCUSED_DOCKER_FAILURE_RECEIPT'])
   target.write_text(json.dumps({'diagnosticOnly':True,'operation':'docker.start','containerId':command[2],'sanitizedCommandOutput':value[:16000]},indent=2)+'\n')
  raise
module.execute=observed
sys.exit(module.main())
