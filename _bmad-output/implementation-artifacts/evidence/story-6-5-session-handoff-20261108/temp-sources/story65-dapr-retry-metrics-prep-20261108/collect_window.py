"""One optional external eight-second observation; no launcher or file entry point."""
import time
from metrics_core import ROLES,Refused,get_metrics,parse,delta
from companion_latency import parse_latency,delta_latency

# Only fixed synthetic codes cross the boundary, including from injected guards.
REASONS={'body-or-role-refused','metrics-encoding-refused','metrics-line-cap','malformed-selected-metric','duplicate-selected-label','selected-family-type-refused','selected-counter-number-refused','duplicate-selected-series','sidecar-start-changed','metrics-listener-unobservable','http-header-refused','http-status-not-200','http-content-encoding-refused','http-framing-refused','http-body-cap-or-framing-refused','http-body-cap','endpoint-refused','metrics-connect-unobservable','metrics-read-deadline','http-wire-cap','http-header-cap','companion-body-or-role-refused','companion-encoding-refused','malformed-companion-sample','companion-number-refused','companion-count-refused','duplicate-companion-series','identity-file-refused','sidecar-flag-refused','metrics-enable-flag-refused','owned-ancestry-refused','role-or-binary-pin-refused','owned-sidecar-resource-unobservable','owned-sidecar-wrapper-unobservable','sidecar-binary-hash-changed','sidecar-arguments-cap','sidecar-arguments-refused','sidecar-app-or-config-refused','metrics-port-not-explicit','metrics-disabled-or-flag-refused','sidecar-namespace-refused','sidecar-config-changed','owned-sidecar-unobservable-or-ambiguous','owned-sidecar-wrapper-changed','sidecar-or-listener-changed','binary-pin-refused','sidecar-executable-changed','owned-apphost-start-changed'}

def collect_window(identities,refresh,session_live,postflight):
    """Called ONLY after Root-released ordinary preflight/failed-MULTI trigger.
    identities and private endpoint coordinates stay RAM-only. refresh(role) must
    recheck exact same ownership/start/socket identity and return True; session_live
    checks original launcher/AppHost/CLI liveness, never holds/signals those targets.
    All expensive source/binary/graph preflight occurs before this clock starts.
    """
    if not set(identities).issubset(ROLES):raise Refused('role-or-binary-pin-refused')
    def live():
        try:return session_live()is True
        except Exception:return False
    def reason(refusal):return refusal.args[0]if len(refusal.args)==1 and refusal.args[0]in REASONS else'collector-guard-fault'
    started=time.monotonic();deadline=started+8;snapshots=[];stopped='window-complete'
    try:
        for ordinal,offset in enumerate([0,3,6],1):
            while time.monotonic()<started+offset:
                if not live():stopped='ordinary-session-closed';break
                time.sleep(min(.05,max(0,started+offset-time.monotonic())))
            if stopped!='window-complete':break
            for role in sorted(ROLES):
                if time.monotonic()>=deadline:stopped='window-deadline';break
                if not live():stopped='ordinary-session-closed';break
                row={'role':role,'snapshotOrdinal':ordinal,'category':'sidecar-unobservable','primary':None,'companion':None};begin=time.monotonic();row['startOffsetMilliseconds']=(begin-started)*1000
                if role in identities:
                    body=None
                    try:
                        if refresh(role)is not True:raise Refused('sidecar-or-listener-changed')
                        if not live()or time.monotonic()>=deadline:raise Refused('metrics-read-deadline')
                        identity=identities[role]
                        # A slow/absent endpoint cannot consume the other roles' entire window.
                        body=get_metrics(identity['family'],identity['address'],identity['port'],min(deadline,time.monotonic()+.35))
                        if refresh(role)is not True:raise Refused('sidecar-or-listener-changed')
                        if not live():raise Refused('sidecar-start-changed')
                        row.update(category='observed',sameOwnedProcessAndSocketBeforeAfter=True)
                        try:row['primary']=parse(body,role);row['primaryCategory']='observed'
                        except Refused as refusal:row['primaryCategory']=reason(refusal)
                        try:row['companion']=parse_latency(body,role);row['companionCategory']='observed'
                        except Refused as refusal:row['companionCategory']=reason(refusal)
                    except Refused as refusal:row['category']=reason(refusal)
                    except Exception:row['category']='collector-guard-fault'
                    finally:body=None
                row['elapsedMilliseconds']=(time.monotonic()-begin)*1000;snapshots.append(row)
            if stopped!='window-complete':break
    finally:
        try:window_accepted=postflight()is True
        except Exception:window_accepted=False
    discarded=0
    if not window_accepted:discarded=len(snapshots);snapshots=[];stopped='postflight-guard-refused'
    deltas={}
    for role in sorted(ROLES):
        row={}
        for family,compute in [('primary',delta),('companion',delta_latency)]:
            observed=[r for r in snapshots if r['role']==role and r['category']=='observed'and r.get(family)is not None]
            if len(observed)<2:row[family]={'category':'fewer-than-two-guarded-snapshots','values':None};continue
            first,last=observed[0],observed[-1]
            row[family]={'category':'guarded-snapshot-pair','fromSnapshotOrdinal':first['snapshotOrdinal'],'toSnapshotOrdinal':last['snapshotOrdinal'],'values':compute(first[family],last[family],True)}
        deltas[role]=row
    return {'diagnosticOnly':True,'featureAcceptance':False,'maximumScheduledHttpWindowSeconds':8,'httpWindowElapsedMilliseconds':(time.monotonic()-started)*1000,'stopCategory':stopped,'snapshots':snapshots,'deltas':deltas,'sameProcessCumulativeObservationsOnly':True,'wholeWindowPostflightAccepted':window_accepted,'discardedSnapshotsOnPostflightFailure':discarded,'rawBodyLabelsEndpointsOrPrivateCoordinatesPersisted':False,'noRequestRetryAttemptOrCrossServiceJoin':True}
