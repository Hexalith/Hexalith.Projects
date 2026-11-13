"""Pure mocked qualification source ONLY; not executed/released during preparation."""
import unittest
from metrics_core import parse,delta,decode_response,get_metrics,Refused
from unittest.mock import patch,MagicMock
from companion_latency import parse_latency,delta_latency
import errno,socket,itertools
from metrics_core import listener,stable_process
from sidecar_identity import chain
from collect_window import collect_window

def exposition(value=1,activation=True,extra='',name='supported-reads-project-state',namespace=''):
    labels=f'app_id="projects",name="{name}",namespace="{namespace}",policy="retry",flow_direction="outbound",target="app_eventstore",status=""{extra}'
    text=f'# TYPE dapr_resiliency_count counter\ndapr_resiliency_count{{{labels}}} {value}\n'
    if activation:text+=f'# TYPE dapr_resiliency_activations_total counter\ndapr_resiliency_activations_total{{{labels}}} {value}\n'
    return text.encode()

class Guards(unittest.TestCase):
    def test_missing_family_is_not_zero(self):
        result=parse(b'', 'projects')
        self.assertFalse(result['retry_activation']['familyDeclared']);self.assertEqual(result['retry_activation']['series'],{})
    def test_observed_zero_is_distinct_from_missing(self):
        result=parse(exposition(0),'projects')
        self.assertEqual(list(result['retry_activation']['series'].values()),[0]);self.assertTrue(result['retry_activation']['familyDeclared'])
    def test_same_series_delta_and_no_attempt_inference(self):
        result=delta(parse(exposition(3),'projects'),parse(exposition(5),'projects'),True)
        self.assertEqual(result['retry_activation']['deltas'][0]['delta'],2)
    def test_same_start_counter_decrease_is_not_negative_delta(self):
        result=delta(parse(exposition(5),'projects'),parse(exposition(3),'projects'),True)
        self.assertEqual(result['retry_activation']['deltas'][0],{'bucket':'project_state/empty/unspecified','category':'counter-decreased','delta':None})
    def test_changed_identity_refuses_delta(self):
        result=delta(parse(exposition(3),'projects'),parse(exposition(5),'projects'),False)
        self.assertIsNone(result['retry_activation']['deltas'][0]['delta'])
    def test_new_series_baseline_not_synthesized_zero(self):
        result=delta(parse(exposition(3,False),'projects'),parse(exposition(5),'projects'),True)
        self.assertEqual(result['retry_activation']['deltas'][0]['category'],'newly-observed-positive')
    def test_exact_label_set_unknown_label_discarded(self):
        result=parse(exposition(extra=',route="synthetic-unknown"'),'projects')
        self.assertEqual(result['retry_activation']['series'],{});self.assertNotIn('synthetic-unknown',str(result))
    def test_namespace_not_private_runtime_namespace(self):
        self.assertEqual(parse(exposition(namespace='synthetic-isolation'),'projects')['retry_activation']['series'],{})
    def test_duplicate_view_series_never_summed(self):
        with self.assertRaises(Refused):parse(exposition()+exposition(),'projects')
    def test_alias_not_counted_twice(self):
        result=parse(exposition()+b'dapr_resiliency_count_total{app_id="projects"} 999\n','projects')
        self.assertEqual(result['retry_execution']['samplesSeen'],1)
    def test_wrong_resource_scope_not_adopted(self):
        self.assertEqual(parse(exposition(name='projects-local-resiliency'),'projects')['retry_activation']['series'],{})
    def test_redirect_never_followed(self):
        with self.assertRaises(Refused):decode_response(b'HTTP/1.1 302 Redirect\r\nLocation: /other\r\n\r\n')
    def test_content_length_and_chunked_exact(self):
        self.assertEqual(decode_response(b'HTTP/1.1 200 OK\r\nContent-Length: 3\r\n\r\nabc'),b'abc')
        self.assertEqual(decode_response(b'HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n3\r\nabc\r\n0\r\n\r\n'),b'abc')
    def test_framing_partial_body_unobservable(self):
        with self.assertRaises(Refused):decode_response(b'HTTP/1.1 200 OK\r\nContent-Length: 4\r\n\r\nabc')
    def test_zero_baseline_is_not_a_clean_delta(self):
        result=delta(parse(exposition(0),'projects'),parse(exposition(5),'projects'),True)
        self.assertIsNone(result['retry_activation']['deltas'][0]['delta'])
    def test_hard_deadline_with_mocked_socket_only(self):
        stream=MagicMock();stream.connect_ex.return_value=errno.EINPROGRESS
        selector=MagicMock();selector.select.return_value=[]
        with patch('metrics_core.socket.socket')as ctor,patch('metrics_core.selectors.DefaultSelector')as select_ctor,patch('metrics_core.time.monotonic',side_effect=[0,0,.5,1,2.1]):
            ctor.return_value.__enter__.return_value=stream;select_ctor.return_value.__enter__.return_value=selector
            with self.assertRaises(Refused)as stopped:get_metrics(socket.AF_INET,'127.0.0.1',1234)
            self.assertEqual(stopped.exception.args,('metrics-read-deadline',));stream.send.assert_not_called();stream.recv.assert_not_called()
    def test_hostnames_and_nonloopback_never_create_socket(self):
        with patch('metrics_core.socket.socket')as ctor:
            for address in ['example.invalid','192.0.2.1']:
                with self.assertRaises(Refused):get_metrics(socket.AF_INET,address,1234)
            ctor.assert_not_called()
    def test_response_body_line_caps_source_cases(self):
        with self.assertRaises(Refused):parse(b'x'*(16384+1),'projects')
        with self.assertRaises(Refused):parse(b'x'*(2*1024*1024+1),'projects')

