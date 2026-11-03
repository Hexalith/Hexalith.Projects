import sys,json,pathlib,hashlib,re,math
run_number=int(sys.argv[1]);coordinate=json.load(open(f'/tmp/story61-live{run_number}-diagnostic-coordinate.json'));private=pathlib.Path(coordinate['privateDiagnosticDirectory']);evidence=pathlib.Path(coordinate['evidence']);rows=[];hashes={}
allowed={'TenantHash','DomainHash','AggregateHash','Phase','ScopedDecodeReuseConfigured','SerializeLocalLeaseReleaseConfigured','RetainValidatedCatalogBytesConfigured','Status','Exists','CurrentSequence','Erased','ProofPresent','ElapsedMilliseconds','RequestCanceled','Available','CallerCanceled','InitialStatus','FinalStatus','InitialSequence','FinalSequence','ObservationsEqual','TotalElapsedMilliseconds','TaskCanceled','TaskFaulted'}
for owner in ['eventstore','folders']:
 path=private/(owner+'.json');body=path.read_bytes();hashes[path.name]=hashlib.sha256(body).hexdigest()
 for log in json.loads(body)['logs']:
  try: entry=json.loads(log['content'])
  except (ValueError,TypeError):continue
  message=entry.get('Message','');state=entry.get('State',{})
  if not (message.startswith('Private native inspection ') or message.startswith('Private native metadata pair ') or message.startswith('Private owner metadata ')):continue
  row={'resource':owner,'kind':'atomic-pair' if message.startswith('Private native metadata pair ') else 'single-inspection' if owner=='eventstore' else 'owner-read','phaseEvent':'started' if ' started:' in message else 'completed'}
  for key,value in state.items():
   if key not in allowed:continue
   if key.endswith('Hash'):
    assert isinstance(value,str) and re.fullmatch('[0-9A-F]{64}',value)
   elif isinstance(value,str):assert value in ['initial','recheck','available','unsupported','invalid','canceled','faulted','unknown']
   else:assert value is None or isinstance(value,(bool,int,float)) and (not isinstance(value,float) or math.isfinite(value))
   row[key]=value
  rows.append(row)
facts={'sourceProfile':'selected-private-Debug-Development','nativeMetadataBudgetMilliseconds':2000,'folderPermissionCatchupBudgetMilliseconds':500,'observations':rows,'lateAuthorityAccepted':False,'rawLogSha256':hashes,'rawLogsRetainedOwnerPrivate':True,'sourceManifestSha256':hashlib.sha256((evidence/'source-manifest.json').read_bytes()).hexdigest()}
if run_number==21:
 facts.update(baselineIdentification='Folders domain; tenant hash equals organization aggregate hash, head3 matches accepted ACL seed.',firstFreshBaselineReadAvailableBeforeCancellation=True,secondFreshBaselineReadCanceled=True,folderClientReceivedHttpStatusForCanceledBaseline=None,directEffectiveScopedOptionCaptured=True,cacheUseEvidence='scoped-validation-phase-facts.json',createFolderHttpStatus=422,domainRejectionType='Hexalith.Folders.Server.FolderCommandRejected',domainRejectionReason='Not retained; no finer domain code inferred.',independentAuthorizationsPreserved=['FoldersDomainServiceRequestHandler.AuthorizeAsync','RepositoryBackedFolderCreationService.ReauthorizeMutationAsync'])
(evidence/'native-inspection-facts.json').write_text(json.dumps(facts,indent=2)+'\n');print('Retained closed native inspection facts:',len(rows),'events; raw logs remain private.')
