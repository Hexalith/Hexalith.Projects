#!/usr/bin/env python3
"""Retain a command, working directory, output and exit status without shell expansion."""
import json
from pathlib import Path
import subprocess
import sys
root=Path(__file__).resolve().parent
name,cwd,*command=sys.argv[1:]
r=subprocess.run(command,cwd=cwd,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,check=False)
(root/(name+'.txt')).write_text(r.stdout)
p=root/'commands.json'
rows=json.loads(p.read_text()) if p.exists() else []
rows.append(dict(name=name,cwd=cwd,command=command,exit_code=r.returncode,output=name+'.txt'))
p.write_text(json.dumps(rows,indent=2)+'\n')
print(r.stdout[-5000:])
print('EXIT:',r.returncode)
sys.exit(r.returncode)
