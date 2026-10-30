import urllib.request,sys,json,hashlib,datetime
from pathlib import Path
url,target=sys.argv[1:]; path=Path(target); assert not path.exists()
req=urllib.request.Request(url,headers={'User-Agent':'Hexalith-story64-read-only-qualification','Cache-Control':'no-cache'})
with urllib.request.urlopen(req,timeout=90) as response:
 data=response.read(); path.write_bytes(data)
 metadata={'url':url,'final_url':response.url,'status':response.status,'observed_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'headers':{k:v for k,v in response.headers.items() if k.lower() in ['date','etag','last-modified','content-type','content-length']}}
 path.with_suffix(path.suffix+'.http.json').write_text(json.dumps(metadata,indent=2)+'\n')
 print(json.dumps(metadata))
