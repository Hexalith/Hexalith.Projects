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
            started = time.monotonic()
            LAUNCHER.cleanup_resources(True, [], "scope", lambda *_args, **_kwargs: None, report, sidecar_wait_seconds=.03)
        self.assertLess(time.monotonic() - started, 1)
        self.assertEqual(report["result"], "failed")
        self.assertEqual(report["ownedSidecarsObservedBeforeStop"], 1)
        self.assertFalse(report["ownedSidecarsAbsent"])
        self.assertEqual(report["remainingOwnedSidecars"], ["folders"])

    def test_cleanup_waits_for_late_owned_sidecar_exit_by_namespace_and_original_identity(self):
        original = {"123": {"start": "original", "arguments": ["/test/daprd"], "appId": "folders"}}
        for first_post_stop in (original, {}):
            report, stopped = {"result": "passed", "cleanup": []}, []
            with self.subTest(namespace_still_visible=bool(first_post_stop)), contextlib.ExitStack() as stack:
                census = stack.enter_context(patch.object(LAUNCHER, "owned_sidecar_census", side_effect=[original, first_post_stop, {}]))
                stack.enter_context(patch.object(Path, "read_bytes", side_effect=[b"", FileNotFoundError()]))
                identity = stack.enter_context(patch.object(LAUNCHER, "process_start", return_value="original"))
                pause = stack.enter_context(patch.object(LAUNCHER.time, "sleep"))
                LAUNCHER.cleanup_resources(True, [], "scope", lambda command, **_kwargs: stopped.append(command), report)
            self.assertEqual(len(stopped), 1)
            self.assertEqual(stopped[0][:4], ["aspire", "stop", "--apphost", str(LAUNCHER.APPHOST)])
            self.assertEqual(census.call_args_list, [unittest.mock.call("scope")] * 3)
            identity.assert_called_once_with(123)
            pause.assert_called_once_with(.1)
            self.assertTrue(report["ownedSidecarsAbsent"])
            self.assertEqual(report["result"], "passed")
            self.assertNotIn("remainingOwnedSidecars", report)

    def test_post_stop_unreadable_census_cmdline_and_start_fail_at_deadline(self):
        original = {"123": {"start": "original", "arguments": ["/test/daprd"], "appId": "folders"}}
        for observation in ("census", "cmdline", "start"):
            report, stopped = {"result": "passed", "cleanup": []}, []
            calls = 0
            def census(_scope):
                nonlocal calls
                calls += 1
                if calls == 1:
                    return original
                if observation == "census":
                    raise PermissionError("unobservable")
                return {}
            with self.subTest(observation=observation), contextlib.ExitStack() as stack:
                stack.enter_context(patch.object(LAUNCHER, "owned_sidecar_census", side_effect=census))
                stack.enter_context(patch.object(Path, "read_bytes", side_effect=PermissionError("unobservable") if observation == "cmdline" else None, return_value=b"/test/daprd"))
                stack.enter_context(patch.object(LAUNCHER, "process_start", side_effect=PermissionError("unobservable") if observation == "start" else None, return_value="original"))
                started = time.monotonic()
                LAUNCHER.cleanup_resources(True, [], "scope", lambda command, **_kwargs: stopped.append(command), report, sidecar_wait_seconds=.03)
            self.assertLess(time.monotonic() - started, 1)
            self.assertEqual(len(stopped), 1)
            self.assertEqual(report["result"], "failed")
            self.assertFalse(report["ownedSidecarsAbsent"])
            self.assertTrue(report["sidecarObservationFailed"])

    def test_sidecar_absence_wait_bounds_stalled_observation(self):
        started = time.monotonic()
        with patch.object(LAUNCHER, "owned_sidecar_census", side_effect=lambda _scope: time.sleep(30)):
            remaining, observable = LAUNCHER.wait_for_owned_sidecar_absence("scope", {}, seconds=.03)
        self.assertLess(time.monotonic() - started, 1)
        self.assertEqual(remaining, {})
        self.assertFalse(observable)

    def test_sidecar_observation_failure_still_stops_the_exact_host(self):
        report = {"result": "passed", "cleanup": []}
        stopped = []
        with patch.object(LAUNCHER, "sidecar_processes", side_effect=OSError("process observation unavailable")), patch.object(LAUNCHER, "execute", return_value='{"resources": []}'):
            LAUNCHER.cleanup_resources(True, [], "scope", lambda command, **_kwargs: stopped.append(command), report, sidecar_wait_seconds=.03)
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


