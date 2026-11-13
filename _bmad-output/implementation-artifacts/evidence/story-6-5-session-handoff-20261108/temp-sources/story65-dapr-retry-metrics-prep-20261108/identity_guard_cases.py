"""Additional pure mocked source cases. No real proc, socket, FD or large hash."""
from contextlib import ExitStack
from pathlib import Path as RealPath
from types import SimpleNamespace
from unittest import TestCase
from unittest.mock import MagicMock,patch,call
import itertools,socket,stat
import sidecar_identity as identity
from metrics_core import Refused,parse,delta
from collect_window import collect_window

PIN='a'*64
BINARY_STAT=SimpleNamespace(st_dev=1,st_ino=2,st_size=100,st_mtime_ns=3,st_ctime_ns=4,st_mode=stat.S_IFREG|0o700)
CONFIG_STAT=SimpleNamespace(st_dev=1,st_ino=3,st_size=10,st_mtime_ns=5,st_ctime_ns=6,st_mode=stat.S_IFREG|0o600)

def sample(role='projects',name='supported-reads-project-state',value=1):
    row=f'app_id="{role}",name="{name}",namespace="",policy="retry",flow_direction="outbound",target="app_eventstore",status=""'
    return(f'dapr_resiliency_count{{{row}}} {value}\ndapr_resiliency_activations_total{{{row}}} {value}\n').encode()

class Fixture:
    """Synthetic identities only; all actual collector functions remain under test."""
    def __init__(self):
        self.rows={100:{'state':'S','start':'10','parent':1,'group':100,'session':100},110:{'state':'R','start':'11','parent':100,'group':110,'session':110},120:{'state':'R','start':'12','parent':110,'group':110,'session':110}}
        self.owned={'appHostPid':100,'appHostStartTicks':'10','_isolation':'synthetic-owned'}
        self.graph={'resources':[{'displayName':'projects-dapr-cli','properties':{'executable.pid':110}}]}
        self.config={'kind':'owned-dapr-runtime-configuration','catalogDirectory':'/tmp/hexalith-route-catalog-synthetic','path':'/tmp/hexalith-route-catalog-synthetic/projects.configuration.json','sha256':PIN}
        self.binary={'fd':7,'statKey':identity.stat_key(BINARY_STAT),'sha256':PIN}
        self.arguments=['synthetic-daprd','--app-id','projects','--config',self.config['path'],'--metrics-port','1234','--enable-metrics=true']
        self.namespace='synthetic-owned';self.turn=0;self.config_stat=CONFIG_STAT
        self.dpr=MagicMock();self.dpr.lstat.return_value=BINARY_STAT;self.guard=MagicMock()
    def process(self,pid):
        if pid not in self.rows:return None
        self.turn+=1;return{**self.rows[pid],'state':'R'if self.turn%2 else'S'}
    def path(self,*parts):
        text='/'.join(str(p).strip('/')for p in parts)
        if text.startswith('proc/'):
            pid=int(parts[1]);leaf=parts[-1];value=MagicMock()
            if leaf=='exe':value.readlink.return_value=self.dpr if pid==120 else RealPath('/synthetic-wrapper');value.stat.return_value=BINARY_STAT
            elif leaf=='cmdline':value.read_bytes.side_effect=lambda:b'\0'.join(s.encode()for s in self.arguments)+b'\0'
            elif leaf=='environ':value.read_bytes.side_effect=lambda:('NAMESPACE='+self.namespace+'\0').encode()
            return value
        if str(parts[0])==self.config['path']:
            value=MagicMock();value.lstat.side_effect=lambda:self.config_stat;return value
        return RealPath(*parts)
    def patches(self):
        stack=ExitStack()
        stack.enter_context(patch.object(identity,'DPR',self.dpr));stack.enter_context(patch.object(identity,'Path',side_effect=self.path))
        stack.enter_context(patch.object(identity,'process',side_effect=self.process));stack.enter_context(patch.object(identity.os,'fstat',return_value=BINARY_STAT))
        self.config_proof=stack.enter_context(patch.object(identity,'file_proof',return_value=identity.stat_key(CONFIG_STAT)))
        self.socket=stack.enter_context(patch.object(identity,'listener',return_value=(socket.AF_INET,'127.0.0.1','99')))
        # Any accidental per-role/scrape hash is a fixture failure.
        self.hash=stack.enter_context(patch.object(identity,'hash_fd',side_effect=AssertionError('unexpected-large-hash-in-point-guard')))
        return stack
    def classify(self):return identity.classify(self.owned,self.graph,'projects',self.config,self.binary,self.guard,self.rows)
    def recheck(self,value):return identity.recheck(value,self.owned,self.binary,self.guard)

