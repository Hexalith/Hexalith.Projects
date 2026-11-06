import json,os,pathlib,re,select,signal,subprocess,tempfile,time
root=pathlib.Path(tempfile.mkdtemp(prefix='session-',dir='/tmp/story65-a11y-tool-probe'));root.chmod(0o700)
result={'qualification':'environment-feasibility-only','featureAcceptance':False,'actualFeatureRoutesChecked':[],'isolatedSession':True,'privateRoot':str(root)}
env=dict(os.environ)
for name,subdir in [('XDG_CONFIG_HOME','config'),('XDG_DATA_HOME','data'),('XDG_CACHE_HOME','cache'),('XDG_RUNTIME_DIR','runtime')]:
 path=root/subdir;path.mkdir(mode=0o700);env[name]=str(path)
env.update(NO_AT_BRIDGE='0',GTK_MODULES='gail:atk-bridge',QT_ACCESSIBILITY='1',ACCESSIBILITY_ENABLED='1')
processes=[];handles=[]
def start(args,name,**kw):
 out=(root/(name+'.stdout')).open('w');err=(root/(name+'.stderr')).open('w');handles.extend([out,err]);p=subprocess.Popen(args,env=env,stdout=out,stderr=err,**kw);processes.append(p);return p
try:
 display=next(i for i in range(760,900) if not pathlib.Path('/tmp/.X%d-lock'%i).exists() and not pathlib.Path('/tmp/.X11-unix/X%d'%i).exists());env['DISPLAY']=':'+str(display)
 xvfb=start(['Xvfb',env['DISPLAY'],'-screen','0','1280x900x24','-nolisten','tcp','-ac'],'xvfb')
 deadline=time.time()+10
 while time.time()<deadline:
  if subprocess.run(['xdpyinfo'],env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode==0:break
  if xvfb.poll() is not None:raise RuntimeError('Owned Xvfb exited')
  time.sleep(.1)
 else:raise RuntimeError('Owned Xvfb was not ready')
 bus=subprocess.run(['gdbus','call','--session','--dest','org.a11y.Bus','--object-path','/org/a11y/bus','--method','org.a11y.Bus.GetAddress'],env=env,capture_output=True,text=True,timeout=10)
 if bus.returncode:raise RuntimeError('AT-SPI bus activation failed')
 env['AT_SPI_BUS_ADDRESS']=re.search("'([^']+)'",bus.stdout)[1]
 config=root/'speech';config.mkdir();log=root/'speech-log';log.mkdir()
 (config/'speechd.conf').write_text('LogLevel 5\nLogDir "'+str(log)+'"\nAddModule "espeak-ng" "sd_espeak-ng" "/etc/speech-dispatcher/modules/espeak-ng.conf"\nDefaultModule espeak-ng\nAudioOutputMethod "alsa"\nAudioALSADevice "null"\n')
 env['SPEECHD_ADDRESS']='unix_socket:'+str(root/'speech.sock')
 speech=start(['speech-dispatcher','--run-single','--config-dir',str(config),'--socket-path',str(root/'speech.sock'),'--pid-file',str(root/'speech.pid'),'--timeout','0'],'speech')
 deadline=time.time()+10
 while time.time()<deadline and not (root/'speech.sock').exists():
  if speech.poll() is not None:raise RuntimeError('Owned speech dispatcher exited')
  time.sleep(.1)
 orca=start(['orca','--debug-file',str(root/'orca-debug.log')],'orca')
 node=root/'probe.cjs';node.write_text(r'''const { chromium } = require(process.cwd() + '/tests/e2e/node_modules/playwright');
const readline = require('node:readline');
(async () => {
 const context = await chromium.launchPersistentContext(process.env.STORY65_ZOOM_PROFILE, {executablePath:'/usr/bin/google-chrome',headless:false,viewport:null,env:process.env,args:['--no-sandbox','--disable-gpu','--disable-dev-shm-usage','--ozone-platform=x11','--force-renderer-accessibility=complete','--window-size=1280,900']});
 const page=context.pages()[0];
 await page.setContent('<title>Story65NativeZoomToolProbe</title><main><h1>Screen reader tool probe</h1><button>Read supported observations</button><label><input type="checkbox">Descriptive inspection</label><details><summary>Enumeration evidence</summary><p>State: Complete</p></details></main>');
 console.log(JSON.stringify({stage:'initial',...(await page.evaluate(()=>({width:innerWidth,height:innerHeight,devicePixelRatio,cssZoom:getComputedStyle(document.documentElement).zoom})))}));
 const lines=readline.createInterface({input:process.stdin});
 for await(const line of lines){
  if(line==='metrics')console.log(JSON.stringify({stage:'after-native-shortcuts',...(await page.evaluate(()=>({width:innerWidth,height:innerHeight,devicePixelRatio,cssZoom:getComputedStyle(document.documentElement).zoom})))}));
  if(line==='close')break;
 }
 lines.close();await context.close();
})().catch(e=>{console.error(e.stack);process.exitCode=1;});''')
 env['STORY65_ZOOM_PROFILE']=str(root/'chrome-profile')
 err=(root/'browser.stderr').open('w');handles.append(err)
 browser=subprocess.Popen(['node',str(node)],env=env,stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=err,text=True);processes.append(browser)
 ready,_,_=select.select([browser.stdout],[],[],30)
 if not ready:raise RuntimeError('Owned browser did not publish initial metrics')
 line=browser.stdout.readline()
 if not line:raise RuntimeError('Owned browser exited before metrics')
 result['initial']=json.loads(line)
 window=subprocess.run(['xdotool','search','--onlyvisible','--name','Story65NativeZoomToolProbe'],env=env,capture_output=True,text=True,timeout=10).stdout.strip().splitlines()[-1]
 subprocess.run(['xdotool','windowfocus','--sync',window],env=env,check=True,timeout=10)
 subprocess.run(['xdotool','key','--clearmodifiers','ctrl+0'],env=env,check=True,timeout=10)
 for i in range(5):
  subprocess.run(['xdotool','key','--clearmodifiers','ctrl+equal'],env=env,check=True,timeout=10);time.sleep(.2)
 browser.stdin.write('metrics\n');browser.stdin.flush();ready,_,_=select.select([browser.stdout],[],[],10)
 if not ready:raise RuntimeError('Owned browser did not publish final metrics')
 result['after']=json.loads(browser.stdout.readline());result['nativeZoom200Available']=result['after']['devicePixelRatio']==2 and result['after']['cssZoom']=='1'
 for key in ['ctrl+0','ctrl+l','Escape','Tab','Tab','space','Tab','space']:
  subprocess.run(['xdotool','key','--clearmodifiers',key],env=env,check=True,timeout=10);time.sleep(.5)
 time.sleep(2)
 debug=(root/'orca-debug.log').read_text(errors='replace') if (root/'orca-debug.log').exists() else ''
 result['announcements']=[re.sub(r'^.*SPEECH OUTPUT: ', '',line) for line in debug.splitlines() if 'SPEECH OUTPUT:' in line][-45:]
 result['screenReaderRuntimeRunning']=orca.poll() is None
 result['speechDispatcherRunning']=speech.poll() is None
 result['screenReaderActualAnnouncements']=bool(result['announcements'])
 result['versions']={name:subprocess.run(args,capture_output=True,text=True).stdout.strip() for name,args in [('orca',['orca','--version']),('speechDispatcher',['speech-dispatcher','--version']),('chrome',['google-chrome','--version'])]}
 browser.stdin.write('close\n');browser.stdin.flush();browser.wait(timeout=15)
except Exception as e:
 result['errorType']=type(e).__name__;result['error']=str(e)
finally:
 for proc in reversed(processes):
  if proc.poll() is None:
   proc.terminate()
   try:proc.wait(timeout=8)
   except subprocess.TimeoutExpired:proc.kill();proc.wait(timeout=5)
 for h in handles:h.close()
 result['ownedProcessesAbsent']=all(proc.poll() is not None for proc in processes)
 (root/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2),flush=True)
