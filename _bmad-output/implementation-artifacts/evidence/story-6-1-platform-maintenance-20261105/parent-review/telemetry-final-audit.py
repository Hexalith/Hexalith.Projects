from pathlib import Path
import datetime,hashlib,json,sqlite3,sys
run=Path(sys.argv[1]).resolve()
console=json.loads((run/'main/native-console.json').read_text())
windows=console['requestWindows']
expected = [(peer['otelServiceName'], peer['otelServiceInstanceId']) for peer in console['peers'].values()]
matches = []
for database in sorted((Path.home() / '.aspire/dashboard/runs').glob('*/dashboard.db')):
    connection = sqlite3.connect(database.as_uri() + '?mode=ro')
    try:
        connection.execute('PRAGMA query_only=ON')
        if not connection.execute("SELECT 1 FROM sqlite_master WHERE type='table' AND name='telemetry_resources'").fetchone():
            continue
        if all(connection.execute('SELECT count(*) FROM telemetry_resources WHERE resource_name=? AND instance_id=?', pair).fetchone()[0] == 1 for pair in expected):
            matches.append(database)
    finally:
        connection.close()
assert len(matches) == 1, 'A unique positive six-peer dashboard binding is required'
database = matches[0]
wal = Path(str(database) + '-wal')
assert not wal.exists() or wal.stat().st_size == 0, 'Wait for the stopped dashboard to finish its checkpoint'
before = database.stat()
connection = sqlite3.connect(database.as_uri() + '?mode=ro')
connection.execute('PRAGMA query_only=ON')
native_span_controls = {}
bindings=[]
positive_channels={table:connection.execute('SELECT count(*) FROM '+table).fetchone()[0] for table in ('telemetry_spans','telemetry_logs','telemetry_metric_exemplars')}
assert all(positive_channels.values()), 'Every telemetry channel needs an ordinary positive collection control'
for service, instance in expected:
    resource_id = connection.execute('SELECT resource_id FROM telemetry_resources WHERE resource_name=? AND instance_id=?', (service, instance)).fetchone()[0]
    native_span_controls[service] = connection.execute('SELECT count(*) FROM telemetry_spans WHERE resource_id=?', (resource_id,)).fetchone()[0]
    bindings.append({'resourceId':resource_id,'serviceName':service,'instanceId':instance,'ordinaryNativeExportedSpans':native_span_controls[service]})
assert all(native_span_controls[service + '-dapr-cli'] > 0 for service in ('eventstore', 'projects', 'folders')), 'Actual read participants need ordinary native exported-span controls'
trace_counts = []
for window in windows:
    counts = {table: connection.execute('SELECT count(*) FROM ' + table + ' WHERE trace_id=?', (window['trace'],)).fetchone()[0] for table in ('telemetry_spans', 'telemetry_logs', 'telemetry_metric_exemplars')}
    assert not any(counts.values()), 'Protected trace-linked telemetry remains'
    trace_counts.append({'kind': window['kind'], 'traceIdFromNativeXml': window['trace'], **counts})
protected = {'query', 'api/v1/queries', 'api/v1/queries/owner-authority/workload', 'api/v1/streams/read/workload'}
metrics = []
for dimension, resource, instance in connection.execute("SELECT d.dimension_id,r.resource_name,r.instance_id FROM telemetry_metric_dimensions d JOIN telemetry_metric_instruments i ON i.instrument_id=d.instrument_id JOIN telemetry_resources r ON r.resource_id=i.resource_id WHERE i.instrument_name='http.server.request.duration'"):
    attributes = dict(connection.execute('SELECT attribute_key,attribute_value FROM telemetry_metric_dimension_attributes WHERE dimension_id=?', (dimension,)))
    route = attributes.get('http.route', '').strip('/')
    if route not in protected or attributes.get('http.request.method') != 'POST':
        continue
    count, maximum = connection.execute('SELECT count(*),max(histogram_count) FROM telemetry_metric_points WHERE dimension_id=?', (dimension,)).fetchone()
    if not count or not maximum:
        continue
    exemplar_count = connection.execute('SELECT count(*) FROM telemetry_metric_exemplars e JOIN telemetry_metric_points p ON p.point_id=e.point_id WHERE p.dimension_id=?', (dimension,)).fetchone()[0]
    assert exemplar_count == 0, 'Protected route metric exemplars remain'
    metrics.append({'resource': resource, 'instanceId': instance, 'route': route, 'httpStatus': attributes.get('http.response.status_code'), 'points': count, 'maxHistogramCount': maximum, 'exemplars': exemplar_count})
