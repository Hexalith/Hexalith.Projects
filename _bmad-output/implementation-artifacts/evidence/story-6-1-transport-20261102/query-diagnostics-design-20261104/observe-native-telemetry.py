from pathlib import Path
import datetime,hashlib,importlib.util,json,os,sys

root=Path('/tmp/story61-isolated-20261103-r5tqvlm3/projects')
private=Path('/tmp/supported-project-reads-private-y48gyw4p')
destination=Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/query-diagnostics-design-20261104')
spec=importlib.util.spec_from_file_location('story61_native_telemetry_observation',root/'tools/qualification/run_supported_project_reads.py')
m=importlib.util.module_from_spec(spec);sys.modules[spec.name]=m;spec.loader.exec_module(m)
session=json.loads((private/'apphost-session.json').read_text())
assert m.owns_current_apphost(session)
assert hashlib.sha256((private/'fixture.json').read_bytes()).hexdigest()==session['configurationSha256']
configurations,receipts=m.receiver_configurations(private/'discovery.json',private/'discovery.sqlite',session['_isolation'])
observations=[]
for parts in m.sidecar_processes():
 if '--config' not in parts or parts[parts.index('--config')+1] not in {str(p) for p in configurations.values()}:continue
 pid=int(parts[0]);started=m.process_start(pid)
 app=parts[parts.index('--app-id')+1]
 assert app in configurations and str(configurations[app])==parts[parts.index('--config')+1]
 environment=dict(item.split(b'=',1) for item in Path('/proc',str(pid),'environ').read_bytes().split(b'\0') if b'=' in item)
 assert environment[b'NAMESPACE']==session['_isolation'].encode()
 config=json.loads(configurations[app].read_text())['spec']
 api_flag=parts[parts.index('--enable-api-logging')+1] if '--enable-api-logging' in parts else None
 tracing=config.get('tracing')
 api_logging=config.get('logging',{}).get('apiLogging')
 assert api_flag is None and tracing is None and api_logging is None
 row={'appId':app,'pid':pid,'startTicks':started,'nativeBinarySha256':hashlib.sha256(Path(parts[1]).read_bytes()).hexdigest(),
      'configurationSha256':hashlib.sha256(configurations[app].read_bytes()).hexdigest(),'tracingSectionPresent':tracing is not None,
      'apiLoggingConfigurationPresent':api_logging is not None,'apiLoggingFlagPresent':api_flag is not None,
      'otelEndpointPresent':bool(environment.get(b'OTEL_EXPORTER_OTLP_ENDPOINT')),
      'otelTracesEndpointPresent':bool(environment.get(b'OTEL_EXPORTER_OTLP_TRACES_ENDPOINT'))}
 assert row['otelEndpointPresent'] and not row['otelTracesEndpointPresent']
 assert started is not None and m.process_start(pid)==started
 observations.append(row)
assert len(observations)==len(m.READ_APPS) and {r['appId'] for r in observations}==m.READ_APPS
assert m.owns_current_apphost(session)
manifest=json.loads(Path('/home/administrator/projects/hexalith/projects/_bmad-output/implementation-artifacts/evidence/story-6-1-transport-20261102/live-63/full-replay/source-manifest.json').read_text())
report={'kind':'bounded-read-only-current-owned-native-telemetry-settings','observedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
        'sourceManifestSha256':hashlib.sha256(json.dumps(manifest,sort_keys=True).encode()).hexdigest(),
        'provider':'live-63/full-replay','nativeSettings':sorted(observations,key=lambda r:r['appId']),
        'readOnly':True,'newRuntimeOrInstrumentationStarted':False,'sourceChanged':False,
        'versionedSourceInterpretation':{'inferenceNotRuntimeExportProof':True,'daprVersion':'1.18.7','effectiveRootSamplingRate':'1',
          'apiLoggingEnabled':False,'sampler':'ParentBased(TraceIDRatioBased(rate))',
          'configurationSource':'https://raw.githubusercontent.com/dapr/dapr/v1.18.7/pkg/config/configuration.go',
          'samplerSource':'https://raw.githubusercontent.com/dapr/dapr/v1.18.7/pkg/diagnostics/tracing_sampler.go',
          'exporterSetupSource':'https://raw.githubusercontent.com/dapr/dapr/v1.18.7/pkg/runtime/runtime.go'},
        'wholeChainPrivacyAccepted':False,'nativeExporterEmissionTested':False}
destination.mkdir(mode=0o700)
(destination/'observations.json').write_text(json.dumps(report,indent=2)+'\n')
(destination/'observe-native-telemetry.py').write_bytes(Path(__file__).read_bytes())
(destination/'commands.json').write_text(json.dumps({'command':['python3',str(Path(__file__))],
    'scriptSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    'observationSha256':hashlib.sha256((destination/'observations.json').read_bytes()).hexdigest()},indent=2)+'\n')
print('Retained bounded six-sidecar telemetry observations; no exporter/privacy acceptance claimed.')
