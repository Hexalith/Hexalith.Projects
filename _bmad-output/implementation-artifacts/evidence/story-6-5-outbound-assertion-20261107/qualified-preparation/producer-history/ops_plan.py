"""Five nonexecuting producer/application lane plans. Application/native/host execution absent."""
from pathlib import Path
import argparse,json
P=Path('/tmp/story65-outbound-assertion-ops-prep-20261107')
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--lane',required=True,choices=['oracle-copy','scratch-producer','live-private','native29','managed-restore']);a=parser.parse_args();out={'preparationOnly':True,'executed':False,'sourceQualificationReady':False,'applicationNativeAndHostsNotAuthorized':True,'lane':a.lane}
 if a.lane=='oracle-copy':out.update(oracle=json.loads((P/'oracle-candidate-interface.json').read_text())['oracle'],noAssemblyLoad=True)
 elif a.lane=='scratch-producer':out.update(command=json.loads((P/'scratch-producer-command-plan.json').read_text())['command'],ordinaryRootModulesReadOnly=True,onlyServerOutputTemp=True,noRestoreRebuildCleanOrGlobalOutputOverrides=True)
 elif a.lane=='live-private':
  live=json.loads((P/'live-carrier-skeleton.json').read_text());out.update(orderedProjects=live['orderedPrimaryProjectsAfterFutureRelease'],primaryBuildCount=11,nativeAdditionalBuildCount=1,onlyServerCompileSubstitution=True,requiredGatewayConsumerBuildUsesOrdinaryGatewaySources=True,futureReferenceGenerationScopeNeedsRootReview=True)
 elif a.lane=='native29':out['nativePlan']=json.loads((P/'native29-plan.json').read_text())
 else:out.update(futureWindow='8seconds after genuine failed MULTIPLE/no genuinepositive/sufficientattemptwindows; only actualEventStore fixedGateway DI population',threeLocalStagesOnly=['Whole','IssueAsyncValueTask','BaseSend'],privateServerMapMandatory=True,sixExistingRuntimeProfilesUnchanged=True,noSignalsCutoffConfigOrDeadlineChanges=True,ordinaryRestoreAfterEveryPartialPrivateBuild=True,restore='Server producer then Gateway and changed/new/private+known affected carrier union plus native artifacts; unrelated recorded generations retained',allPrivateReadinessOrBrowserSuccessRemainsDiagnostic=True)
 print(json.dumps(out,indent=2))
if __name__=='__main__':main()