assert metrics and any(row['route'] == 'api/v1/queries' and row['httpStatus'] == '404' for row in metrics)
ordinary, protected_spans = 0, 0
for trace_id, span_id, route in connection.execute("SELECT s.trace_id,s.span_id,a.attribute_value FROM telemetry_spans s JOIN telemetry_span_attributes a ON a.trace_id=s.trace_id AND a.span_id=s.span_id WHERE s.kind=2 AND a.attribute_key='http.route'"):
    if route.strip('/') in protected:
        attrs = dict(connection.execute('SELECT attribute_key,attribute_value FROM telemetry_span_attributes WHERE trace_id=? AND span_id=?', (trace_id, span_id)))
        if attrs.get('http.request.method') == 'POST':
            protected_spans += 1
    else:
        ordinary += 1
assert protected_spans == 0 and ordinary > 0
metric_channel_controls=[]
for resource,instance in sorted({(row['resource'],row['instanceId']) for row in metrics}):
    rid=connection.execute('SELECT resource_id FROM telemetry_resources WHERE resource_name=? AND instance_id=?',(resource,instance)).fetchone()[0]
    logs=connection.execute('SELECT count(*) FROM telemetry_logs WHERE resource_id=?',(rid,)).fetchone()[0]
    exemplars=connection.execute('SELECT count(*) FROM telemetry_metric_exemplars e JOIN telemetry_metric_points p ON p.point_id=e.point_id JOIN telemetry_metric_dimensions d ON d.dimension_id=p.dimension_id JOIN telemetry_metric_instruments i ON i.instrument_id=d.instrument_id WHERE i.resource_id=?',(rid,)).fetchone()[0]
    metric_channel_controls.append({'resource':resource,'instanceId':instance,'ordinaryLogs':logs,'ordinaryExemplars':exemplars})
assert any(row['ordinaryLogs']>0 for row in metric_channel_controls) and any(row['ordinaryExemplars']>0 for row in metric_channel_controls)
connection.close()
after = database.stat()
assert (before.st_ino, before.st_size, before.st_mtime_ns) == (after.st_ino, after.st_size, after.st_mtime_ns)
assert not wal.exists() or wal.stat().st_size == 0
result={'observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'sourceManifestSha256':console['sourceManifestSha256'],'database':str(database),'databaseSha256':hashlib.sha256(database.read_bytes()).hexdigest(),'sixOriginalPeerBindingRows':bindings,'positiveChannelTotals':positive_channels,'protectedMetricResourceChannelControls':metric_channel_controls,'databaseMetadataUnchanged':True,'queryOnly':True,'positiveSixOwnedNativeResourcesBinding':True,'traceLinkedTelemetry':trace_counts,'nativeExportedSpanPositiveControls':native_span_controls,'positiveProtectedMetricDimensionsWithoutExemplars':metrics,'protectedDedicatedPostServerSpans':protected_spans,'ordinaryRoutedServerSpanPositiveControl':ordinary,'limits':['Aggregate operation-count inference accepted by the user.','50ms gateway floor; slower and fault variation remains.','Observed current request intervals only; no constant-time, load or universal fault equivalence.']}
(run/'parent-telemetry-final-validation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'sourceManifestSha256':result['sourceManifestSha256'],'positivePeerBinding':True,'traceWindows':len(trace_counts),'protectedTraceLinkedRows':sum(sum(v for k,v in row.items() if k.startswith('telemetry_')) for row in trace_counts),'nativeSpanControls':native_span_controls,'positiveChannels':positive_channels,'protectedMetricDimensions':len(metrics),'protectedPostSpans':protected_spans,'ordinaryServerSpans':ordinary}))
