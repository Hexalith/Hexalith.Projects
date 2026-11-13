"""Five nonexecuting plans. This skeleton has no application/native/lifecycle execute branch."""
from pathlib import Path
import argparse,json
P=Path('/tmp/story65-gateway-controller-ops-prep-20261107')
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--lane',required=True,choices=['oracle-copy','scratch-producer','live-private','native40','managed-restore']);a=parser.parse_args()
 interface=json.loads((P/'oracle-candidate-interface.json').read_text());live=json.loads((P/'live-carrier-skeleton.json').read_text());base={'preparationOnly':True,'executed':False,'sourceQualificationReady':False,'applicationNativeAndHostsNotAuthorized':True,'lane':a.lane}
 if a.lane=='oracle-copy':base.update(oracle=interface['oracle'],noAssemblyLoad=True)
 elif a.lane=='scratch-producer':base.update(command=json.loads((P/'scratch-producer-command-plan.json').read_text())['command'],onlyTempGatewayOutput=True,ordinaryRootModuleReadOnly=True,noRebuildCleanRestore=True)
 elif a.lane=='live-private':base.update(orderedProjects=live['orderedSourceProfileNineBuildsAfterFutureRelease'],futureBuildType='FORCE refsfalse only after actual ALC qualification and root operational review',sourceHook='gateway-source.targets without scratch output redirect')
 elif a.lane=='native40':base.update(expectedNativeCases=40,expectedSourceSelections=[{'class':'Hexalith.EventStore.Server.Tests.Controllers.BoundOwnerQueryControllerTests','cases':1},{'class':'Hexalith.EventStore.Server.Tests.Controllers.BoundOwnerQueryAuthorityTests','cases':15},{'class':'Hexalith.EventStore.Server.Tests.Routing.BoundOwnerCatalogAdmissionTests','cases':19},{'class':'Hexalith.EventStore.Server.Tests.Controllers.ProjectionCurrentAuthorityTests','cases':4},{'method':'Hexalith.EventStore.Server.Tests.Controllers.QueriesControllerTests.CatalogBackedOwnerAuthoritySubmitsResolveTenantOwnerAuthorityWithNullProjection','cases':1}],fullHistoricalXml40Absent=True,only19CasesPreviouslyXmlCorroborated=True,noHistoricalRetentionOrOrdinaryUniqueCountClaim=True,actualPrivateGatewayMapMandatory=True,filterAndExactObservedCanonicalXmlPending=True)
 else:base.update(futureWindow='8seconds after genuine failedMULTIPLE/no positive/sufficient windows; fixedgateway local ordinals only',allSixSourceRuntimeProofs=True,privateGatewayMappedInOwnedEventStoreHost=True,noSignalsOrCutoff=True,normalManagedCleanupAnd10SecondReadOnlyAbsencePolling=True,ordinaryRestoreAfterEveryPartialPrivateBuild=True,restore='Gateway producer/carrier changed-new-private union plus actual native Server.Tests artifacts; SDK8/Domain98/Server10/native147 preserved',acceptanceRemainsUnexercised=True)
 print(json.dumps(base,indent=2))
if __name__=='__main__':main()