class SupportedReadProjectsRestartControls(unittest.TestCase):
    def identity(self, pid=301, wrapper=201, started="100"):
        return {"resourceName": "projects-owned", "launcherPid": wrapper, "launcherStartTicks": started,
                "pid": pid, "startTicks": started, "binarySha256": "a" * 64, "assemblySha256": "b" * 64}

    def restart(self, commands, observation, *, identities=None, gone=True, guards=True, fail=None):
        before, after = self.identity(), self.identity(302, 202, "200")
        def run(command, **kwargs):
            commands.append(command)
            self.assertLessEqual(kwargs["timeout"], 135)
            if command[:4] == ["aspire", "resource", "projects", fail] or fail == "wait" and command[:2] == ["aspire", "wait"]:
                raise RuntimeError("controlled failure")
            return ""
        with contextlib.ExitStack() as stack:
            stack.enter_context(patch.object(LAUNCHER, "owns_current_apphost", side_effect=guards if isinstance(guards, list) else None, return_value=guards))
            stack.enter_context(patch.object(LAUNCHER, "resource_graph", return_value={"resources": []}))
            stack.enter_context(patch.object(LAUNCHER, "projects_process_identity", side_effect=identities or [before, before, after, after]))
            stack.enter_context(patch.object(LAUNCHER, "projects_processes_gone", return_value=gone))
            stack.enter_context(patch.object(LAUNCHER, "refresh_owned_endpoints", return_value={"resolver": "sqlite"},
                                             side_effect=RuntimeError("rediscovery failed") if fail == "rediscovery" else None))
            stack.enter_context(patch.object(LAUNCHER.time, "sleep"))
            LAUNCHER.mutate_owned_resource({"appHostPid": 101, "appHostStartTicks": "50", "configurationSha256": "c" * 64}, "projects", "restart", run, observation)

    def test_allowlist_rejects_other_resources_and_actions_without_mutation(self):
        for resource, action in (("eventstore", "restart"), ("projects-workers", "stop"), ("projects", "rebuild"),
                                 ("projects", "stop"), ("projects", "start"), ("folders", "restart"), ("foreign", "start")):
            with self.subTest(resource=resource, action=action), self.assertRaises(RuntimeError):
                LAUNCHER.mutate_owned_resource({}, resource, action, lambda *_args, **_kwargs: self.fail("Disallowed mutation"), {})

    def test_folder_compatibility_keeps_guarded_stop_start(self):
        for action in ("stop", "start"):
            with self.subTest(action=action), patch.object(LAUNCHER, "mutate_owned_folder") as folder:
                LAUNCHER.mutate_owned_resource({"identity": True}, "folders", action, None, {})
                folder.assert_called_once_with({"identity": True}, action, None)

    def test_restart_observes_distinct_server_and_wrapper_then_rediscovers(self):
        commands, observation = [], {}
        self.restart(commands, observation)
        self.assertEqual([command[:4] for command in commands], [["aspire", "resource", "projects", "stop"],
            ["aspire", "resource", "projects", "start"], ["aspire", "wait", "projects", "--apphost"]])
        self.assertTrue(observation["completed"] and observation["oldProcessExited"] and observation["endpointsRediscovered"])
        self.assertNotEqual(observation["before"]["pid"], observation["after"]["pid"])
        self.assertNotIn("_configuration", json.dumps(observation))

    def test_replaced_apphost_before_stop_or_start_cannot_be_mutated(self):
        for guards, expected in (([False], []), ([True, False], []), ([True, True, False], []),
                                 ([True, True, True, False], ["stop"])):
            commands, observation = [], {}
            with self.subTest(guards=guards), self.assertRaises(RuntimeError):
                self.restart(commands, observation, guards=guards)
            self.assertEqual([command[3] for command in commands], expected)
            self.assertFalse(observation["completed"])

    def test_unobservable_or_replaced_query_process_refuses_stop(self):
        before = self.identity()
        for identities in ([PermissionError("unobservable")], [before, PermissionError("unobservable")], [before, self.identity(999)]):
            commands, observation = [], {}
            with self.subTest(identities=identities), self.assertRaises((RuntimeError, PermissionError)):
                self.restart(commands, observation, identities=identities)
            self.assertEqual(commands, [])
            self.assertFalse(observation["completed"])

    def test_stop_failure_or_original_server_remaining_never_attempts_start(self):
        for fail, gone in (("stop", True), (None, False)):
            commands, observation = [], {}
            with self.subTest(fail=fail), self.assertRaises(RuntimeError):
                self.restart(commands, observation, fail=fail, gone=gone)
            self.assertEqual([command[3] for command in commands], ["stop"])
            self.assertFalse(observation["completed"])
            self.assertNotIn("oldProcessExited", observation)

    def test_start_failure_cannot_produce_passing_restart_evidence(self):
        commands, observation = [], {}
        with self.assertRaises(RuntimeError):
            self.restart(commands, observation, fail="start")
        self.assertTrue(observation["oldProcessExited"])
        self.assertFalse(observation["completed"])
        self.assertEqual(observation["stage"], "start")

    def test_readiness_or_rediscovery_failure_cannot_pass(self):
        for failure in ("wait", "rediscovery"):
            observation = {}
            with self.subTest(failure=failure), self.assertRaises(RuntimeError):
                self.restart([], observation, fail=failure)
            self.assertTrue(observation["oldProcessExited"])
            self.assertFalse(observation["completed"])

    def test_unobservable_apphost_after_stop_refuses_start(self):
        commands, observation = [], {}
        with self.assertRaises(PermissionError):
            self.restart(commands, observation, guards=[True, True, True, PermissionError("unobservable")])
        self.assertEqual([command[3] for command in commands], ["stop"])
        self.assertFalse(observation["completed"])

    def test_actual_server_identity_requires_namespace_private_storage_and_development(self):
        project = LAUNCHER.ROOT / "src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj"
        binary = project.parent / "bin/Debug/net10.0/Hexalith.Projects.Server"
        resource = {"displayName": "projects", "resourceType": "Project", "name": "projects-owned",
                    "properties": {"project.path": str(project), "executable.pid": 201}}
        expected = {"NAMESPACE": "scope", "EventStore__DomainService__AppId": "projects", "XDG_DATA_HOME": "/private/data-projects",
                    "ASPNETCORE_ENVIRONMENT": "Development"}
        for changed in (None, *expected):
            environment = {**expected, **({changed: "foreign"} if changed else {})}
            def contents(path):
                if path.name == "environ":
                    return b"\0".join((key + "=" + value).encode() for key, value in environment.items())
                if path.name == "cmdline":
                    return b"dotnet\0run\0" if path.parts[2] == "201" else str(binary).encode()
                return b"synthetic-binary"
            with self.subTest(changed=changed), contextlib.ExitStack() as stack:
                stack.enter_context(patch.object(LAUNCHER, "process_start", return_value="100"))
                stack.enter_context(patch.object(Path, "readlink", autospec=True, side_effect=lambda path: Path("/tools/dotnet") if path.parts[2] == "201" else binary))
                stack.enter_context(patch.object(Path, "read_text", autospec=True, side_effect=lambda path: "301" if path.parts[2] == "201" else ""))
                stack.enter_context(patch.object(Path, "read_bytes", autospec=True, side_effect=contents))
                if changed:
                    with self.assertRaises(RuntimeError):
                        LAUNCHER.projects_process_identity({"_configuration": "/private/fixture.json", "_isolation": "scope"}, {"resources": [resource]})
                else:
                    identity = LAUNCHER.projects_process_identity({"_configuration": "/private/fixture.json", "_isolation": "scope"}, {"resources": [resource]})
                    self.assertEqual((identity["pid"], identity["launcherPid"]), (301, 201))

    def test_unchanged_process_or_wrapper_and_changed_binary_are_refused(self):
        before = self.identity()
        after = self.identity(302, 202, "200")
        for unchanged in (before, {**after, "pid": before["pid"], "startTicks": before["startTicks"]},
                          {**after, "launcherPid": before["launcherPid"], "launcherStartTicks": before["launcherStartTicks"]},
                          {**after, "assemblySha256": "c" * 64}, {**after, "resourceName": "replacement"}):
            observation = {}
            with self.subTest(unchanged=unchanged), self.assertRaises(RuntimeError):
                self.restart([], observation, identities=[before, before, unchanged])
            self.assertFalse(observation["completed"])

    def test_old_process_observation_error_is_not_exit(self):
        with patch.object(LAUNCHER, "process_start", side_effect=PermissionError("unobservable")), self.assertRaises(PermissionError):
            LAUNCHER.projects_processes_gone(self.identity())
        with patch.object(LAUNCHER, "process_start", side_effect=[None, "100"]):
            self.assertFalse(LAUNCHER.projects_processes_gone(self.identity()))

    def test_process_identity_requires_exact_source_and_observable_actual_server(self):
        project = LAUNCHER.ROOT / "src/Hexalith.Projects.Server/Hexalith.Projects.Server.csproj"
        resource = {"displayName": "projects", "resourceType": "Project", "name": "projects-owned",
                    "properties": {"project.path": str(project), "executable.pid": 201}}
        for graph in ({"resources": []}, {"resources": [resource, resource]},
                      {"resources": [{**resource, "properties": {"project.path": "/foreign/Server.csproj", "executable.pid": 201}}]}):
            with self.subTest(graph=graph), self.assertRaises(RuntimeError):
                LAUNCHER.projects_process_identity({}, graph)
        with patch.object(LAUNCHER, "process_start", return_value="100"), patch.object(Path, "readlink", side_effect=PermissionError()), self.assertRaises(PermissionError):
            LAUNCHER.projects_process_identity({}, {"resources": [resource]})