def latency(sum_value=10,count=2,status='200',extra=''):
    base='dapr_runtime_service_invocation_res_recv_latency_ms'
    key=f'app_id="projects",src_app_id="eventstore",status="{status}"{extra}'
    return f'# TYPE {base} histogram\n{base}_sum{{{key}}} {sum_value}\n{base}_count{{{key}}} {count}\n'.encode()

class CompanionGuards(unittest.TestCase):
    def test_same_process_positive_pair_mean(self):
        result=delta_latency(parse_latency(latency(),'projects'),parse_latency(latency(20,4),'projects'),True)
        self.assertEqual(result['rows'][0]['deltaMeanMilliseconds'],5)
    def test_missing_and_zero_delta_do_not_make_mean(self):
        for left,right in [(b'',latency()),(latency(),latency())]:
            result=delta_latency(parse_latency(left,'projects'),parse_latency(right,'projects'),True)
            self.assertIsNone(result['rows'][0]['deltaMeanMilliseconds'])
    def test_reset_and_changed_start_do_not_make_mean(self):
        for right,same in [(latency(5,1),True),(latency(20,4),False)]:
            result=delta_latency(parse_latency(latency(),'projects'),parse_latency(right,'projects'),same)
            self.assertIsNone(result['rows'][0]['deltaMeanMilliseconds'])
    def test_extra_labels_status_and_unknown_peer_discarded(self):
        self.assertEqual(parse_latency(latency(extra=',namespace="synthetic-unknown"'),'projects')['series'],{})
        self.assertEqual(parse_latency(latency(status='unknown-status'),'projects')['series'],{})
        self.assertEqual(parse_latency(latency(status='50'),'projects')['series'],{})
    def test_duplicate_sum_never_summed(self):
        with self.assertRaises(Refused):parse_latency(latency()+latency(),'projects')
    def test_buckets_and_response_total_ignored(self):
        body=latency()+b'dapr_runtime_service_invocation_res_recv_total{app_id="projects",src_app_id="eventstore",status="200",type="unary"} 999\n'
        self.assertEqual(parse_latency(body,'projects')['samplesSeen'],2)

class OwnershipAndWindowGuards(unittest.TestCase):
    def test_listener_running_sleeping_state_is_not_identity_change(self):
        first={'state':'R','start':'123','parent':1,'group':42,'session':42};last={**first,'state':'S'}
        fd_dir=MagicMock();fd_dir.iterdir.return_value=['synthetic-fd']
        tcp=MagicMock();tcp.read_text.return_value='header\n0: 0100007F:04D2 00000000:0000 0A 0 0 0 0 0 99\n'
        tcp6=MagicMock();tcp6.read_text.return_value='header\n'
        def fake_path(*parts):return fd_dir if parts[-1]=='fd'else tcp if parts[-1]=='tcp'else tcp6
        with patch('metrics_core.process',side_effect=[first,last]),patch('metrics_core.Path',side_effect=fake_path),patch('metrics_core.os.readlink',return_value='socket:[99]'):
            self.assertEqual(listener(42,'123',1234),(socket.AF_INET,'127.0.0.1','99'))
        self.assertEqual(stable_process(first),stable_process(last))
    def test_zombie_listener_refused(self):
        with patch('metrics_core.process',return_value={'state':'Z','start':'123'}):
            with self.assertRaises(Refused):listener(42,'123',1234)
    def test_family_address_version_refused_before_socket(self):
        with patch('metrics_core.socket.socket')as ctor:
            for family,address in [(socket.AF_INET,'::1'),(socket.AF_INET6,'127.0.0.1')]:
                with self.assertRaises(Refused):get_metrics(family,address,1234)
            ctor.assert_not_called()
    def test_ppid_chain_proves_wrapper_and_apphost(self):
        rows={100:{'state':'S','parent':1},110:{'state':'S','parent':100},140:{'state':'S','parent':110}}
        self.assertTrue(chain(140,110,rows));self.assertTrue(chain(110,100,rows));self.assertFalse(chain(110,999,rows))
    def test_maximum_eight_second_http_deadline_and_eighteen_calls(self):
        ticks=itertools.count(0,.1);identity={'family':socket.AF_INET,'address':'127.0.0.1','port':1234}
        with patch('collect_window.time.monotonic',side_effect=lambda:next(ticks)),patch('collect_window.time.sleep'),patch('collect_window.get_metrics',return_value=exposition())as getter:
            result=collect_window({r:identity for r in ['eventstore','tenants','projects','projects-workers','folders','folders-workers']},lambda role:True,lambda:True,lambda:True)
            self.assertLessEqual(getter.call_count,18);self.assertTrue(all(call.args[3]<=8 for call in getter.call_args_list));self.assertTrue(result['wholeWindowPostflightAccepted'])
    def test_postflight_failure_discards_entire_window(self):
        ticks=itertools.count(0,.1);post=MagicMock(return_value=False)
        with patch('collect_window.time.monotonic',side_effect=lambda:next(ticks)),patch('collect_window.time.sleep'):
            result=collect_window({},lambda role:True,lambda:True,post)
        post.assert_called_once();self.assertFalse(result['wholeWindowPostflightAccepted']);self.assertEqual(result['snapshots'],[])
    def test_unreviewed_guard_text_never_escapes(self):
        ticks=itertools.count(0,.1)
        def refresh(role):raise Refused('synthetic-unreviewed-private-text')
        with patch('collect_window.time.monotonic',side_effect=lambda:next(ticks)),patch('collect_window.time.sleep'):
            result=collect_window({'projects':{}},refresh,lambda:True,lambda:True)
        self.assertNotIn('synthetic-unreviewed-private-text',str(result))

# Root may separately authorize a pure guarded run later; no automatic entry point.
