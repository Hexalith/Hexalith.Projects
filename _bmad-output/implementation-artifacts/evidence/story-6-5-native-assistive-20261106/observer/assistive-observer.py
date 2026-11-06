import datetime,hashlib,importlib.util,json,os,pathlib,re,select,shutil,signal,subprocess,sys,tempfile,time,urllib.request
ROOT=pathlib.Path.cwd();HOLD=pathlib.Path('/tmp/story65-a11y-live-hold');OUTPUT=pathlib.Path(os.environ.get('STORY65_LIVE_OUTPUT','/tmp/story65-supported-a11y-native-observer-20261106-r1'));OUTPUT.mkdir(mode=0o700,exist_ok=False)
private=pathlib.Path(tempfile.mkdtemp(prefix='assistive-private-',dir='/tmp/story65-a11y-tool-probe'));private.chmod(0o700)
report={'qualification':'actual-assistive-technology-and-native-browser-zoom','humanTestPerformed':False,'productionReadiness':False,'privateCaptureRemoved':False,'checksStartedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat()};processes=[];handles=[];monitor=None;interval={};env=dict(os.environ)
sys.path.insert(0,str(ROOT/'tools/qualification'))
import run_supported_project_reads as owner
from supported_browser_reads import _BrowserTelemetry

def start(args,name,**kw):
 out=(private/(name+'.stdout')).open('w');err=(private/(name+'.stderr')).open('w');handles.extend([out,err]);p=subprocess.Popen(args,env=env,stdout=out,stderr=err,**kw);processes.append(p);return p

def observation(url):
 with urllib.request.urlopen(url,timeout=15) as response:
  if response.status!=200:raise RuntimeError('Test-owned persisted observer did not return success')
  result=json.load(response)
 if not result.get('domain'):raise RuntimeError('Persisted domain observation was empty')
 return result
try:
 if not (HOLD/'access.json').exists():raise RuntimeError('Owned post-proof hold is not ready')
 access=json.loads((HOLD/'access.json').read_text());matches=[]
 for candidate in pathlib.Path('/tmp').glob('supported-project-reads-private-*/fixture.json'):
  if candidate.stat().st_uid!=os.getuid() or candidate.stat().st_mode&0o077:continue
  config=json.loads(candidate.read_text())
  if config.get('Projects',{}).get('SupportedReads',{}).get('Tenant')==access['tenant']:matches.append((candidate,config))
 if len(matches)!=1:raise RuntimeError('Exact private topology configuration is ambiguous')
 configuration,config=matches[0];fixture=config['Projects']['SupportedReads'];report['isolationId']=fixture['IsolationId']
 protected=[fixture['Browser'][key] for key in ['Password','ClientSecret','TenantToken','ForeignToken']]+['Synthetic project project-a','Synthetic descriptive metadata','Restricted legacy description']
 protected_file=private/'protected.json';protected_file.write_text(json.dumps(protected));protected_file.chmod(0o600)
 for name,subdir in [('XDG_CONFIG_HOME','config'),('XDG_DATA_HOME','data'),('XDG_CACHE_HOME','cache'),('XDG_RUNTIME_DIR','runtime')]:
  path=private/subdir;path.mkdir(mode=0o700);env[name]=str(path)
 env.update(NO_AT_BRIDGE='0',GTK_MODULES='gail:atk-bridge',QT_ACCESSIBILITY='1',ACCESSIBILITY_ENABLED='1')
 graph=json.loads(subprocess.run(['aspire','describe','--apphost',str(ROOT/'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'),'--format','Json','--non-interactive'],capture_output=True,text=True,check=True,timeout=30).stdout)
 urls=[url['url'] for resource in graph['resources'] if resource.get('displayName')=='browser-read-observer' for url in resource['urls'] if url.get('name')=='http']
 if len(urls)!=1:raise RuntimeError('Test-owned persisted observer endpoint is ambiguous')
 observation_url=urls[0]+'/api/v1/live-fixtures/graphs/'+fixture['IsolationId']+'/persisted-observation'
 before=observation(observation_url)
 monitor=_BrowserTelemetry(ROOT,protected,owner.test_group_members);monitor.start();interval['startedAtUtc']=monitor.now()
 display=next(i for i in range(760,900) if not pathlib.Path('/tmp/.X%d-lock'%i).exists() and not pathlib.Path('/tmp/.X11-unix/X%d'%i).exists());env['DISPLAY']=':'+str(display)
 xvfb=start(['Xvfb',env['DISPLAY'],'-screen','0','1280x1000x24','-nolisten','tcp','-ac'],'xvfb');deadline=time.time()+10
 while time.time()<deadline:
  if subprocess.run(['xdpyinfo'],env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0:break
  if xvfb.poll() is not None:raise RuntimeError('Owned Xvfb exited')
  time.sleep(.1)
 else:raise RuntimeError('Owned Xvfb was not ready')
 bus=subprocess.run(['gdbus','call','--session','--dest','org.a11y.Bus','--object-path','/org/a11y/bus','--method','org.a11y.Bus.GetAddress'],env=env,capture_output=True,text=True,timeout=10)
 if bus.returncode:raise RuntimeError('AT-SPI bus activation failed')
 env['AT_SPI_BUS_ADDRESS']=re.search("'([^']+)'",bus.stdout)[1]
 speech_config=private/'speech';speech_config.mkdir();speech_log=private/'speech-log';speech_log.mkdir()
 (speech_config/'speechd.conf').write_text('LogLevel 5\nLogDir "'+str(speech_log)+'"\nAddModule "espeak-ng" "sd_espeak-ng" "/etc/speech-dispatcher/modules/espeak-ng.conf"\nDefaultModule espeak-ng\nAudioOutputMethod "alsa"\nAudioALSADevice "null"\n')
 env['SPEECHD_ADDRESS']='unix_socket:'+str(private/'speech.sock');speech=start(['speech-dispatcher','--run-single','--config-dir',str(speech_config),'--socket-path',str(private/'speech.sock'),'--pid-file',str(private/'speech.pid'),'--timeout','0'],'speech');deadline=time.time()+10
 while time.time()<deadline and not (private/'speech.sock').exists():
  if speech.poll() is not None:raise RuntimeError('Owned speech dispatcher exited')
  time.sleep(.1)
 env.update(STORY65_ACCESS_FILE=str(HOLD/'access.json'),STORY65_PROTECTED_FILE=str(protected_file),STORY65_OUTPUT=str(OUTPUT),STORY65_ORCA_DEBUG=str(private/'orca-debug.log'),STORY65_ZOOM_PROFILE=str(private/'chrome-profile'))
 err=(private/'browser.stderr').open('w');handles.append(err)
 browser=subprocess.Popen(['node','/tmp/story65-a11y-tool-probe/live.cjs'],env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=err,text=True);processes.append(browser)
 ready,_,_=select.select([browser.stdout],[],[],90)
 if not ready:raise RuntimeError('Owned browser did not finish OIDC')
 authenticated=json.loads(browser.stdout.readline())
 if authenticated.get('stage')!='authenticated' or not authenticated.get('safeMetadata'):raise RuntimeError('Owned browser did not prove post-OIDC Safe Metadata')
 report['screenReaderCaptureStartedAfterAuthentication']=True
 orca=start(['orca','--debug-file',str(private/'orca-debug.log')],'orca');deadline=time.time()+15
 while time.time()<deadline:
  if orca.poll() is not None:raise RuntimeError('Owned Orca exited')
  debug=private/'orca-debug.log'
  if debug.exists() and 'SPEECH OUTPUT:' in debug.read_text(errors='replace'):break
  time.sleep(.1)
 browser.stdin.write('run\n');browser.stdin.flush()
 deadline=time.time()+1100;done=False
 while time.time()<deadline:
  ready,_,_=select.select([browser.stdout],[],[],2)
  if ready:
   line=browser.stdout.readline()
   if not line:break
   status=json.loads(line)
   print(json.dumps(status),flush=True)
   if status.get('stage')=='complete':done=True;report['browserResult']=status;break
  if browser.poll() is not None:break
 if not done:raise RuntimeError('Owned assistive browser did not complete its checks')
 browser.wait(timeout=20)
 after=observation(observation_url)
 persisted={'before':before,'after':after,'domainUnchanged':before['domain']==after['domain'],'inspectionAuditUnchanged':before.get('audit')==after.get('audit')}
 (OUTPUT/'persisted-boundary.json').write_text(json.dumps(persisted,indent=2)+'\n')
 report['domainUnchanged']=persisted['domainUnchanged'];report['safeMetadataAuditUnchanged']=persisted['inspectionAuditUnchanged']
 report['versions']={'orca':subprocess.run(['orca','--version'],capture_output=True,text=True).stdout.strip(),'chrome':subprocess.run(['google-chrome','--version'],capture_output=True,text=True).stdout.strip(),'speechDispatcher':'0.12.1','speechSynthesis':'espeak-ng 1.52.0; ALSA null output (actual engine, no audible human evaluation)'}
 report['result']='passed' if report['browserResult']['result']=='passed' and report['domainUnchanged'] and report['safeMetadataAuditUnchanged'] else 'failed'
except BaseException as e:
 report['result']='failed';report['failureType']=type(e).__name__;report['failure']=str(e)[:200]
finally:
 for proc in reversed(processes):
  if proc.poll() is None:
   proc.terminate()
   try:proc.wait(timeout=8)
   except subprocess.TimeoutExpired:proc.kill();proc.wait(timeout=5)
 for h in handles:h.close()
 report['ownedAssistiveProcessesAbsent']=all(proc.poll() is not None for proc in processes)
 if monitor:
  interval['endedAtUtc']=monitor.now();telemetry=monitor.finish(interval)
  (OUTPUT/'telemetry.json').write_text(json.dumps(telemetry,indent=2)+'\n');report['telemetryComplete']=telemetry['completeIntervalObserved'];report['telemetryProtectedAbsent']=telemetry['protectedMarkerAbsent']
  if not report['telemetryComplete'] or not report['telemetryProtectedAbsent']:report['result']='failed'
 speech_log_path=private/'speech-log/speech-dispatcher.log'
 if speech_log_path.exists():
  chunks=audio_samples=0
  with speech_log_path.open(errors='replace') as handle:
   for line in handle:
    if 'Got audio parameter num_samples ' in line:
     chunks+=1
     match=re.search(r'num_samples (\d+)',line)
     if match:audio_samples+=int(match[1])
  report['speechSynthesis']={'engine':'espeak-ng','audioSink':'ALSA null','audioChunksGenerated':chunks,'audioSamplesGenerated':audio_samples,'audibleHumanEvaluation':False}
 report['privateCaptureSha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in private.glob('*.log') if p.is_file()}
 shutil.rmtree(private);report['privateCaptureRemoved']=not private.exists();report['checksEndedAtUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
 (OUTPUT/'run.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ['privateCaptureSha256']},indent=2),flush=True)