class SupportedReadRestartDeadlineControls(unittest.TestCase):
    def test_stalled_ownership_guard_is_bounded_before_mutation(self):
        observation, started = {}, time.monotonic()
        with patch.object(LAUNCHER, "owns_current_apphost", side_effect=lambda _owned: time.sleep(30)), self.assertRaises(TimeoutError):
            LAUNCHER.mutate_owned_resource({}, "projects", "restart", lambda *_args, **_kwargs: self.fail("Mutation after stalled guard"), observation, budget_seconds=.05)
        self.assertLess(time.monotonic() - started, 1)
        self.assertFalse(observation["completed"])
        self.assertIsNone(LAUNCHER._command_deadline)

    def test_guard_command_uses_remaining_budget_and_reaps_its_child(self):
        with tempfile.TemporaryDirectory() as directory:
            ready = Path(directory) / "ready"
            code = "import os,sys,time;open(sys.argv[1],'w').write(str(os.getpid()));time.sleep(30)"
            started = time.monotonic()
            with self.assertRaises((TimeoutError, subprocess.TimeoutExpired)), LAUNCHER.restart_budget(.5):
                LAUNCHER.execute([sys.executable, "-c", code, str(ready)])
            self.assertLess(time.monotonic() - started, 2)
            self.assertIsNone(LAUNCHER.process_start(int(ready.read_text())))

    def test_runner_timeout_and_cancellation_reap_helpers_and_preserve_unrelated_process(self):
        code = "import os,subprocess,sys,time; child=subprocess.Popen([sys.executable,'-c','import time;time.sleep(30)']);open(sys.argv[1],'w').write(str(os.getpid())+' '+str(child.pid));time.sleep(30)"
        outsider = subprocess.Popen([sys.executable, "-c", "import time;time.sleep(30)"])
        try:
            for cancelled in (False, True):
                with self.subTest(cancelled=cancelled), tempfile.TemporaryDirectory() as directory:
                    ready, cleanup = Path(directory) / "ready", []
                    def interrupt(_process, **_kwargs):
                        deadline = time.monotonic() + 3
                        while not ready.exists() and time.monotonic() < deadline:
                            time.sleep(.01)
                        self.assertTrue(ready.exists())
                        raise KeyboardInterrupt
                    with contextlib.ExitStack() as stack:
                        if cancelled:
                            stack.enter_context(patch.object(subprocess.Popen, "communicate", autospec=True, side_effect=interrupt))
                        with self.assertRaises(KeyboardInterrupt if cancelled else subprocess.TimeoutExpired):
                            LAUNCHER.run_test_process([sys.executable, "-c", code, str(ready)], env=os.environ.copy(), timeout=1, cleanup=cleanup)
                    self.assertTrue(cleanup[0]["descendantsAbsent"])
                    for pid in map(int, ready.read_text().split()):
                        self.assertIsNone(LAUNCHER.process_start(pid))
                    self.assertIsNone(outsider.poll())
        finally:
            outsider.kill()
            outsider.wait(timeout=5)

    def test_regressions_use_the_refreshed_endpoint_file(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "endpoints.json"
            path.write_text(json.dumps({"eventstore-dapr-cli": "http://localhost:1111", "state-grpc": "http://localhost:2222"}))
            original = LAUNCHER.regression_environment({"preserved": "value"}, path)
            path.write_text(json.dumps({"eventstore-dapr-cli": "http://localhost:3333", "state-grpc": "http://localhost:4444"}))
            refreshed = LAUNCHER.regression_environment(original, path)
            self.assertEqual(refreshed["STORY61_DAPR_HTTP_ENDPOINT"], "http://localhost:3333")
            self.assertEqual(refreshed["STORY61_DAPR_GRPC_ENDPOINT"], "http://localhost:4444")
            self.assertEqual(refreshed["preserved"], "value")


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
        with patch.object(LAUNCHER, "execute", return_value="123 daprd"), patch.object(Path, "read_bytes", side_effect=PermissionError("unobservable")), self.assertRaises(PermissionError):
            list(LAUNCHER.sidecar_processes())
        with patch.object(LAUNCHER, "execute", return_value="123 daprd"), patch.object(Path, "read_bytes", side_effect=FileNotFoundError()):
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
                        calls = iter([original])
                        stack.enter_context(patch.object(LAUNCHER, "owned_sidecar_census", side_effect=lambda _scope: next(calls, {})))
                    LAUNCHER.cleanup_resources({"owned": True}, [], "scope", None, report, sidecar_wait_seconds=.03)
                self.assertEqual(stopped, [True])
                self.assertEqual(report["result"], "failed")
                self.assertFalse(report["ownedSidecarsAbsent"])

    def test_fresh_post_stop_census_detects_new_owned_sidecar(self):
        report = {"result": "passed", "cleanup": []}
        replacement = {"456": {"start": "replacement", "appId": "folders", "arguments": []}}
        calls = iter([{}])
        with patch.object(LAUNCHER, "owned_sidecar_census", side_effect=lambda _scope: next(calls, replacement)), patch.object(LAUNCHER, "stop_owned_apphost"):
            LAUNCHER.cleanup_resources({"owned": True}, [], "scope", None, report, sidecar_wait_seconds=.03)
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


class SupportedReadReplayEvidenceTests(unittest.TestCase):
    def valid_evidence(self):
        def digest(value):
            return hashlib.sha256(value.encode()).hexdigest().upper()
        sources, observations, fingerprints, watermarks = [], [], {}, []
        for tenant_number, tenant in enumerate(("primary", "foreign")):
            index_key = digest(tenant + ":index")
            maximum = tenant_number * 10 + 6
            watermarks.append({"tenantSha256": digest(tenant), "keySha256": index_key, "globalPosition": maximum, "status": "observed"})
            fingerprints[index_key] = {"keySha256": index_key, "bytesSha256": digest(tenant + ":index-bytes"), "etagSha256": digest(tenant + ":index-etag"), "status": "observed"}
            for project_number, project in enumerate(("a", "b")):
                detail_key = digest(tenant + ":" + project)
                last_position = tenant_number * 10 + (project_number + 2) * 2
                source = {"kind": "source", "tenantSha256": digest(tenant), "projectSha256": digest(project),
                          "sourceSha256": digest(tenant + project + "source"), "requestEventsSha256": digest(tenant + project + "events"),
                          "catalogSha256": digest("catalog"), "retainedDispatchIdSha256": digest(tenant + project + "seed"),
                          "detailKeySha256": detail_key, "indexKeySha256": index_key,
                          "observedHead": 2, "eventCount": 2, "complete": True,
                          "events": [{"sequence": sequence, "globalPosition": last_position - 2 + sequence, "timestamp": "2026-10-26T12:00:00+00:00",
                                      "sourceEventSha256": digest(tenant + project + str(sequence) + "source"),
                                      "projectionEventSha256": digest(tenant + project + str(sequence) + "projection")} for sequence in (1, 2)]}
                sources.append(source)
                watermarks.append({"tenantSha256": digest(tenant), "keySha256": detail_key, "globalPosition": last_position, "status": "observed"})
                fingerprints[detail_key] = {"keySha256": detail_key, "bytesSha256": digest(tenant + project + "bytes"), "etagSha256": digest(tenant + project + "etag"), "status": "observed"}
        for family in ("journal", "folder-acl", "tenant-access"):
            key = digest("non-projection:" + family)
            fingerprints[key] = {"keySha256": key, "bytesSha256": digest(family + "bytes"), "etagSha256": digest(family + "etag"), "status": "observed"}
        def attempt(kind, scenario, source):
            return {"kind": kind, "scenario": scenario, "route": "project/v2" if kind == "duplicate" else "project/rebuild/v1",
                    **{field: source[field] for field in ("tenantSha256", "projectSha256", "sourceSha256", "catalogSha256", "retainedDispatchIdSha256", "observedHead")},
                    "requestEventsSha256": source["requestEventsSha256"], "requestSha256": digest(kind + scenario + source["sourceSha256"]),
                    "dispatchIdSha256": source["retainedDispatchIdSha256"] if scenario == "retained" else digest(kind + scenario + source["sourceSha256"] + "dispatch"),
                    "eventCount": 2, "requestHead": None if kind == "duplicate" else 2, "httpStatus": 200, "protocolVersion": 2, "failureType": None,
                    "outcomes": [{"projectionType": "project-reads", "status": {"duplicate": "AlreadyCompleted", "invalid": "Failed", "rebuild": "Completed"}[kind]}]}
        for source in sources:
            observations.append(source)
            observations.extend(attempt("duplicate", scenario, source) for scenario in ("retained", "fresh"))
            for scenario in ("truncated", "mismatched-head"):
                value = attempt("invalid", scenario, source)
                if scenario == "truncated":
                    value.update(eventCount=1, requestEventsSha256=digest("truncated"))
                else:
                    value["requestHead"] = 1
                observations.append(value)
        for route in ("project/v2", "project/rebuild/v1"):
            for scenario in ("anonymous", "human-bearer", "public-user-workload", "wrong-operation"):
                observations.append({"kind": "authentication", "route": route, "scenario": scenario,
                                     **{field: sources[0][field] for field in ("tenantSha256", "projectSha256", "sourceSha256", "requestEventsSha256", "catalogSha256", "retainedDispatchIdSha256", "observedHead", "eventCount")},
                                     "requestSha256": digest(route + scenario + "request"), "dispatchIdSha256": digest(route + scenario + "dispatch"),
                                     "requestHead": sources[0]["observedHead"], "protocolVersion": None, "failureType": None, "httpStatus": 403 if scenario == "wrong-operation" else 401, "outcomes": None})
        def ordered(values):
            return [values[key].copy() for key in sorted(values)]
        def state(phase, before, after, changed):
            observations.append({"kind": "state", "phase": phase, "comparison": {
                "valid": True, "bothTenantsCompared": True, "domainKeysAndBytesUnchanged": True, "exactWatermarksUnchanged": True, "onlyExpectedETagsChanged": True,
                "expectedChangedETagKeys": sorted(changed), "changedETagKeys": sorted(changed), "before": ordered(before), "after": ordered(after),
                "beforeWatermarks": json.loads(json.dumps(watermarks)), "afterWatermarks": json.loads(json.dumps(watermarks))}})
        for phase in ("duplicate", "duplicate-reads", "invalid", "authentication", "refusal-reads"):
            state(phase, fingerprints, fingerprints, set())
        previous = {key: value.copy() for key, value in fingerprints.items()}
        for source in sources:
            observations.append(attempt("rebuild", "full-prefix", source))
            changed = {source["detailKeySha256"], source["indexKeySha256"]}
            after = {key: value.copy() for key, value in previous.items()}
            for key in changed:
                after[key]["etagSha256"] = digest(after[key]["etagSha256"] + "rebuilt")
            state("rebuild:" + source["tenantSha256"] + ":" + source["projectSha256"], previous, after, changed)
            previous = after
        projection_keys = {source[field] for source in sources for field in ("detailKeySha256", "indexKeySha256")}
        state("full-rebuild", fingerprints, previous, projection_keys)
        state("post-rebuild-reads", previous, previous, set())
        for phase in ("duplicate", "invalid", "authentication", "rebuild"):
            observations.append({"kind": "reads", "phase": phase, "readCount": 8, "bothTenantsCompared": True, "stableMetadataAgrees": True,
                                 "freshPagingWithoutGapsOrDuplicates": True, "listOpenAgree": True, "foreignAndAbsentDenials": True, "retainedContinuationsAccepted": phase != "rebuild"})
        observations.append({"kind": "cursor-control", "observation": {"httpStatus": 200, "responseState": "Complete", "itemCount": 1, "hasMore": True, "cursorPresent": True}})
        observations.append({"kind": "cursor-probe", "observation": {"attempt": 1, "httpStatus": 400, "failureType": None, "withinLifetime": True,
                             "observedAt": "2026-10-26T12:00:59+00:00",
                             "observerStartedAt": "2026-10-26T12:00:50+00:00", "observerDeadline": "2026-10-26T12:02:20+00:00",
                             "problem": {"status": 400, "reasonCode": "query_invalid_page", "reason": "invalid-cursor"}}})
        observations.append({"kind": "cursor", "acceptedHttpStatus": 200, "rejectedHttpStatus": 400, "withinLifetime": True,
                             "tenantSha256": sources[0]["tenantSha256"], "indexKeySha256": sources[0]["indexKeySha256"],
                             "beforeIndexETagSha256": fingerprints[sources[0]["indexKeySha256"]]["etagSha256"],
                             "afterIndexETagSha256": previous[sources[0]["indexKeySha256"]]["etagSha256"],
                             "observerStartedAt": "2026-10-26T12:00:50+00:00", "observerDeadline": "2026-10-26T12:02:20+00:00",
                             "rejectionProblem": {"status": 400, "reasonCode": "query_invalid_page", "reason": "invalid-cursor"},
                             "requestedAt": "2026-10-26T12:00:00+00:00", "acceptedAt": "2026-10-26T12:00:01+00:00", "rejectedAt": "2026-10-26T12:01:00+00:00"})
        matrix = {"cases": list(LAUNCHER.MATRIX_CASES), "allCasesExecuted": True, "projectsProcessRestartVerified": True,
                  "duplicateDeliveryVerified": True, "fullReplayVerified": True, "readOnlyDomainBytesAndEtagsUnchanged": True,
                  "acceptedEquivalence": False, "productionReadiness": False}
        def only(kind, phase=None):
            return next(value for value in observations if value["kind"] == kind and (phase is None or value.get("phase") == phase))
        ordered_observations = []
        for source in sources:
            ordered_observations.append(source)
            ordered_observations.extend(value for value in observations if value["kind"] == "duplicate" and value["sourceSha256"] == source["sourceSha256"])
        ordered_observations.extend((only("state", "duplicate"), only("reads", "duplicate"), only("state", "duplicate-reads")))
        ordered_observations.extend(value for value in observations if value["kind"] == "invalid")
        ordered_observations.extend((only("state", "invalid"), only("reads", "invalid")))
        ordered_observations.extend(value for value in observations if value["kind"] == "authentication")
        ordered_observations.extend((only("state", "authentication"), only("reads", "authentication"), only("state", "refusal-reads"), only("cursor-control")))
        for source in sources:
            ordered_observations.append(next(value for value in observations if value["kind"] == "rebuild" and value["sourceSha256"] == source["sourceSha256"]))
            ordered_observations.append(only("state", "rebuild:" + source["tenantSha256"] + ":" + source["projectSha256"]))
        ordered_observations.extend((only("state", "full-rebuild"), only("cursor-probe"), only("cursor"), only("reads", "rebuild"), only("state", "post-rebuild-reads")))
        return matrix, {"version": 1, "duplicateVerified": True, "replayVerified": True,
                        "ownedDomainKeyCensus": sorted(fingerprints), "observations": ordered_observations}

    def test_provider_observer_requires_live_exact_binary_and_version_control(self):
        observations = [{"keySha256": hashlib.sha256(str(index).encode()).hexdigest().upper(), "bytesSha256": "A" * 64, "etagSha256": "B" * 64} for index in range(6)]
        valid = {"verified": True, "binaryAndEscapingBoundariesExact": True, "domainKeyCount": 2, "controlKeyCount": 4, "individual": observations, "batched": observations}
        LAUNCHER.require_provider_observer_control(valid)
        for field in valid:
            missing = valid.copy()
            del missing[field]
            with self.subTest(missing=field), self.assertRaises(RuntimeError):
                LAUNCHER.require_provider_observer_control(missing)
        changed = json.loads(json.dumps(valid))
        changed["batched"][0]["etagSha256"] = "C" * 64
        with self.assertRaises(RuntimeError):
            LAUNCHER.require_provider_observer_control(changed)

    def test_complete_replay_evidence_requires_all_eleven_exact_rows(self):
        matrix, replay = self.valid_evidence()
        result = LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        self.assertEqual(result, {"sources": 4, "duplicateAttempts": 8, "rebuildAttempts": 4, "invalidReplayAttempts": 8, "authenticationRefusals": 8, "persistedComparisons": 11})
        for cases in (matrix["cases"][:-1], [*matrix["cases"][:-1], "unrelated-row"], [*matrix["cases"][:-1], matrix["cases"][0]], list(reversed(matrix["cases"]))):
            with self.subTest(cases=cases), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence({**matrix, "cases": cases}, replay)

    def test_missing_and_failed_replay_observations_never_qualify(self):
        matrix, valid = self.valid_evidence()
        for index in range(len(valid["observations"])):
            replay = json.loads(json.dumps(valid))
            replay["observations"].pop(index)
            with self.subTest(index=index), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        for kind, field, value in (("source", "complete", False), ("source", "observedHead", 3), ("duplicate", "httpStatus", 503),
                                   ("duplicate", "outcomes", []), ("rebuild", "sourceSha256", "0" * 64), ("rebuild", "requestHead", 1),
                                   ("invalid", "outcomes", [{"projectionType": "project-reads", "status": "Completed"}]),
                                   ("authentication", "httpStatus", 200), ("reads", "stableMetadataAgrees", False),
                                   ("cursor", "rejectedAt", "2026-10-26T12:16:00+00:00")):
            replay = json.loads(json.dumps(valid))
            next(item for item in replay["observations"] if item["kind"] == kind)[field] = value
            with self.subTest(kind=kind, field=field), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        for field, value in (("responseState", "Unavailable"), ("itemCount", 0), ("hasMore", False), ("cursorPresent", False)):
            replay = json.loads(json.dumps(valid))
            next(item for item in replay["observations"] if item["kind"] == "cursor-control")["observation"][field] = value
            with self.subTest(cursor_control=field), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        for flag in ("duplicateVerified", "replayVerified"):
            with self.subTest(flag=flag), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, {**valid, flag: False})

    def test_cursor_probes_accept_only_honest_unavailable_before_definitive_refusal(self):
        matrix, replay = self.valid_evidence()
        final = next(item for item in replay["observations"] if item["kind"] == "cursor-probe")
        final["observation"]["attempt"] = 2
        retry = {"kind": "cursor-probe", "observation": {"attempt": 1, "httpStatus": 200, "failureType": None, "withinLifetime": True,
                 "observedAt": "2026-10-26T12:00:58+00:00", "honestEmptyUnavailable": True,
                 "observerStartedAt": final["observation"]["observerStartedAt"], "observerDeadline": final["observation"]["observerDeadline"],
                 "admission": {"responseState": "Unavailable", "itemCount": 0, "hasMore": False, "cursorPresent": False,
                               "asOfPresent": True, "projectVersion": 0, "components": [{"included": False, "freshness": "Unavailable"}]}}}
        replay["observations"].insert(replay["observations"].index(final), retry)
        LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        for field, value in (("responseState", "Complete"), ("responseState", "Partial"), ("itemCount", 1), ("cursorPresent", True), ("hasMore", True)):
            invalid = json.loads(json.dumps(replay))
            next(item for item in invalid["observations"] if item["kind"] == "cursor-probe")["observation"]["admission"][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, invalid)
        invalid = json.loads(json.dumps(replay))
        invalid["observations"].remove(next(item for item in invalid["observations"] if item["kind"] == "cursor-probe" and item["observation"]["httpStatus"] == 400))
        with self.assertRaises(RuntimeError):
            LAUNCHER.require_distributed_replay_evidence(matrix, invalid)

    def test_complete_domain_census_prevents_consistent_non_projection_omission(self):
        matrix, replay = self.valid_evidence()
        projection_keys = {value[field] for value in replay["observations"] if value["kind"] == "source" for field in ("detailKeySha256", "indexKeySha256")}
        self.assertGreater(len(replay["ownedDomainKeyCensus"]), len(projection_keys))
        for state in (value["comparison"] for value in replay["observations"] if value["kind"] == "state"):
            for phase in ("before", "after"):
                state[phase] = [value for value in state[phase] if value["keySha256"] in projection_keys]
        with self.assertRaises(RuntimeError):
            LAUNCHER.require_distributed_replay_evidence(matrix, replay)

    def test_every_watermark_tuple_is_bound_to_actual_sources(self):
        matrix, valid = self.valid_evidence()
        for corruption in ("position-pair", "tenant-swap"):
            replay = json.loads(json.dumps(valid))
            tenants = list({value["tenantSha256"] for value in replay["observations"] if value["kind"] == "source"})
            for value in replay["observations"]:
                if value["kind"] != "state" or corruption == "position-pair" and value["phase"] in ("duplicate", "duplicate-reads"):
                    continue
                for phase in ("beforeWatermarks", "afterWatermarks"):
                    for watermark in value["comparison"][phase]:
                        if corruption == "position-pair":
                            watermark["globalPosition"] += 100
                        else:
                            watermark["tenantSha256"] = tenants[1 - tenants.index(watermark["tenantSha256"])]
            with self.subTest(corruption=corruption), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)

    def test_source_timestamps_require_timezone_and_non_default_value(self):
        matrix, valid = self.valid_evidence()
        for timestamp in ("garbage", "2026-10-26T12:00:00", "0001-01-01T00:00:00+00:00", "0001-01-01T01:00:00+01:00", "", None):
            replay = json.loads(json.dumps(valid))
            next(value for value in replay["observations"] if value["kind"] == "source")["events"][0]["timestamp"] = timestamp
            with self.subTest(timestamp=timestamp), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)

    def test_authentication_requires_all_emitted_bindings_and_fresh_dispatch(self):
        matrix, valid = self.valid_evidence()
        fields = ("tenantSha256", "projectSha256", "sourceSha256", "requestSha256", "requestEventsSha256", "catalogSha256", "dispatchIdSha256", "retainedDispatchIdSha256", "observedHead", "requestHead", "eventCount")
        for field in fields:
            for corruption in ("missing", "wrong"):
                replay = json.loads(json.dumps(valid))
                attempt = next(value for value in replay["observations"] if value["kind"] == "authentication")
                if corruption == "missing":
                    del attempt[field]
                else:
                    attempt[field] = 999 if type(attempt[field]) is int else "invalid"
                with self.subTest(field=field, corruption=corruption), self.assertRaises(RuntimeError):
                    LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        for other_kind in ("source", "authentication", "rebuild", "duplicate"):
            replay = json.loads(json.dumps(valid))
            attempts = [value for value in replay["observations"] if value["kind"] == "authentication"]
            other = next(value for value in replay["observations"] if value["kind"] == other_kind)
            attempts[-1]["dispatchIdSha256"] = other["retainedDispatchIdSha256"] if other_kind == "source" else other["dispatchIdSha256"]
            with self.subTest(reused_from=other_kind), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)

    def test_cursor_refusal_requires_platform_problem_and_separate_observer_bound(self):
        matrix, valid = self.valid_evidence()
        for problem in (None, {}, {"status": 400, "reasonCode": "query_invalid_page", "reason": "invalid-page"},
                        {"status": 400, "reasonCode": "query_malformed_request", "reason": "invalid-cursor"}, "malformed"):
            for kind, field in (("cursor-probe", "problem"), ("cursor", "rejectionProblem")):
                replay = json.loads(json.dumps(valid))
                observation = next(value for value in replay["observations"] if value["kind"] == kind)
                if kind == "cursor-probe":
                    observation = observation["observation"]
                observation[field] = problem
                with self.subTest(problem=problem, kind=kind), self.assertRaises(RuntimeError):
                    LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        for scenario in ("ten-minute", "late-success", "missing-start", "missing-deadline", "inconsistent-probe"):
            replay = json.loads(json.dumps(valid))
            cursor = next(value for value in replay["observations"] if value["kind"] == "cursor")
            probe = next(value for value in replay["observations"] if value["kind"] == "cursor-probe")["observation"]
            if scenario == "ten-minute":
                cursor["observerDeadline"] = probe["observerDeadline"] = "2026-10-26T12:10:50+00:00"
            elif scenario == "late-success":
                cursor["rejectedAt"] = "2026-10-26T12:02:21+00:00"
            elif scenario == "inconsistent-probe":
                probe["observerStartedAt"] = "2026-10-26T12:00:49+00:00"
            else:
                del cursor["observerStartedAt" if scenario == "missing-start" else "observerDeadline"]
            with self.subTest(scenario=scenario), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)

    def test_evidence_preserves_causal_order_between_operations_and_comparisons(self):
        matrix, valid = self.valid_evidence()
        for index, value in enumerate(valid["observations"]):
            if value["kind"] not in ("state", "reads", "cursor-control", "cursor-probe", "cursor"):
                continue
            replay = json.loads(json.dumps(valid))
            replay["observations"].insert(0, replay["observations"].pop(index))
            with self.subTest(kind=value["kind"], phase=value.get("phase")), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)

    def test_matrix_requires_read_only_and_explicit_local_proof_flags(self):
        matrix, replay = self.valid_evidence()
        for field in ("readOnlyDomainBytesAndEtagsUnchanged", "acceptedEquivalence", "productionReadiness"):
            for missing in (False, True):
                invalid = matrix.copy()
                if missing:
                    del invalid[field]
                else:
                    invalid[field] = not invalid[field]
                with self.subTest(field=field, missing=missing), self.assertRaises(RuntimeError):
                    LAUNCHER.require_distributed_replay_evidence(invalid, replay)

    def test_replay_comparisons_recompute_bytes_etags_watermarks_and_baseline_chain(self):
        matrix, valid = self.valid_evidence()
        for phase, section, field, value in (("duplicate", "after", "bytesSha256", "0" * 64),
                                             ("duplicate", "after", "etagSha256", "0" * 64),
                                             ("full-rebuild", "afterWatermarks", "globalPosition", 999),
                                             ("post-rebuild-reads", "before", "etagSha256", "0" * 64)):
            replay = json.loads(json.dumps(valid))
            state = next(item["comparison"] for item in replay["observations"] if item["kind"] == "state" and item["phase"] == phase)
            state[section][0][field] = value
            with self.subTest(phase=phase, section=section, field=field), self.assertRaises(RuntimeError):
                LAUNCHER.require_distributed_replay_evidence(matrix, replay)
        replay = json.loads(json.dumps(valid))
        source = next(item for item in replay["observations"] if item["kind"] == "source")
        source["events"][-1]["globalPosition"] += 1
        with self.assertRaises(RuntimeError):
            LAUNCHER.require_distributed_replay_evidence(matrix, replay)


if __name__ == "__main__":
    unittest.main()
