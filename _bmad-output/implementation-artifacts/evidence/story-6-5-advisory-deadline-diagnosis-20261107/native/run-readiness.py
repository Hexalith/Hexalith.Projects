import os, subprocess, json, hashlib, xml.etree.ElementTree as ET
from pathlib import Path
root=Path("/home/administrator/projects/hexalith/projects");evidence=Path(__file__).parent
path=evidence/"native-commands.json";receipt=json.loads(path.read_text());environment=os.environ.copy()
environment.pop("CustomBeforeMicrosoftCommonTargets",None);environment.update(receipt["environment"])
directory=root/"tests/Hexalith.Projects.Integration.Tests"
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def save():path.write_text(json.dumps(receipt,indent=2)+"\n")
def run(command,label):
 print(label+" started",flush=True)
 with (evidence/(label+".log")).open("w") as output: result=subprocess.run(command,cwd=directory,env=environment,text=True,stdout=output,stderr=subprocess.STDOUT,timeout=900)
 item={"cwd":str(directory),"command":command,"exitCode":result.returncode,"log":label+".log","logSha256":digest(evidence/(label+".log"))};receipt["commands"].append(item);save();print(label+" exit "+str(result.returncode),flush=True)
 if result.returncode:raise SystemExit(result.returncode)
 return item
run(["dotnet","build","--configuration","Debug","-m:1","-p:NuGetAudit=false","--disable-build-servers","-p:SupportedProjectReadDistributed=true"],"integration-readiness-build")
assembly=directory/"bin/Debug/net10.0/Hexalith.Projects.Integration.Tests.dll";xml=evidence/"integration-readiness-tests.xml"
item=run(["dotnet",str(assembly),"-class","Hexalith.Projects.Integration.Tests.SupportedProjectAdvisoryReadinessTests","-xml",str(xml)],"integration-readiness-tests")
item.update(assemblySha256=digest(assembly),xml=xml.name,xmlSha256=digest(xml),totals=[{k:a.get(k) for k in ["total","passed","failed","skipped","errors"]} for a in ET.parse(xml).getroot().iter("assembly")]);save();print(json.dumps(item["totals"]),flush=True)
marker="story65-advisory-diagnostic-overlay-20261107".encode("utf-16-le")
receipt["ordinarySourceRestorationAfterReadiness"]=[]
for item in receipt["ordinarySourceRestoration"]:
 p=root/item["path"];receipt["ordinarySourceRestorationAfterReadiness"].append({"path":item["path"],"sha256":digest(p),"diagnosticMarkerAbsent":marker not in p.read_bytes()})
save()
if not all(x["diagnosticMarkerAbsent"] for x in receipt["ordinarySourceRestorationAfterReadiness"]):raise SystemExit("Diagnostic marker remains")
