"""Five nonexecuting plans. No application/native/topology execution branch."""
from pathlib import Path
import argparse,json
P=Path('/tmp/story65-dapr-invoker-ops-prep-20261107')
def main():
 p=argparse.ArgumentParser();p.add_argument('--lane',required=True,choices=['oracle-copy','scratch-producer','live-private','native15','managed-restore']);a=p.parse_args()
 interface=json.loads((P/'oracle-candidate-interface.json').read_text());live=json.loads((P/'live-carrier-skeleton.json').read_text());out={'preparationOnly':True,'executed':False,'sourceQualificationReady':False,'applicationNativeAndHostsNotAuthorized':True,'lane':a.lane}
 if a.lane=='oracle-copy':out.update(oracle=interface['oracle'],noAssemblyLoad=True)
 elif a.lane=='scratch-producer':out.update(command=json.loads((P/'scratch-producer-command-plan.json').read_text())['command'],onlyTempGatewayOutput=True,ordinaryRootModuleReadOnly=True,noRebuildCleanRestore=True)
 elif a.lane=='live-private':out.update(orderedProjects=live['orderedPrimaryBuildProjectsAfterFutureRelease'],primaryBuildCount=10,nativeBuildAdditionalCount=1,futureBuildType=live['buildType'],sourceHook='gateway-source.targets without isolated output redirect',originalControllerUnchanged=True)
 elif a.lane=='native15':out.update(nativePlan=json.loads((P/'native15-plan.json').read_text()))
 else:out.update(futureWindow='8seconds after genuine failed MULTIPLE/no positive/sufficient windows; Gateway invoker local ordinals only',allSixSourceRuntimeProofs=True,privateGatewayMappedInOwnedEventStoreHost=True,noSignalsOrCutoff=True,normalManagedCleanupAnd10SecondReadOnlyAbsencePolling=True,ordinaryRestoreAfterEveryPartialPrivateBuild=True,restore='Gateway producer/carrier changed-new-private union plus actual native Server.Tests artifacts; SDK8/Domain98/scopedServer10/native147 preserved',diagnosticOnlyRegardlessReadinessOrBrowserResult=True)
 print(json.dumps(out,indent=2))
if __name__=='__main__':main()
