import json,os,pathlib,sys,urllib.request
root=pathlib.Path.cwd();sys.path.insert(0,str(root/'tools/qualification'))
import run_supported_project_reads as owner
import supported_browser_reads as browser
original=browser.run_browser

def diagnostic_run(root,private,evidence,graph,endpoint,fixture,env,run,assembly,report,run_test_process,group_members):
    try:
        return original(root,private,evidence,graph,endpoint,fixture,env,run,assembly,report,run_test_process,group_members)
    except BaseException:
        report['diagnosticOnly']=True
        report['automatedProofAccepted']=False
        run(['aspire','wait','projects-ui','--apphost',str(root/'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'),'--timeout','120','--non-interactive'])
        run(['aspire','wait','browser-read-observer','--apphost',str(root/'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'),'--timeout','120','--non-interactive'])
        values={'graphId':fixture['IsolationId'],'runId':fixture['IsolationId'],'workerIndex':0,'retry':0,'repeatEachIndex':0,'scenario':'supported-oidc','tenantId':fixture['Tenant'],'principalId':fixture['Browser']['PrimaryActor'],'filePath':'fixture.txt'}
        for key in ['projectId','secondaryProjectId','proposalProjectId','proposalRetryProjectId','degradedProjectId','conversationId','ambiguousConversationId','existingConversationId','staleConversationId','forbiddenConversationId','unavailableConversationId','folderId','secondaryFolderId','proposalFolderId','workspaceId','fileReferenceId','secondaryFileReferenceId','proposalFileReferenceId','deniedFileReferenceId','memoryReferenceId','correlationId','taskId','idempotencyKey']:
            values[key]='owned-'+key.lower()
        values.update(projectId='project-a',secondaryProjectId='project-b',folderId='folder-project-a')
        request=urllib.request.Request(endpoint(graph,'browser-read-observer')+'/_fixtures/graphs',json.dumps(values).encode(),{'Content-Type':'application/json'})
        with urllib.request.urlopen(request,timeout=10) as response:
            if response.status!=200:raise RuntimeError('Diagnostic test observation registration failed')
        access={'uiUrl':endpoint(graph,'projects-ui')+'/inspect/projects','identityProviderOrigin':fixture['Browser']['Origin'],'username':'actor-1','password':fixture['Browser']['Password'],'tenant':fixture['Tenant'],'safeMetadataProject':'project-a','quarantinedProject':'legacy-a'}
        print('FAILED automated proof: private diagnostic hold active; result remains failed.',flush=True)
        report['diagnosticHold']=browser.manual_hold(os.environ['SUPPORTED_READ_MANUAL_HOLD'],access,seconds=3600)
        raise
browser.run_browser=diagnostic_run
raise SystemExit(owner.main())
