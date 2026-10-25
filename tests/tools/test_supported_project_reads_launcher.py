"""Ownership and exception controls for the supported-read launcher's real cleanup logic."""
import contextlib
import hashlib
import importlib.util
import io
import os
import socket
import xml.etree.ElementTree as ET
import json
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import textwrap
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("supported_read_launcher", ROOT / "tools/qualification/run_supported_project_reads.py")
LAUNCHER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(LAUNCHER)


def container_id(name):
    return hashlib.sha256(name.encode()).hexdigest()


def resources(*names):
    return [{"name": name, "id": container_id(name)} for name in names]


class SupportedReadLauncherCleanupTests(unittest.TestCase):
    def setUp(self):
        controls = patch.object(LAUNCHER, "run_launcher_controls", return_value={"total": 1, "passed": 1})
        controls.start()
        self.addCleanup(controls.stop)
        stop = patch.object(LAUNCHER, "stop_owned_apphost", side_effect=lambda _owned, run:
                            run(["aspire", "stop", "--apphost", str(LAUNCHER.APPHOST), "--non-interactive"], timeout=60))
        stop.start()
        self.addCleanup(stop.stop)

    def test_success_assertion_failure_and_partial_start_remove_only_owned_resources(self):
        for outcome in ("success", "assertion-failure", "partial-start"):
            with self.subTest(outcome=outcome):
                containers = {"external": "foreign", "owned-a": "scope", "owned-b": "scope", "owned-c": "scope"}
                if outcome == "partial-start":
                    containers = {"external": "foreign", "owned-a": "scope"}
                calls = []
                report = {"result": "passed" if outcome == "success" else "failed", "cleanup": []}

                def execute(command, **_kwargs):
                    calls.append(command)
                    if command[:2] == ["aspire", "describe"]:
                        return '{"resources": []}'
                    if command[:3] == ["docker", "inspect", "--format"]:
                        return json.dumps({"id": command[-1], "owner": containers[next(name for name in containers if container_id(name) == command[-1])]})
                    if command[:2] == ["docker", "rm"]:
                        selected = next(name for name in containers if container_id(name) == command[-1])
                        del containers[selected]
                        return ""
                    if command[:2] == ["docker", "ps"]:
                        return "\n".join(json.dumps({"id": container_id(name), "names": name}) for name in containers)
                    self.fail("Unexpected cleanup command")

                with patch.object(LAUNCHER, "sidecar_processes", return_value=[]), patch.object(LAUNCHER, "execute", side_effect=execute):
                    LAUNCHER.cleanup_resources(True, resources("owned-a", "owned-b", "owned-c"), "scope", lambda command, **_kwargs: calls.append(command), report)
                self.assertEqual(containers, {"external": "foreign"})
                self.assertEqual([item["resource"] for item in report["cleanup"]], ["exact-apphost", "owned-c", "owned-b", "owned-a"])
                self.assertTrue(all(item["removed"] and item["absenceVerified"] for item in report["cleanup"]))
                self.assertEqual(report["result"], "passed" if outcome == "success" else "failed")
                self.assertEqual(calls[0], ["aspire", "stop", "--apphost", str(LAUNCHER.APPHOST), "--non-interactive"])

    def test_host_stop_failure_still_attempts_every_container(self):
        report = {"result": "passed", "cleanup": []}
        removed = []
        containers = {"owned-a", "owned-b"}

        def execute(command, **_kwargs):
            if command[:3] == ["docker", "inspect", "--format"]:
                return json.dumps({"id": command[-1], "owner": "scope"})
            if command[:2] == ["docker", "rm"]:
                selected = next(name for name in containers if container_id(name) == command[-1])
                containers.remove(selected)
                removed.append(selected)
                return ""
            if command[:2] == ["docker", "ps"]:
                return "\n".join(json.dumps({"id": container_id(name), "names": name}) for name in containers)
            self.fail("Unexpected cleanup command")

        with patch.object(LAUNCHER, "sidecar_processes", return_value=[]), patch.object(LAUNCHER, "execute", side_effect=execute):
            LAUNCHER.cleanup_resources(True, resources("owned-a", "owned-b"), "scope", lambda *_args, **_kwargs: (_ for _ in ()).throw(RuntimeError("actual stop failure")), report)
        self.assertEqual(removed, ["owned-b", "owned-a"])
        self.assertEqual(report["result"], "failed")
        self.assertFalse(report["cleanup"][0]["removed"])

    def test_remaining_owned_sidecar_refuses_qualification(self):
        report = {"result": "passed", "cleanup": []}
        owned = {"123": {"start": "original", "arguments": ["/test/daprd"], "appId": "folders"}}
        with patch.object(LAUNCHER, "owned_sidecar_census", return_value=owned), patch.object(Path, "read_bytes", return_value=b"/test/daprd\0"), patch.object(LAUNCHER, "process_start", return_value="original"):
            LAUNCHER.cleanup_resources(True, [], "scope", lambda *_args, **_kwargs: None, report)
        self.assertEqual(report["result"], "failed")
        self.assertEqual(report["ownedSidecarsObservedBeforeStop"], 1)
        self.assertFalse(report["ownedSidecarsAbsent"])
        self.assertEqual(report["remainingOwnedSidecars"], ["folders"])

    def test_sidecar_observation_failure_still_stops_the_exact_host(self):
        report = {"result": "passed", "cleanup": []}
        stopped = []
        with patch.object(LAUNCHER, "sidecar_processes", side_effect=OSError("process observation unavailable")), patch.object(LAUNCHER, "execute", return_value='{"resources": []}'):
            LAUNCHER.cleanup_resources(True, [], "scope", lambda command, **_kwargs: stopped.append(command), report)
        self.assertEqual(stopped[0][:2], ["aspire", "stop"])
        self.assertEqual(report["result"], "failed")
        self.assertFalse(report["ownedSidecarsAbsent"])
        self.assertTrue(report["cleanup"][0]["absenceVerified"])

    def test_retained_log_facts_exclude_raw_payload_and_dynamic_templates(self):
        protected = "synthetic-protected-description"
        raw = json.dumps({"Message": protected, "Exception": protected,
                          "State": {"{OriginalFormat}": protected, "TenantId": protected,
                                    "Reason": "assertion-missing", "ExceptionType": "HttpRequestException"}})
        facts = LAUNCHER.safe_log_facts(raw)
        self.assertNotIn(protected, json.dumps(facts))
        self.assertEqual(facts["closedFacts"], sorted(["ExceptionTypeSha256=" + hashlib.sha256(b"HttpRequestException").hexdigest(), "ReasonSha256=" + hashlib.sha256(b"assertion-missing").hexdigest()]))
        self.assertEqual(len(facts["templateSha256"][0]), 64)

    def test_foreign_ownership_and_removal_failure_never_prevent_later_cleanup(self):
        report = {"result": "passed", "cleanup": []}
        removed = []
        containers = {"owned", "broken", "external"}

        def execute(command, **_kwargs):
            if command[:3] == ["docker", "inspect", "--format"]:
                return json.dumps({"id": command[-1], "owner": "foreign" if command[-1] == container_id("external") else "scope"})
            if command[:2] == ["docker", "rm"]:
                selected = next(name for name in containers if container_id(name) == command[-1])
                if selected == "broken":
                    raise RuntimeError("removal failure")
                containers.remove(selected)
                removed.append(selected)
                return ""
            if command[:2] == ["docker", "ps"]:
                return "\n".join(json.dumps({"id": container_id(name), "names": name}) for name in containers)
            self.fail("Unexpected cleanup command")

        with patch.object(LAUNCHER, "execute", side_effect=execute):
            LAUNCHER.cleanup_resources(False, resources("owned", "broken", "external"), "scope", lambda *_args, **_kwargs: self.fail("Unowned AppHost stop"), report)
        self.assertEqual(removed, ["owned"])
        self.assertEqual([item["removed"] for item in report["cleanup"]], [False, False, True])
        self.assertEqual(report["result"], "failed")

    def test_container_observation_failure_does_not_prove_absence(self):
        report = {"result": "passed", "cleanup": []}
        removed = []
        observations = 0
        def execute(command, **_kwargs):
            nonlocal observations
            if command[:2] == ["docker", "inspect"]:
                return json.dumps({"id": container_id("owned"), "owner": "scope"})
            if command[:2] == ["docker", "rm"]:
                removed.append(command[-1])
                return ""
            observations += 1
            if observations == 1:
                return json.dumps({"id": container_id("owned"), "names": "owned"})
            raise RuntimeError("Docker daemon observation unavailable")
        with patch.object(LAUNCHER, "execute", side_effect=execute):
            LAUNCHER.cleanup_resources(None, resources("owned"), "scope", lambda *_args, **_kwargs: None, report)
        self.assertEqual(removed, [container_id("owned")])
        self.assertEqual(report["result"], "failed")
        self.assertFalse(report["cleanup"][0]["removed"])
        self.assertNotIn("absenceVerified", report["cleanup"][0])

    def test_container_removal_targets_the_owned_id_and_preserves_a_name_replacement(self):
        report = {"result": "passed", "cleanup": []}
        removed = []
        observations = 0
        def execute(command, **_kwargs):
            nonlocal observations
            if command[:2] == ["docker", "inspect"]:
                return json.dumps({"id": container_id("owned-original"), "owner": "scope"})
            if command[:2] == ["docker", "rm"]:
                removed.append(command[-1])
                return ""
            if command[:2] == ["docker", "ps"]:
                observations += 1
                return json.dumps({"id": container_id("owned-original") if observations == 1 else container_id("foreign-replacement"), "names": "reused-name"})
            self.fail("Unexpected ownership command")
        with patch.object(LAUNCHER, "execute", side_effect=execute):
            LAUNCHER.cleanup_resources(None, [{"name": "reused-name", "id": container_id("owned-original")}], "scope", lambda *_args, **_kwargs: None, report)
        self.assertEqual(removed, [container_id("owned-original")])
        self.assertTrue(report["cleanup"][0]["absenceVerified"])
        self.assertEqual(report["result"], "passed")

    def test_already_running_apphost_is_refused_before_any_owned_mutation(self):
        with tempfile.TemporaryDirectory() as directory:
            evidence = Path(directory) / "evidence"
            with patch.object(LAUNCHER.shutil, "which", return_value="/test/tool"), patch.object(LAUNCHER, "execute", return_value='{"resources": [{"displayName": "pre-existing"}]}') as execute:
                with patch.object(LAUNCHER.sys, "argv", ["launcher", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
                    result = LAUNCHER.main()
            self.assertEqual(result, 1)
            self.assertEqual(execute.call_count, 1)
            self.assertEqual(execute.call_args.args[0][:2], ["aspire", "describe"])
            report = json.loads((evidence / "run.json").read_text())
            self.assertEqual(report["cleanup"], [])
            self.assertTrue(report["privateMaterialRemoved"])


class SupportedReadLauncherOwnershipTests(unittest.TestCase):
    def setUp(self):
        controls = patch.object(LAUNCHER, "run_launcher_controls", return_value={"total": 1, "passed": 1})
        controls.start()
        self.addCleanup(controls.stop)

    def test_each_folder_mutation_refuses_a_replaced_apphost_session(self):
        for action in ("stop", "start"):
            with self.subTest(action=action), patch.object(LAUNCHER, "owns_current_apphost", return_value=False), self.assertRaises(RuntimeError):
                LAUNCHER.mutate_owned_folder({}, action, lambda *_args, **_kwargs: self.fail("Replacement resource mutation"))

    def test_owned_folder_restart_uses_the_guarded_target_and_waits_for_readiness(self):
        commands = []
        with patch.object(LAUNCHER, "owns_current_apphost", return_value=True) as guard:
            LAUNCHER.mutate_owned_folder({"owned": "identity"}, "start", lambda command, **_kwargs: commands.append(command))
        guard.assert_called_once()
        self.assertEqual(commands[0][:4], ["aspire", "resource", "folders", "start"])
        self.assertEqual(commands[1][:3], ["aspire", "wait", "folders"])
    def test_sigterm_enters_cleanup_removes_private_material_and_restores_handler(self):
        harness = textwrap.dedent('''
            import importlib.util, json, signal, sys
            from pathlib import Path
            spec = importlib.util.spec_from_file_location("launcher", sys.argv[1])
            launcher = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(launcher)
            evidence, ready = map(Path, sys.argv[2:])
            prior = signal.getsignal(signal.SIGTERM)
            original_allocate = launcher.tempfile.mkdtemp
            allocated = None
            def allocate(*args, **kwargs):
                global allocated
                kwargs["dir"] = str(evidence.parent)
                allocated = Path(original_allocate(*args, **kwargs))
                (allocated / "owned-test-private-key").write_text("owned synthetic test signer")
                return str(allocated)
            launcher.tempfile.mkdtemp = allocate
            launcher.shutil.which = lambda _name: "/test/tool"
            launcher.run_launcher_controls = lambda _evidence: {"total": 1, "passed": 1}
            def execute(command, **_kwargs):
                assert command[:2] == ["aspire", "describe"]
                ready.write_text(json.dumps({"private": str(allocated)}))
                signal.pause()
                raise AssertionError("Signal did not enter the failure path")
            launcher.execute = execute
            original_cleanup = launcher.cleanup_resources
            def cleanup(*args, **kwargs):
                ready.with_suffix(".cleanup").write_text("entered")
                original_cleanup(*args, **kwargs)
            launcher.cleanup_resources = cleanup
            sys.argv = ["launcher", "--evidence", str(evidence)]
            result = launcher.main()
            ready.with_suffix(".restored").write_text(str(signal.getsignal(signal.SIGTERM) == prior))
            sys.exit(result)
        ''')
        with tempfile.TemporaryDirectory() as directory:
            evidence, ready = Path(directory) / "evidence", Path(directory) / "ready.json"
            child = subprocess.Popen([sys.executable, "-c", harness, str(ROOT / "tools/qualification/run_supported_project_reads.py"), str(evidence), str(ready)],
                                     stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            try:
                deadline = time.monotonic() + 10
                while not ready.exists() and child.poll() is None and time.monotonic() < deadline:
                    time.sleep(.01)
                self.assertTrue(ready.exists(), "Signal control did not reach its protected allocation")
                private = Path(json.loads(ready.read_text())["private"])
                self.assertTrue((private / "owned-test-private-key").is_file())
                child.send_signal(signal.SIGTERM)
                child.communicate(timeout=10)
                self.assertEqual(child.returncode, 1)
                self.assertTrue(ready.with_suffix(".cleanup").is_file())
                self.assertEqual(ready.with_suffix(".restored").read_text(), "True")
                self.assertFalse(private.exists())
                report = json.loads((evidence / "run.json").read_text())
                self.assertEqual(report["interruptedSignal"], "SIGTERM")
                self.assertTrue(report["privateMaterialRemoved"])
            finally:
                if child.poll() is None:
                    child.kill()
                    child.communicate(timeout=5)

    def test_description_requires_recognized_json(self):
        for value in ("banner\n{\"resources\": []}", "", "[]", "{}", '{"resources": {}}'):
            with self.subTest(value=value), self.assertRaises(RuntimeError):
                LAUNCHER.apphost_running(value)
        self.assertFalse(LAUNCHER.apphost_running('{"resources": []}'))
        self.assertTrue(LAUNCHER.apphost_running('{"resources": [{}]}'))

    def test_malformed_preflight_cannot_authorize_start(self):
        with tempfile.TemporaryDirectory() as directory:
            evidence = Path(directory) / "evidence"
            with patch.object(LAUNCHER.shutil, "which", return_value="/test/tool"), patch.object(LAUNCHER, "execute", return_value='banner\n{"resources": []}') as execute:
                with patch.object(LAUNCHER.sys, "argv", ["launcher", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
                    self.assertEqual(LAUNCHER.main(), 1)
            self.assertEqual(execute.call_count, 1)
            self.assertEqual(json.loads((evidence / "run.json").read_text())["cleanup"], [])

    def test_blank_description_cannot_hide_an_existing_structured_session(self):
        with tempfile.TemporaryDirectory() as directory:
            evidence = Path(directory) / "evidence"
            replies = ["", json.dumps([{"appHostPath": str(LAUNCHER.APPHOST), "appHostPid": 101, "cliPid": 202}])]
            with patch.object(LAUNCHER.shutil, "which", return_value="/test/tool"), patch.object(LAUNCHER, "execute", side_effect=replies) as execute:
                with patch.object(LAUNCHER.sys, "argv", ["launcher", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
                    self.assertEqual(LAUNCHER.main(), 1)
            self.assertEqual(execute.call_count, 2)
            self.assertEqual(json.loads((evidence / "run.json").read_text())["cleanup"], [])

    def test_mixed_case_configuration_is_removed_and_unrelated_environment_preserved(self):
        source = {"PATH": "/tools", "UNRELATED_VALUE": "keep", "aUtHeNtIcAtIoN__Issuer": "foreign",
                  "FOLDERS__Authority": "foreign", "pRoJeCtS__SupportedReads__Enabled": "foreign",
                  "TENANTS__Bootstrap": "foreign", "eventSTORE__OwnerQueries__OperationTimeout": "foreign",
                  "dApR__RedisHost": "foreign", "dapr_HTTP_ENDPOINT": "foreign", "PARAMETERS__secret": "foreign",
                  "Authentication:Workload:Issuer": "foreign"}
        self.assertEqual(LAUNCHER.sanitized_environment(source), {"PATH": "/tools", "UNRELATED_VALUE": "keep"})

    def test_start_race_or_existing_session_is_never_adopted(self):
        started = json.dumps({"appHostPath": str(LAUNCHER.APPHOST), "appHostPid": 101, "cliPid": 202})
        for reply in (started, 'banner\n' + started, json.dumps({"alreadyRunning": True})):
            with self.subTest(reply=reply), patch.object(LAUNCHER, "find_owned_apphost", return_value=None), self.assertRaises(RuntimeError):
                LAUNCHER.start_owned_apphost(lambda *_args, **_kwargs: reply, Path("/private/fixture.json"), "scope")

    def test_partial_session_matches_only_its_private_configuration_and_namespace(self):
        session = {"appHostPid": 101, "cliPid": 202}
        configuration = Path("/private/fixture.json")
        for configured, namespace, expected in ((str(configuration), "scope", True), ("/foreign/fixture.json", "scope", False), (str(configuration), "foreign", False)):
            environment = ("Projects__SupportedReads__ConfigurationFile=" + configured + "\0NAMESPACE=" + namespace + "\0").encode()
            def contents(path):
                return b"{}" if path == configuration else environment
            with self.subTest(configured=configured, namespace=namespace), patch.object(Path, "read_bytes", autospec=True, side_effect=contents), patch.object(LAUNCHER, "process_start", return_value="ticks"):
                observed = LAUNCHER.session_identity(session, configuration, "scope")
            self.assertEqual(observed is not None, expected)
            if observed:
                self.assertEqual(observed["appHostPid"], 101)
                self.assertEqual(observed["cliPid"], 202)
                self.assertEqual(observed["appHostStartTicks"], "ticks")

    def test_partial_owned_start_can_stop_but_changed_session_cannot(self):
        owned = {"appHostPid": 101, "appHostStartTicks": "one", "cliPid": 202, "cliStartTicks": "two"}
        stopped = []
        with patch.object(LAUNCHER, "owned_apphost_gone", side_effect=[False, True]), patch.object(LAUNCHER, "owns_current_apphost", return_value=True):
            LAUNCHER.stop_owned_apphost(owned, lambda command, **_kwargs: stopped.append(command))
        self.assertEqual(stopped[0][:2], ["aspire", "stop"])
        with patch.object(LAUNCHER, "owned_apphost_gone", return_value=False), patch.object(LAUNCHER, "owns_current_apphost", return_value=False), self.assertRaises(RuntimeError):
            LAUNCHER.stop_owned_apphost(owned, lambda *_args, **_kwargs: self.fail("Mismatched session stop"))

    def test_replacement_and_reused_process_identity_are_preserved(self):
        owned = {"appHostPid": 101, "appHostStartTicks": "one", "cliPid": 202, "cliStartTicks": "two"}
        with patch.object(LAUNCHER, "process_start", return_value="replacement"):
            self.assertTrue(LAUNCHER.owned_apphost_gone(owned))
            LAUNCHER.stop_owned_apphost(owned, lambda *_args, **_kwargs: self.fail("Replacement session stop"))


class SupportedReadLauncherReviewControls(unittest.TestCase):
    def test_renamed_owned_container_is_removed_by_retained_creation_id(self):
        identity = container_id("creation")
        calls, remaining = [], {identity: {"renamed-owned"}, container_id("foreign"): {"original-name"}}
        def execute(command, **_kwargs):
            calls.append(command)
            if command[:2] == ["docker", "ps"]:
                return "\n".join(json.dumps({"id": key, "names": next(iter(names))}) for key, names in remaining.items())
            if command[:2] == ["docker", "inspect"]:
                self.assertEqual(command[-1], identity)
                return json.dumps({"id": identity, "owner": "scope"})
            if command[:2] == ["docker", "rm"]:
                del remaining[command[-1]]
                return ""
            self.fail("Unexpected mutation")
        report = {"result": "passed", "cleanup": []}
        with patch.object(LAUNCHER, "execute", side_effect=execute):
            LAUNCHER.cleanup_resources(None, [{"name": "original-name", "id": identity}], "scope", None, report)
        self.assertEqual(set(remaining), {container_id("foreign")})
        self.assertTrue(report["cleanup"][0]["absenceVerified"])
        self.assertNotIn("alreadyAbsent", report["cleanup"][0])

    def test_partial_creation_recovers_immutable_cidfile(self):
        identity = container_id("creation")
        with tempfile.TemporaryDirectory() as directory:
            cidfile = Path(directory) / "creation.cid"
            cidfile.write_text(identity)
            report = {"result": "passed", "cleanup": []}
            with patch.object(LAUNCHER, "container_listing", return_value={}):
                LAUNCHER.cleanup_resources(None, [{"name": "renamed", "id": None, "cidfile": str(cidfile)}], "scope", None, report)
            self.assertEqual(report["cleanup"][0]["containerId"], identity)
            self.assertTrue(report["cleanup"][0]["absenceVerified"])

    def test_cmdline_observation_failure_is_not_disappearance(self):
        with patch.object(LAUNCHER.subprocess, "check_output", return_value="123 daprd"), patch.object(Path, "read_bytes", side_effect=PermissionError("unobservable")), self.assertRaises(PermissionError):
            list(LAUNCHER.sidecar_processes())
        with patch.object(LAUNCHER.subprocess, "check_output", return_value="123 daprd"), patch.object(Path, "read_bytes", side_effect=FileNotFoundError()):
            self.assertEqual(list(LAUNCHER.sidecar_processes()), [])

    def test_inner_environ_and_post_cmdline_failures_still_attempt_cleanup(self):
        original = {"123": {"start": "ticks", "appId": "folders", "arguments": []}}
        for path in ("environ", "cmdline"):
            with self.subTest(path=path):
                report, stopped = {"result": "passed", "cleanup": []}, []
                with contextlib.ExitStack() as stack:
                    stack.enter_context(patch.object(LAUNCHER, "stop_owned_apphost", side_effect=lambda *_: stopped.append(True)))
                    stack.enter_context(patch.object(Path, "read_bytes", side_effect=PermissionError("unobservable")))
                    if path == "environ":
                        stack.enter_context(patch.object(LAUNCHER, "sidecar_processes", return_value=[["123", "/test/daprd", "--app-id", "folders"]]))
                    else:
                        stack.enter_context(patch.object(LAUNCHER, "owned_sidecar_census", side_effect=[original, {}]))
                    LAUNCHER.cleanup_resources({"owned": True}, [], "scope", None, report)
                self.assertEqual(stopped, [True])
                self.assertEqual(report["result"], "failed")
                self.assertFalse(report["ownedSidecarsAbsent"])

    def test_fresh_post_stop_census_detects_new_owned_sidecar(self):
        report = {"result": "passed", "cleanup": []}
        replacement = {"456": {"start": "replacement", "appId": "folders", "arguments": []}}
        with patch.object(LAUNCHER, "owned_sidecar_census", side_effect=[{}, replacement]), patch.object(LAUNCHER, "stop_owned_apphost"):
            LAUNCHER.cleanup_resources({"owned": True}, [], "scope", None, report)
        self.assertFalse(report["ownedSidecarsAbsent"])
        self.assertEqual(report["remainingOwnedSidecars"], ["folders"])

    def test_reused_pid_with_unrelated_start_identity_is_preserved(self):
        report = {"result": "passed", "cleanup": []}
        original = {"123": {"start": "original", "appId": "folders", "arguments": []}}
        with patch.object(LAUNCHER, "owned_sidecar_census", side_effect=[original, {}]), patch.object(LAUNCHER, "stop_owned_apphost"), patch.object(Path, "read_bytes", return_value=b"unrelated"), patch.object(LAUNCHER, "process_start", return_value="replacement"):
            LAUNCHER.cleanup_resources({"owned": True}, [], "scope", None, report)
        self.assertTrue(report["ownedSidecarsAbsent"])
        self.assertEqual(report["result"], "passed")

    def test_discovery_requires_local_address_and_the_owned_process_listener(self):
        with socket.socket() as listener:
            listener.bind(("127.0.0.1", 0))
            listener.listen()
            port = listener.getsockname()[1]
            self.assertTrue(LAUNCHER.sidecar_listens_at(os.getpid(), port, "127.0.0.1:" + str(port)))
            self.assertFalse(LAUNCHER.sidecar_listens_at(os.getpid(), port, "203.0.113.17:" + str(port)))
            self.assertFalse(LAUNCHER.sidecar_listens_at(os.getpid(), port + 1, "127.0.0.1:" + str(port + 1)))

    def test_empty_or_unsuccessful_regression_summaries_cannot_qualify(self):
        for attributes in (None, {"total": "0", "passed": "0", "failed": "0", "skipped": "0"}, {"total": "1", "passed": "0", "failed": "0", "skipped": "0"}):
            assemblies = [] if attributes is None else [ET.Element("assembly", attributes)]
            with self.subTest(attributes=attributes), self.assertRaises(RuntimeError):
                LAUNCHER.require_test_results(assemblies)
        self.assertEqual(len(LAUNCHER.require_test_results([ET.Element("assembly", {"total": "2", "passed": "2", "failed": "0", "skipped": "0"})])), 1)

    def test_all_dynamic_log_fact_fields_hash_protected_values(self):
        protected = "ProtectedAlphabeticDescription"
        fields = {key: protected for key in ("ComponentName", "Reason", "ProcessingResult", "ExceptionType")}
        facts = LAUNCHER.safe_log_facts(json.dumps(fields))
        self.assertNotIn(protected, json.dumps(facts))
        self.assertEqual(len(facts["closedFacts"]), 4)
        self.assertTrue(all(hashlib.sha256(protected.encode()).hexdigest() in value for value in facts["closedFacts"]))

    def test_launcher_controls_require_nonempty_success_and_retain_output(self):
        with tempfile.TemporaryDirectory() as directory:
            evidence = Path(directory)
            for output, code in (("Ran 0 tests in 0s\nOK\n", 0), ("Ran 1 test in 0s\nFAILED\n", 1), ("Ran 1 test in 0s\nOK (skipped=1)\n", 0)):
                with self.subTest(output=output), patch.object(LAUNCHER.subprocess, "run", return_value=subprocess.CompletedProcess([], code, output)), self.assertRaises(RuntimeError):
                    LAUNCHER.run_launcher_controls(evidence)
            with patch.object(LAUNCHER.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "Ran 2 tests in 0.1s\nOK\n")):
                self.assertEqual(LAUNCHER.run_launcher_controls(evidence)["passed"], 2)
            self.assertIn("Ran 2 tests", (evidence / "launcher-controls.log").read_text())

    def test_failed_launcher_controls_block_before_external_allocation(self):
        with tempfile.TemporaryDirectory() as directory, patch.object(LAUNCHER.shutil, "which", return_value="/tool"), patch.object(LAUNCHER, "run_launcher_controls", side_effect=RuntimeError("controls failed")), patch.object(LAUNCHER, "execute") as execute, patch.object(LAUNCHER.tempfile, "mkdtemp") as allocate:
            evidence = Path(directory) / "evidence"
            with patch.object(LAUNCHER.sys, "argv", ["launcher", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(LAUNCHER.main(), 1)
            execute.assert_not_called()
            allocate.assert_not_called()


if __name__ == "__main__":
    unittest.main()