class FocusedIdentityGuards(TestCase):
    def test_per_family_counter_meaning_is_distinct(self):
        rows=delta(parse(sample(value=1),'projects'),parse(sample(value=3),'projects'),True)
        self.assertEqual(rows['retry_activation']['meaning'],'first-retry-notification-increments')
        self.assertEqual(rows['retry_execution']['meaning'],'policy-application-increments')
        self.assertTrue(rows['retry_execution']['notWireAttemptCount']);self.assertTrue(rows['retry_activation']['notWireAttemptCount'])
    def test_impossible_owner_app_eventstore_tuple_discarded(self):
        rows=parse(sample('eventstore','supported-reads-owner-deadline'),'eventstore')
        self.assertEqual(rows['retry_execution']['series'],{});self.assertEqual(rows['retry_activation']['series'],{})
    def test_open_and_finish_hash_exactly_twice_close_once(self):
        dpr=MagicMock();dpr.lstat.return_value=BINARY_STAT
        with patch.object(identity,'DPR',dpr),patch.object(identity.os,'open',return_value=7),patch.object(identity.os,'fstat',return_value=BINARY_STAT),patch.object(identity,'hash_fd',return_value=PIN)as hashed,patch.object(identity.os,'close')as close:
            proof=identity.open_binary(PIN);self.assertTrue(identity.finish_binary(proof))
            self.assertEqual(hashed.call_args_list,[call(7),call(7)]);close.assert_called_once_with(7)
    def test_open_hash_failure_always_closes_admitted_descriptor(self):
        with patch.object(identity.os,'open',return_value=7),patch.object(identity.os,'fstat',return_value=BINARY_STAT),patch.object(identity,'hash_fd',return_value='b'*64),patch.object(identity.os,'close')as close:
            with self.assertRaises(Refused):identity.open_binary(PIN)
            close.assert_called_once_with(7)
    def test_finish_hash_failure_closes_and_invalidates_window(self):
        dpr=MagicMock();dpr.lstat.return_value=BINARY_STAT;ticks=itertools.count(0,.1)
        with patch.object(identity,'DPR',dpr),patch.object(identity.os,'open',return_value=7),patch.object(identity.os,'fstat',return_value=BINARY_STAT),patch.object(identity,'hash_fd',side_effect=[PIN,'b'*64])as hashed,patch.object(identity.os,'close')as close:
            proof=identity.open_binary(PIN)
            with patch('collect_window.time.monotonic',side_effect=lambda:next(ticks)),patch('collect_window.time.sleep'):
                result=collect_window({},lambda role:True,lambda:True,lambda:identity.finish_binary(proof))
            self.assertFalse(result['wholeWindowPostflightAccepted']);self.assertEqual(result['snapshots'],[]);self.assertGreater(result['discardedSnapshotsOnPostflightFailure'],0)
            self.assertEqual(hashed.call_count,2);close.assert_called_once_with(7)
    def test_application_settings_not_dapr_runtime_config(self):
        fixture=Fixture();fixture.config={'kind':'app-settings','path':'/synthetic/fixture.json','sha256':PIN}
        with fixture.patches():
            with self.assertRaises(Refused):fixture.classify()
    def test_graph_wrapper_must_descend_from_owned_apphost(self):
        fixture=Fixture();fixture.rows[110]['parent']=999
        with fixture.patches():
            with self.assertRaises(Refused):fixture.classify()
    def test_positive_classify_recheck_accept_rs_but_refuse_changed_start(self):
        fixture=Fixture()
        with fixture.patches():
            value=fixture.classify();self.assertTrue(fixture.recheck(value));fixture.hash.assert_not_called();fixture.config_proof.assert_called_once_with(fixture.config['path'],PIN)
            fixture.rows[120]['start']='99'
            with self.assertRaises(Refused):fixture.recheck(value)
    def test_namespace_and_flags_are_refused(self):
        for mode in ['namespace','app','metrics','duplicate-config']:
            with self.subTest(mode=mode):
                fixture=Fixture()
                if mode=='namespace':fixture.namespace='synthetic-other'
                elif mode=='app':fixture.arguments[2]='synthetic-other'
                elif mode=='metrics':fixture.arguments[6]='0'
                else:fixture.arguments+=['--config',fixture.config['path']]
                with fixture.patches():
                    with self.assertRaises(Refused):fixture.classify()
    def test_socket_config_and_parent_changes_refused(self):
        for mode in ['socket','config','parent']:
            with self.subTest(mode=mode):
                fixture=Fixture()
                with fixture.patches():
                    value=fixture.classify()
                    if mode=='socket':fixture.socket.return_value=(socket.AF_INET,'127.0.0.1','100')
                    elif mode=='config':fixture.config_stat=SimpleNamespace(**{**vars(CONFIG_STAT),'st_ino':4})
                    else:fixture.rows[120]['parent']=999
                    with self.assertRaises(Refused):fixture.recheck(value)
                    fixture.hash.assert_not_called()
