import os, subprocess, json, hashlib, datetime, xml.etree.ElementTree as ET
from pathlib import Path
root=Path("/home/administrator/projects/hexalith/projects")
evidence=Path(__file__).parent
mapping=json.loads((evidence/"mapping.stdout.json").read_text())
environment=os.environ.copy()
environment.pop("CustomBeforeMicrosoftCommonTargets", None)
environment.update(mapping["buildPreparation"]["environment"])
environment.update(DOTNET_CLI_USE_MSBUILD_SERVER="0",MSBUILDDISABLENODEREUSE="1",DOTNET_PROCESSOR_COUNT="2")
receipt={"canonicalRevision":subprocess.check_output(["git","rev-parse","HEAD"],cwd=root,text=True).strip(),
 "ordinarySource":True,"diagnosticOverridePresent":False,"environment":{k:environment[k] for k in [*mapping["buildPreparation"]["environment"],"DOTNET_CLI_USE_MSBUILD_SERVER","MSBUILDDISABLENODEREUSE","DOTNET_PROCESSOR_COUNT"]},"commands":[]}
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def save(): (evidence/"native-commands.json").write_text(json.dumps(receipt,indent=2)+"\n")
def run(command,cwd,label):
 print(label+" started",flush=True)
 item={"command":command,"cwd":str(cwd),"startedAtUtc":datetime.datetime.now(datetime.timezone.utc).isoformat()}
 with (evidence/(label+".log")).open("w") as output:
  result=subprocess.run(command,cwd=cwd,env=environment,text=True,stdout=output,stderr=subprocess.STDOUT,timeout=900)
 item.update(exitCode=result.returncode,log=label+".log",logSha256=digest(evidence/(label+".log")))
 receipt["commands"].append(item);save()
 print(label+" exit "+str(result.returncode),flush=True)
 if result.returncode:raise SystemExit(result.returncode)
 return item
selections={
 "UI":["-class","Hexalith.Projects.UI.Tests.*"],
 "Server":["-class","Hexalith.Projects.Server.Tests.Queries.GetProjectContextQueryHandlerTests","-class","Hexalith.Projects.Server.Tests.Queries.GetConversationStartSetupQueryHandlerTests","-class","Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadTests","-class","Hexalith.Projects.Server.Tests.Queries.ProjectAdvisoryQueryTests","-method-","Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadTests.GatewayCatalogRefusesTargetsOutsideCommittedAuthorityWithoutWrites"],
 "Contracts":["-class","Hexalith.Projects.Contracts.Tests.Ui.ProjectResolutionTraceProjectionTests","-class","Hexalith.Projects.Contracts.Tests.Ui.ProjectVocabularyTests"],
 "Integration":["-class","Hexalith.Projects.Integration.Tests.AspireTopologyTests","-class","Hexalith.Projects.Integration.Tests.PersistedReadObserverBindingTests","-class","Hexalith.Projects.Integration.Tests.SupportedProjectAdvisoryReadinessTests"]}
for module,selection in selections.items():
 directory=root/"tests"/("Hexalith.Projects."+module+".Tests")
 run(["dotnet","build","--configuration","Debug","-m:1","-p:NuGetAudit=false","--disable-build-servers"],directory,module.lower()+"-build")
 assembly=directory/"bin/Debug/net10.0"/("Hexalith.Projects."+module+".Tests.dll")
 xml=evidence/(module.lower()+"-tests.xml")
 item=run(["dotnet",str(assembly),*selection,"-xml",str(xml)],directory,module.lower()+"-tests")
 item.update(assemblySha256=digest(assembly),xml=xml.name,xmlSha256=digest(xml),totals=[{k:a.get(k) for k in ["total","passed","failed","skipped","errors"]} for a in ET.parse(xml).getroot().iter("assembly")]);save()
 print(module+" totals: "+json.dumps(item["totals"]),flush=True)
marker="story65-advisory-diagnostic-overlay-20261107".encode("utf-16-le")
receipt["ordinarySourceRestoration"]=[]
for relative in ["src/Hexalith.Projects.Server/bin/Debug/net10.0/Hexalith.Projects.Server.dll","tests/Hexalith.Projects.Server.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.dll","tests/Hexalith.Projects.Integration.Tests/bin/Debug/net10.0/Hexalith.Projects.Server.dll"]:
 path=root/relative
 receipt["ordinarySourceRestoration"].append({"path":relative,"sha256":digest(path),"diagnosticMarkerAbsent":marker not in path.read_bytes()})
save()
if not all(x["diagnosticMarkerAbsent"] for x in receipt["ordinarySourceRestoration"]):raise SystemExit("Diagnostic marker remains")
