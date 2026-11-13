from pathlib import Path
import json,statistics,hashlib,collections,sys
OVERLAY=Path('/tmp/story65-multiple-overlay-20261107')
OUT=Path('/tmp/story65-multiple-diagnostic-20261107')
spans=[];aggregates=[];coverage=[]
for file in sorted(OVERLAY.glob('*.jsonl')):
 data=file.read_bytes();rows=[json.loads(line) for line in data.splitlines()];begins={};ends={};other=[]
 for row in rows:
  key=(row['component'],row['pid'],row.get('ordinal'))
  if row['state']=='begin':begins[key]=row
  elif row['state']=='end':ends[key]=row
  elif row['state']=='aggregate':aggregates.append(row)
  else:other.append(row)
 for key,begin in begins.items():
  if key not in ends:continue
  end=ends[key]
  spans.append({'component':begin['component'],'pid':begin['pid'],'stage':begin['stage'],'ordinal':begin['ordinal'],'parent':begin['parent'],'startTicks':begin['ticks'],'endTicks':end['ticks'],'elapsedMs':end['elapsedTicks']*1000/end['frequency'],'selfMs':end.get('selfTicks',0)*1000/end['frequency'],'parentStage':begin.get('parentStage'),'phase':begin.get('phase'),'frequency':end['frequency'],'traceId':begin.get('traceId'),'spanId':begin.get('spanId'),'parentSpanId':begin.get('parentSpanId')})
 coverage.append({'file':file.name,'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'records':len(rows),'cap':250000,'capReached':len(rows)>=250000,'pairedSpans':len(begins.keys()&ends.keys()),'unpairedBegins':[{'stage':begins[k]['stage'],'ordinal':k[2]} for k in begins.keys()-ends.keys()],'unpairedEnds':[{'stage':ends[k]['stage'],'ordinal':k[2]} for k in ends.keys()-begins.keys()],'aggregates':sum(r['state']=='aggregate' for r in rows),'otherRecords':len(other)})
spans.sort(key=lambda s:s['startTicks']);roots=[s for s in spans if s['stage']=='advisory.root'];byid={(s['component'],s['pid'],s['ordinal']):s for s in spans}
stats=[]
for (component,pid,stage),items in __import__('itertools').groupby(sorted(spans,key=lambda s:(s['component'],s['pid'],s['stage'])),lambda s:(s['component'],s['pid'],s['stage'])):
 durations=[s['elapsedMs'] for s in items];stats.append({'component':component,'pid':pid,'stage':stage,'count':len(durations),'medianMs':statistics.median(durations),'minMs':min(durations),'maxMs':max(durations)})
rootDetails=[]
for number,root in enumerate(roots,1):
 frequency=root['frequency'];contained=[s for s in spans if s['startTicks']>=root['startTicks'] and s['endTicks']<=root['endTicks']]
 window=[]
 for s in contained:
  copy=dict(s);copy['startMs']=(s['startTicks']-root['startTicks'])*1000/frequency;copy['endMs']=(s['endTicks']-root['startTicks'])*1000/frequency
  window.append(copy)
 agg=[]
 for a in aggregates:
  parent=byid.get((a['component'],a['pid'],a['parent']))
  if parent and parent in contained:
   agg.append({'component':a['component'],'pid':a['pid'],'stage':a['stage'],'parent':a['parent'],'parentStage':a.get('parentStage',parent['stage']),'phase':a.get('phase'),'selfMs':a.get('selfTicks',0)*1000/a['frequency'],'units':a.get('units',0),'minUnits':a.get('minUnits',0),'maxUnits':a.get('maxUnits',0),'count':a['count'],'elapsedMs':a['elapsedTicks']*1000/a['frequency'],'firstMs':(a['firstTicks']-root['startTicks'])*1000/frequency,'lastMs':(a['lastTicks']-root['startTicks'])*1000/frequency})
 rootDetails.append({'number':number,'root':root,'spans':window,'aggregates':agg})
result={'diagnosticOnly':True,'featureAcceptance':False,'limits':['Private timing instrumentation affects latency; diagnostic durations are not ordinary acceptance measurements.','Inclusive spans and concurrent branches overlap. Population medians must not be added to represent an actual request.','Monotonic tick containment aligns processes on this host; assembly parent identifiers are local.','No actor/network residual is claimed to be pure wire latency.'],'coverage':coverage,'stagePopulationSummary':stats,'advisoryRoots':rootDetails}
(OUT/'timing-analysis.json').write_text(json.dumps(result,indent=2)+'\n')
(OUT/'paired-spans.json').write_text(json.dumps(spans,indent=2)+'\n')
print(json.dumps({'files':len(coverage),'records':sum(c['records'] for c in coverage),'pairs':len(spans),'aggregates':len(aggregates),'roots':[r['elapsedMs'] for r in roots],'caps':sum(c['capReached'] for c in coverage),'unpairedBegins':sum(len(c['unpairedBegins']) for c in coverage),'unpairedEnds':sum(len(c['unpairedEnds']) for c in coverage)},indent=2))
