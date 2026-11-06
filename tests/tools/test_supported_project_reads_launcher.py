"""Ownership and exception controls for the supported-read launcher's real cleanup logic."""
import contextlib
import base64
import hashlib
import importlib.util
import io
import os
import socket
import sqlite3
import ssl
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
from types import SimpleNamespace
import urllib.error
import urllib.parse
import urllib.request
from unittest.mock import Mock, patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("supported_read_launcher", ROOT / "tools/qualification/run_supported_project_reads.py")
LAUNCHER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(LAUNCHER)


def container_id(name):
    return hashlib.sha256(name.encode()).hexdigest()


def resources(*names):
    return [{"name": name, "id": container_id(name)} for name in names]


class OwnerSourceReadCustodyTests(unittest.TestCase):
    def test_fresh_owner_source_custody_is_private_and_cannot_be_reused(self):
        with tempfile.TemporaryDirectory() as temporary:
            private = Path(temporary)
            directory = LAUNCHER.prepare_owner_source_read_directory(private)
            self.assertEqual(directory, private / "owner-source-reads")
            self.assertEqual(directory.stat().st_mode & 0o777, 0o700)
            with self.assertRaises(FileExistsError): LAUNCHER.prepare_owner_source_read_directory(private)

    def test_owner_source_custody_is_removed_with_owned_private_material(self):
        with tempfile.TemporaryDirectory() as temporary:
            private = Path(temporary) / "owned"
            private.mkdir(mode=0o700)
            directory = LAUNCHER.prepare_owner_source_read_directory(private)
            (directory / "actual.request").write_text("private request")
            report = {"cleanup": [], "result": "passed"}
            LAUNCHER.cleanup_private_material(private, "reads-test", None, False, report)
            self.assertTrue(report["privateMaterialRemoved"])
            self.assertFalse(directory.exists())


class NativeConsoleEvidenceTests(unittest.TestCase):
    def windows(self):
        return [{"trace": format(index + 1, "032x"), "kind": ("Denied", "Absent", "ForeignOnly")[index % 3],
                 "startUtc": f"2026-11-03T20:00:{index + 1:02d}+00:00", "endUtc": f"2026-11-03T20:00:{index + 1:02d}.100000+00:00"}
                for index in range(15)]

    def native_output(self, app="eventstore"):
        message = 'time="2026-11-03T19:59:50Z" level=info msg="Initialized name resolution to sqlite" scope=dapr.runtime type=log app_id=' + app
        return json.dumps({"logs": [{"resourceName": app + "-dapr-cli", "timestamp": "2026-11-03T19:59:50Z",
                                     "content": message, "isError": False}]})

    def write_native(self, path):
        windows = self.windows()
        observations = {kind + "Trace": windows[index]["trace"] for index, kind in enumerate(("Denied", "Absent", "ForeignOnly"))}
        observations.update(RequestWindows=[{"Trace": row["trace"], "StartUtc": row["startUtc"], "EndUtc": row["endUtc"]} for row in windows],
                            BoundedInterleavedTimingSamples=[{"Round": index // 3, "Kind": row["kind"], "Trace": row["trace"]}
                                                            for index, row in enumerate(windows[3:])])
        root = ET.Element("assemblies")
        test = ET.SubElement(root, "test", name="Hexalith.Projects.Server.Tests.Queries.SupportedProjectGatewayHttpTests.GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures",
                             result="Pass", **{"start-rtf": "2026-11-03T19:59:00Z", "finish-rtf": "2026-11-03T20:01:00Z"})
        ET.SubElement(test, "output").text = "Actual real-owner denial observations: " + json.dumps(observations)
        ET.ElementTree(root).write(path)
        return observations

    def test_windows_require_exact_passing_native_method_and_all_distinct_bounded_requests(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "server.xml"
            self.write_native(path)
            self.assertEqual(LAUNCHER.real_owner_request_windows(path), self.windows())
            for mutation in ("method", "failed", "duplicate", "overlap", "outside", "missing", "wrong-kind", "non-utc"):
                with self.subTest(mutation=mutation):
                    value = self.write_native(path)
                    tree = ET.parse(path)
                    test = tree.getroot().find("test")
                    if mutation == "method": test.set("name", "substitute")
                    elif mutation == "failed": test.set("result", "Fail")
                    elif mutation == "duplicate": value["RequestWindows"][-1]["Trace"] = value["RequestWindows"][0]["Trace"]
                    elif mutation == "overlap": value["RequestWindows"][-1]["StartUtc"] = value["RequestWindows"][0]["StartUtc"]
                    elif mutation == "outside": value["RequestWindows"][-1]["EndUtc"] = "2026-11-03T20:01:01Z"
                    elif mutation == "missing": value["RequestWindows"].pop()
                    elif mutation == "wrong-kind": value["BoundedInterleavedTimingSamples"][0]["Kind"] = "Other"
                    elif mutation == "non-utc": value["RequestWindows"][0]["StartUtc"] = "2026-11-03T20:00:01"
                    test.find("output").text = "Actual real-owner denial observations: " + json.dumps(value)
                    tree.write(path)
                    with self.assertRaises((RuntimeError, ValueError)): LAUNCHER.real_owner_request_windows(path)

    def test_console_coverage_refuses_empty_unbound_untimed_or_saturated_snapshots(self):
        began = LAUNCHER.utc_time("2026-11-03T19:59:40Z")
        valid = json.loads(self.native_output())
        facts = LAUNCHER.native_console_coverage(json.dumps(valid), "eventstore-dapr-cli", "eventstore", self.windows(), began, LAUNCHER.utc_time("2026-11-03T20:02:00Z"))
        self.assertTrue(facts["coverageVerified"])
        self.assertEqual(facts["windowCounts"], [0] * 15)
        for mutation in ("empty", "wrong-resource", "untimed", "non-utc", "wrong-app", "no-startup", "saturated", "after-request", "future-row"):
            with self.subTest(mutation=mutation):
                value = json.loads(self.native_output())
                if mutation == "empty": value["logs"] = []
                elif mutation == "wrong-resource": value["logs"][0]["resourceName"] = "foreign-dapr-cli"
                elif mutation == "untimed": value["logs"][0].pop("timestamp")
                elif mutation == "non-utc": value["logs"][0]["timestamp"] = "2026-11-03T19:59:50"
                elif mutation == "wrong-app": value["logs"][0]["content"] = value["logs"][0]["content"].replace("app_id=eventstore", "app_id=foreign")
                elif mutation == "no-startup": value["logs"][0]["content"] = "synthetic-protected-description"
                elif mutation == "saturated": value["logs"] *= 10000
                elif mutation == "after-request": value["logs"][0]["timestamp"] = "2026-11-03T20:00:02Z"
                elif mutation == "future-row": value["logs"].append(dict(value["logs"][0], timestamp="2026-11-03T20:03:00Z"))
                result = LAUNCHER.native_console_coverage(json.dumps(value), "eventstore-dapr-cli", "eventstore", self.windows(), began, LAUNCHER.utc_time("2026-11-03T20:02:00Z"))
                self.assertFalse(result["coverageVerified"])
                self.assertNotIn("synthetic-protected-description", json.dumps(result))
        self.assertFalse(LAUNCHER.native_console_coverage(self.native_output(), "eventstore-dapr-cli", "eventstore", self.windows(),
                                                       LAUNCHER.utc_time("2026-11-03T20:00:02Z"), LAUNCHER.utc_time("2026-11-03T20:02:00Z"))["coverageVerified"])

    def test_console_counts_keep_all_rows_in_each_actual_window_and_following_interval(self):
        value = json.loads(self.native_output())
        value["logs"].append(dict(value["logs"][0], timestamp="2026-11-03T20:00:01.050000Z", content="synthetic-private-runtime-record"))
        value["logs"].append(dict(value["logs"][0], timestamp="2026-11-03T20:00:50Z", content="synthetic-private-drain-record"))
        facts = LAUNCHER.native_console_coverage(json.dumps(value), "eventstore-dapr-cli", "eventstore", self.windows(), LAUNCHER.utc_time("2026-11-03T19:59:40Z"), LAUNCHER.utc_time("2026-11-03T20:02:00Z"))
        self.assertEqual(facts["windowCounts"], [1] + [0] * 14)
        self.assertEqual(facts["rowsFromFirstObservation"], 2)
        self.assertNotIn("synthetic-private", json.dumps(facts))

    def test_peer_census_refuses_foreign_config_missing_duplicate_or_replaced_native_process(self):
        with tempfile.TemporaryDirectory() as temporary:
            binary = Path(temporary) / "daprd"; binary.write_bytes(b"actual-native-control")
            configurations = {app: Path(temporary) / (app + ".configuration.json") for app in LAUNCHER.READ_APPS}
            census = {str(index + 1): {"appId": app, "start": "123", "arguments": [str(binary), "--config", str(configurations[app])]}
                      for index, app in enumerate(sorted(LAUNCHER.READ_APPS))}
            read_bytes = Path.read_bytes
            def native_environment(path):
                if str(path).startswith("/proc/") and path.name == "environ":
                    app = census[path.parent.name]["appId"]
                    return ("OTEL_SERVICE_NAME=" + app + "-dapr-cli\0OTEL_RESOURCE_ATTRIBUTES=service.instance.id=abcdefgh,private.unrelated=do-not-publish\0PRIVATE_UNRELATED=do-not-publish").encode()
                return read_bytes(path)
            with patch.object(LAUNCHER.Path, "read_bytes", native_environment), patch.object(LAUNCHER, "owns_current_apphost", return_value=True), patch.object(LAUNCHER, "owned_sidecar_census", return_value=census), patch.object(LAUNCHER, "process_start", return_value="123"):
                observed = LAUNCHER.native_console_peers({"_isolation": "owned"}, configurations)
                self.assertEqual(set(observed), LAUNCHER.READ_APPS)
                self.assertNotIn("do-not-publish", json.dumps(observed))
                self.assertTrue(all(peer["otelServiceName"] == app + "-dapr-cli" and peer["otelServiceInstanceId"] == "abcdefgh" for app, peer in observed.items()))
                for identity in (b"", b"OTEL_SERVICE_NAME=foreign\0OTEL_RESOURCE_ATTRIBUTES=service.instance.id=abcdefgh", b"OTEL_SERVICE_NAME=eventstore-dapr-cli\0OTEL_RESOURCE_ATTRIBUTES=service.instance.id=private/path", b"OTEL_SERVICE_NAME=eventstore-dapr-cli\0OTEL_RESOURCE_ATTRIBUTES=service.instance.id=abcdefgh,service.instance.id=abcdefgh"):
                    with self.subTest(otel=identity[:20]), patch.object(LAUNCHER.Path, "read_bytes", lambda path: identity if str(path).startswith("/proc/") else read_bytes(path)):
                        with self.assertRaises(RuntimeError): LAUNCHER.native_console_peers({"_isolation": "owned"}, configurations)
                for mutation in ("missing", "duplicate", "foreign-config", "replaced", "foreign-apphost"):
                    with self.subTest(mutation=mutation):
                        changed = json.loads(json.dumps(census))
                        if mutation == "missing": changed.pop("1")
                        elif mutation == "duplicate": changed["7"] = changed["1"]
                        elif mutation == "foreign-config": changed["1"]["arguments"][-1] += ".foreign"
                        with patch.object(LAUNCHER, "owned_sidecar_census", return_value=changed), patch.object(LAUNCHER, "process_start", return_value="999" if mutation == "replaced" else "123"), patch.object(LAUNCHER, "owns_current_apphost", return_value=mutation != "foreign-apphost"):
                            with self.assertRaises(RuntimeError): LAUNCHER.native_console_peers({"_isolation": "owned"}, configurations)

    def test_capture_retains_only_bounded_private_raw_and_checks_identity_after_all_six_reads(self):
        for mode in ("success", "truncated", "replacement", "raw-replaced"):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as temporary:
                base = Path(temporary); raw = base / "raw"; raw.mkdir(mode=0o700)
                self.write_native(base / "server.xml")
                configurations = {app: base / (app + ".configuration.json") for app in LAUNCHER.READ_APPS}
                receipts = {"identities": {str(path): {"sha256": "a" * 64} for path in configurations.values()}}
                peers = {app: {"pid": index + 1, "startTicks": "1", "binarySha256": "b" * 64, "otelServiceName": app + "-dapr-cli", "otelServiceInstanceId": "abcdefgh"} for index, app in enumerate(sorted(LAUNCHER.READ_APPS))}
                owned = {"_isolation": "owned"}
                report = {"sourceManifestSha256": "c" * 64, "ownedAppHostSession": {"appHostPid": 99}, "commands": [],
                          "discoveryAfterReads": {"ownedSidecars": [{"appId": app, **peer} for app, peer in peers.items()], "daprdSha256": ["b" * 64]}}
                calls = []
                def execute(command, **kwargs):
                    calls.append(command)
                    self.assertEqual(kwargs["timeout"], 15)
                    self.assertNotIn("--tail", command); self.assertNotIn("--follow", command); self.assertNotIn("--search", command)
                    if mode == "raw-replaced" and len(calls) == 6:
                        target = raw / (sorted(LAUNCHER.READ_APPS)[0] + ".json"); target.write_text("substituted")
                    return "synthetic-private" * 100000 if mode == "truncated" else self.native_output(command[2][:-9])
                after = {**peers, "eventstore": {**peers["eventstore"], "startTicks": "2"}} if mode == "replacement" else peers
                with patch.object(LAUNCHER.Path, "read_text", return_value="btime 0\n"), patch.object(LAUNCHER.tempfile, "mkdtemp", return_value=str(raw)), patch.object(LAUNCHER, "receiver_configurations", return_value=(configurations, receipts)), patch.object(LAUNCHER, "native_console_peers", side_effect=[peers, after]), patch.object(LAUNCHER, "execute", side_effect=execute):
                    if mode in ("replacement", "raw-replaced"):
                        with self.assertRaises(RuntimeError): LAUNCHER.capture_native_console(owned, base / "discovery.json", base / "discovery.sqlite", {}, base, report)
                    else: LAUNCHER.capture_native_console(owned, base / "discovery.json", base / "discovery.sqlite", {}, base, report)
                self.assertEqual(len(calls), 6)
                self.assertEqual(set(path.name for path in raw.iterdir()), {app + ".json" for app in LAUNCHER.READ_APPS})
                for path in raw.iterdir():
                    self.assertLessEqual(path.stat().st_size, 1024 * 1024)
                    self.assertEqual(path.stat().st_mode & 0o777, 0o600)
                facts = json.loads((base / "native-console.json").read_text())
                self.assertEqual(facts["coverageVerified"], mode == "success")
                self.assertFalse(facts["privacyEqualityAccepted"])
                self.assertNotIn("synthetic-private", json.dumps(facts))


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

    def test_native_resolver_failures_keep_only_closed_facts_across_real_console_formats(self):
        protected = "protected-tenant-and-private-path"
        message = ("[INIT_COMPONENT_FAILURE]: initialization error occurred for nr (sqlite/v1): "
                   "failed to perform migrations: failed to get connection: database is locked (5) (SQLITE_BUSY) " + protected)
        native = {"time": "2026-11-04T13:00:00Z", "level": "error", "msg": message,
                  "scope": "dapr.runtime", "type": "log", "app_id": protected, "ver": "1.18.7"}
        logfmt = 'time="2026-11-04T13:00:00Z" level=error msg=' + json.dumps(message) + ' scope=dapr.runtime type=log app_id=' + protected
        expected = ["SqliteBusy", "SqliteNameResolutionInitializationFailed", "SqliteNameResolutionMigrationsFailed"]
        for raw in (json.dumps(native), logfmt, json.dumps({"logs": [{"content": logfmt}]}),
                    json.dumps({"logs": [{"content": json.dumps(native)}]}),
                    json.dumps({"ignored": protected}) + "\n" + json.dumps(native)):
            with self.subTest(format=raw[:30]):
                facts = LAUNCHER.safe_log_facts(raw)
                self.assertEqual(facts["nativeStartupFacts"], expected)
                self.assertNotIn(protected, json.dumps(facts))

    def test_native_resolver_facts_require_the_exact_native_scope_severity_and_initialization(self):
        valid = {"scope": "dapr.runtime", "type": "log", "level": "error",
                 "msg": "[INIT_COMPONENT_FAILURE]: initialization error occurred for nr (sqlite/v1): SQLITE_LOCKED"}
        self.assertEqual(LAUNCHER.safe_log_facts(json.dumps(valid))["nativeStartupFacts"],
                         ["SqliteLocked", "SqliteNameResolutionInitializationFailed"])
        for key, value in (("scope", "application"), ("type", "payload"), ("level", "info"),
                           ("msg", "SQLITE_BUSY database is locked"), ("msg", "[INIT_COMPONENT_FAILURE]: nr (mdns/v1): SQLITE_BUSY")):
            with self.subTest(key=key, value=value):
                self.assertEqual(LAUNCHER.safe_log_facts(json.dumps(dict(valid, **{key: value})))["nativeStartupFacts"], [])
        for raw in ('time="unterminated', "unstructured SQLITE_BUSY", "{not-json}", "", "[]"):
            self.assertEqual(LAUNCHER.safe_log_facts(raw)["nativeStartupFacts"], [])

    def test_native_resolver_success_is_separate_from_any_initialization_failure(self):
        success = {"scope": "dapr.runtime", "type": "log", "level": "info", "msg": "Initialized name resolution to sqlite"}
        self.assertEqual(LAUNCHER.safe_log_facts(json.dumps(success))["nativeStartupFacts"], ["SqliteNameResolutionInitialized"])
        self.assertEqual(LAUNCHER.safe_log_facts(json.dumps(dict(success, level="error")))["nativeStartupFacts"], [])

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
                with patch.object(LAUNCHER.sys, "argv", ["launcher", "--scenario", "main", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
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
            sys.argv = ["launcher", "--scenario", "main", "--evidence", str(evidence)]
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
                with patch.object(LAUNCHER.sys, "argv", ["launcher", "--scenario", "main", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
                    self.assertEqual(LAUNCHER.main(), 1)
            self.assertEqual(execute.call_count, 1)
            self.assertEqual(json.loads((evidence / "run.json").read_text())["cleanup"], [])

    def test_blank_description_cannot_hide_an_existing_structured_session(self):
        with tempfile.TemporaryDirectory() as directory:
            evidence = Path(directory) / "evidence"
            replies = ["", json.dumps([{"appHostPath": str(LAUNCHER.APPHOST), "appHostPid": 101, "cliPid": 202}])]
            with patch.object(LAUNCHER.shutil, "which", return_value="/test/tool"), patch.object(LAUNCHER, "execute", side_effect=replies) as execute:
                with patch.object(LAUNCHER.sys, "argv", ["launcher", "--scenario", "main", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
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


class SupportedReadSourceManifestControls(unittest.TestCase):
    def test_linked_platform_helpers_and_consumed_manifests_bind_actual_bytes(self):
        import subprocess
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            platform = root / "references/Hexalith.Platform"
            platform.mkdir(parents=True)
            for repository in (root, platform):
                subprocess.run(["git", "init", "-q", str(repository)], check=True, capture_output=True)
            inputs = [root / "module/hexalith.module-manifest.v2.json", platform / "module/hexalith.module-manifest.v2.json",
                      platform / "DaprSelfHostedMtls.cs", platform / "DaprSelfHostedMtlsSubscriber.cs", platform / "hexalith-platform.json"]
            for path in inputs:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("original")
            archived = platform / "module/archived-evidence/NotABuildInput.cs"
            archived.parent.mkdir(parents=True)
            archived.write_text("historical")
            with patch.object(LAUNCHER, "ROOT", root):
                initial = LAUNCHER.source_manifest()
                self.assertNotIn(str(archived.relative_to(root)), initial)
                archived.write_text("changed historical evidence")
                self.assertEqual(initial, LAUNCHER.source_manifest())
                for path in inputs:
                    name = str(path.relative_to(root))
                    self.assertIn(name, initial)
                    path.write_text("mutated")
                    changed = LAUNCHER.source_manifest()
                    self.assertNotEqual(initial[name], changed[name])
                    self.assertEqual(set(initial), set(changed))
                    path.write_text("original")


class SupportedReadProjectsRestartControls(unittest.TestCase):
    def identity(self, pid=301, wrapper=201, started="100"):
        return {"resourceName": "projects-owned", "launcherPid": wrapper, "launcherStartTicks": started,
                "pid": pid, "startTicks": started, "binarySha256": "a" * 64, "assemblySha256": "b" * 64,
                "runtimeMode": {"environment": "Development", "options": {**LAUNCHER.RUNTIME_FLAGS,
                    "EventStore__RouteCatalog__ReuseHeldMetadataDispatch": "false"}}}

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
                    "ASPNETCORE_ENVIRONMENT": "Development", **LAUNCHER.RUNTIME_FLAGS,
                    "EventStore__RouteCatalog__ReuseHeldMetadataDispatch": "false"}
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

    def test_restart_refuses_changed_runtime_option_after_start(self):
        before, after = self.identity(), self.identity(302, 202, "200")
        after["runtimeMode"]["options"][next(iter(LAUNCHER.RUNTIME_FLAGS))] = "changed"
        observation = {}
        with self.assertRaises(RuntimeError):
            self.restart([], observation, identities=[before, before, after])
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

    def test_container_disappearance_requires_exact_missing_identity_and_fresh_absence(self):
        identity = container_id("disappearing-owned")
        outputs = [
            ("Error: No such object: " + identity + "\n", True),
            ("[]\nError response from daemon: No such container: " + identity + "\n", True),
            ("error: no such object: " + identity + "\n", True),
            ("\nerror: no such object: " + identity + "\n", True),
            ("[]\nerror: no such object: " + identity + "\n", True),
            ("error: no such object: " + container_id("foreign") + "\n", False),
            ("error: no such object: " + identity[:12] + "\n", False),
            ("error: no such object: " + identity + "f\n", False),
            ("error: no such object: " + identity + "\npermission denied\n", False),
            ("error: permission denied: " + identity + "\n", False),
            ("unrelated error: no such object: " + identity + "\n", False),
            ("ERROR: NO SUCH OBJECT: " + identity + "\n", False)]
        for output, known_missing in outputs:
            for fault in ("none", "still-present", "permission", "failed-listing"):
                report = {"result": "passed", "cleanup": []}
                failure = RuntimeError("private Docker diagnostic")
                failure.command_output = output if fault != "permission" else "permission denied"
                observation = RuntimeError("listing failed") if fault == "failed-listing" else ({identity: {"owned"}} if fault == "still-present" else {})
                with self.subTest(output=output, fault=fault), patch.object(LAUNCHER, "execute", side_effect=failure), patch.object(
                        LAUNCHER, "container_listing", side_effect=[{identity: {"owned"}}, observation]):
                    LAUNCHER.cleanup_resources(None, [{"name": "owned", "id": identity}], "scope", None, report)
                accepted = known_missing and fault == "none"
                self.assertEqual(report["cleanup"][0].get("absenceVerified", False), accepted)
                self.assertEqual(report["result"], "passed" if accepted else "failed")

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
            with patch.object(LAUNCHER.sys, "argv", ["launcher", "--scenario", "main", "--evidence", str(evidence)]), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(LAUNCHER.main(), 1)
            execute.assert_not_called()
            allocate.assert_not_called()


class SupportedReadCanonicalInfrastructureTests(unittest.TestCase):
    def setUp(self):
        self.workspace = tempfile.TemporaryDirectory(prefix="supported-read-controls-")
        self.addCleanup(self.workspace.cleanup)
        self.private = Path(self.workspace.name)
        self.isolation = "reads-0123456789abcdef"
        # Receipt shape is canonical; no infrastructure or domain state is started by these controls.
        self.run = Path(tempfile.gettempdir()) / "hexalith-dapr" / os.urandom(16).hex()
        self.run.mkdir(mode=0o700, parents=True)
        self.addCleanup(lambda: LAUNCHER.shutil.rmtree(self.run, ignore_errors=True))
        self.catalog = Path(tempfile.mkdtemp(prefix="hexalith-route-catalog-"))
        self.addCleanup(lambda: LAUNCHER.shutil.rmtree(self.catalog, ignore_errors=True))
        self.certificates = self.private / "mtls" / ("dapr-credentials-" + hashlib.sha256(str(self.run).encode()).hexdigest().upper()[:32])
        self.certificates.mkdir(mode=0o700, parents=True)
        (self.private / "catalog-custody").mkdir(mode=0o700)
        (self.private / "catalog-invocations").mkdir(mode=0o700)
        self.write(self.private / "mtls-plane.json", {"isolationId": self.isolation, "runDirectory": str(self.run), "certificateDirectory": str(self.certificates)})
        self.write(self.catalog / "catalog.json", {"test": "canonical-owner-catalog"})
        self.write(self.private / "catalog-plane.json", {"isolationId": self.isolation, "directory": str(self.catalog),
            "rootDigest": hashlib.sha256((self.catalog / "catalog.json").read_bytes()).hexdigest(), "custodyDirectory": str(self.private / "catalog-custody")})
        self.registry = self.private / "discovery.sqlite"
        self.configuration = self.private / "discovery.json"
        self.write(self.configuration, {})
        self.binary = self.private / "daprd"
        self.binary.write_bytes(b"synthetic-runtime")
        self.binary.chmod(0o600)
        self.graph, self.containers, self.processes, self.environments = {"resources": []}, {}, [], {}
        for file in ("ca.crt", "issuer.crt", "issuer.key"):
            (self.certificates / file).write_bytes(("private-" + file).encode())
            (self.certificates / file).chmod(0o600)
        for index, name in enumerate(LAUNCHER.MTLS_RESOURCES):
            identity, port = container_id(name), 54000 + index
            self.graph["resources"].append({"displayName": name, "state": "Running", "healthStatus": "Healthy", "properties": {"container.id": identity},
                "urls": [{"name": "grpc", "url": f"tcp://localhost:{port}"}]})
            self.containers[identity] = {"Id": identity, "Image": "sha256:" + container_id("image-" + name),
                "Config": {"Labels": {"hexalith.supported-reads": self.isolation}, "Image": "daprio/sentry:1.18.7" if name == "dapr-sentry" else "daprio/dapr:1.18.7",
                    "Cmd": ["--trust-domain=localhost", "--tls-enabled", "--sentry-address=dapr-sentry:50001"]},
                "NetworkSettings": {"Ports": {"50001/tcp": [{"HostIp": "127.0.0.1", "HostPort": str(port)}]}},
                "HostConfig": {"PortBindings": {"50001/tcp": [{"HostIp": "127.0.0.1", "HostPort": str(port)}]}},
                "Mounts": [{"Source": str(self.certificates), "Destination": "/var/run/dapr/credentials"},
                           {"Source": str(self.run / "scheduler"), "Destination": "/var/lock"}]}
        for index, app in enumerate(sorted(LAUNCHER.READ_APPS)):
            path = self.catalog / (app + ".configuration.json")
            self.write(path, {"spec": {"nameResolution": {"component": "sqlite", "version": "v1", "configuration": {"connectionString": str(self.registry)}},
                "accessControl": {"defaultAction": "deny", "trustDomain": "public", "policies": []}}})
            pid, port = str(21000 + index), 55000 + index
            self.processes.append([pid, str(self.binary), "--app-id", app, "--config", str(path), "--dapr-internal-grpc-port", str(port),
                "--enable-mtls", "--sentry-address", "localhost:54000", "--placement-host-address", "localhost:54001", "--scheduler-host-address", "localhost:54002"])
            environment = {"NAMESPACE": self.isolation, "DAPR_CONTROLPLANE_TRUST_DOMAIN": "localhost", "DAPR_CONTROLPLANE_NAMESPACE": "default",
                "DAPR_TRUST_ANCHORS": "private-ca.crt", "DAPR_CERT_CHAIN": "private-issuer.crt", "DAPR_CERT_KEY": "private-issuer.key"}
            self.environments[pid] = b"\0".join((key + "=" + value).encode() for key, value in environment.items())
        with contextlib.closing(sqlite3.connect(self.registry)) as database, database:
            database.execute("CREATE TABLE hosts (app_id TEXT, address TEXT, registration_id TEXT)")
            database.executemany("INSERT INTO hosts VALUES (?, ?, ?)", [(parts[3], "127.0.0.1:" + parts[7], parts[0]) for parts in self.processes])
        self.registry.chmod(0o600)

    def write(self, path, value):
        path.write_text(json.dumps(value))
        path.chmod(0o600)

    def execute(self, command, **_kwargs):
        if command[:2] == ["docker", "inspect"]:
            return json.dumps([self.containers[command[-1]]])
        if command[:2] == ["docker", "ps"]:
            return ""
        self.fail("Unexpected canonical observation command")

    def capture(self):
        with patch.object(LAUNCHER, "execute", side_effect=self.execute):
            return LAUNCHER.capture_infrastructure(self.private, self.isolation, self.graph)

    def observe(self):
        if not (self.private / "infrastructure-observed.json").exists():
            self.write(self.private / "infrastructure-observed.json", self.capture())
        original = Path.read_bytes
        def read(path):
            if str(path).startswith("/proc/"):
                return self.environments[path.parts[2]]
            return original(path)
        with patch.object(LAUNCHER, "sidecar_processes", return_value=self.processes), patch.object(Path, "read_bytes", read), patch.object(LAUNCHER, "sidecar_listens_at", return_value=True), patch.object(LAUNCHER, "process_start", return_value="observed-test-start"):
            return LAUNCHER.observe_discovery(self.configuration, self.registry, self.isolation)

    def test_captures_exact_three_canonical_container_images_and_listeners(self):
        item = self.containers[container_id(LAUNCHER.MTLS_RESOURCES[0])]
        item["NetworkSettings"]["Ports"]["unpublished/tcp"] = None
        item["NetworkSettings"]["Ports"]["8080/tcp"] = [{"HostIp": "127.0.0.1", "HostPort": "54100"}]
        item["HostConfig"]["PortBindings"]["8080/tcp"] = [{"HostIp": "127.0.0.1", "HostPort": "0"}]
        capture = self.capture()
        self.assertEqual({item["name"] for item in capture["containers"]}, set(LAUNCHER.MTLS_RESOURCES))
        self.assertEqual(len({item["id"] for item in capture["containers"]}), 3)
        self.assertTrue(all(item["imageId"].startswith("sha256:") and item["bindings"] for item in capture["containers"]))

    def test_all_six_rewritten_configurations_use_one_registry_and_native_mtls_identity(self):
        observation = self.observe()
        self.assertEqual(len(observation["ownedSidecars"]), 6)
        self.assertTrue(all(item["mtlsIdentityVerified"] for item in observation["ownedSidecars"]))
        self.assertEqual(set(observation["configurationSha256"]), LAUNCHER.READ_APPS)
        self.assertIsNotNone(json.loads((self.private / "infrastructure-observed.json").read_text())["registryIdentity"])

    def test_missing_changed_or_foreign_receipt_refuses_capture(self):
        path = self.private / "mtls-plane.json"
        original = path.read_text()
        path.unlink()
        with self.assertRaises(FileNotFoundError): self.capture()
        path.write_text(original)
        path.chmod(0o600)
        receipt = json.loads(original)
        receipt["isolationId"] = "foreign"
        self.write(path, receipt)
        with self.assertRaises(RuntimeError): self.capture()

    def test_receipt_symlink_foreign_owner_shared_mode_and_escape_refuse_capture(self):
        path = self.private / "mtls-plane.json"
        path.chmod(0o644)
        with self.assertRaises(RuntimeError): self.capture()
        path.chmod(0o600)
        with patch.object(LAUNCHER.os, "getuid", return_value=os.getuid() + 1), self.assertRaises(RuntimeError): self.capture()
        target = self.private / "receipt-real.json"
        path.rename(target)
        path.symlink_to(target)
        with self.assertRaises(RuntimeError): self.capture()
        path.unlink()
        target.rename(path)
        receipt = json.loads(path.read_text())
        receipt["runDirectory"] = str(self.private)
        self.write(path, receipt)
        with self.assertRaises(RuntimeError): self.capture()

    def test_catalog_root_and_receiver_registry_or_namespace_mismatch_refuse_capture(self):
        catalog = self.catalog / "catalog.json"
        original = catalog.read_bytes()
        catalog.write_bytes(b"changed-catalog")
        with self.assertRaises(RuntimeError): self.capture()
        catalog.write_bytes(original)
        path = self.catalog / "projects.configuration.json"
        configuration = json.loads(path.read_text())
        configuration["spec"]["nameResolution"]["configuration"]["connectionString"] += "-foreign"
        self.write(path, configuration)
        with self.assertRaises(RuntimeError): self.capture()
        configuration["spec"]["nameResolution"]["configuration"]["connectionString"] = str(self.registry)
        configuration["spec"]["accessControl"]["policies"] = [{"namespace": "default", "trustDomain": "public"}]
        self.write(path, configuration)
        with self.assertRaises(RuntimeError): self.capture()

    def test_container_missing_identity_owner_image_mount_and_listener_refuse_capture(self):
        name = LAUNCHER.MTLS_RESOURCES[0]
        original = json.dumps(self.containers)
        for fault in ("identity", "owner", "image", "mount", "listener", "tls"):
            self.containers = json.loads(original)
            item = self.containers[container_id(name)]
            if fault == "identity": item["Id"] = container_id("replacement")
            if fault == "owner": item["Config"]["Labels"]["hexalith.supported-reads"] = "foreign"
            if fault == "image": item["Image"] = "mutable-image"
            if fault == "mount": item["Mounts"] = []
            if fault == "listener": item["NetworkSettings"]["Ports"]["50001/tcp"][0]["HostIp"] = "0.0.0.0"
            if fault == "tls": item["Config"]["Cmd"] = []
            with self.subTest(fault=fault), self.assertRaises(RuntimeError): self.capture()

    def test_failed_container_inspection_and_missing_model_never_prove_readiness(self):
        with patch.object(LAUNCHER, "execute", side_effect=OSError("unobservable")), self.assertRaises(OSError):
            LAUNCHER.capture_infrastructure(self.private, self.isolation, self.graph)
        self.graph["resources"].pop()
        with self.assertRaises(RuntimeError): self.capture()

    def test_changed_configuration_and_replaced_registry_refuse_discovery(self):
        self.observe()
        path = self.catalog / "projects.configuration.json"
        path.write_text(path.read_text() + " ")
        with self.assertRaises(RuntimeError): self.observe()
        path.write_text(path.read_text().rstrip())
        replacement = self.private / "replacement.sqlite"
        replacement.write_bytes(self.registry.read_bytes())
        replacement.chmod(0o600)
        replacement.replace(self.registry)
        with self.assertRaises(RuntimeError): self.observe()

    def test_changed_receipt_and_issuer_identity_after_capture_refuse_discovery(self):
        self.observe()
        path = self.private / "mtls-plane.json"
        original = path.read_text()
        path.write_text(original + " ")
        with self.assertRaises(RuntimeError): self.observe()
        path.write_text(original)
        (self.certificates / "issuer.key").write_text("replacement-private-key")
        with self.assertRaises(RuntimeError): self.observe()

    def test_missing_or_duplicate_sidecar_and_false_identity_refuse_discovery(self):
        self.observe()
        original = json.dumps(self.processes)
        for fault in ("missing", "duplicate", "config", "namespace", "control-domain", "credential", "mtls", "placement"):
            self.processes = json.loads(original)
            previous = self.environments[self.processes[0][0]]
            if fault == "missing": self.processes.pop()
            if fault == "duplicate": self.processes.append(self.processes[0])
            if fault == "config": self.processes[0][5] = str(self.configuration)
            if fault == "namespace": self.environments[self.processes[0][0]] = previous.replace(self.isolation.encode(), b"foreign")
            if fault == "control-domain": self.environments[self.processes[0][0]] = previous.replace(b"TRUST_DOMAIN=localhost", b"TRUST_DOMAIN=public")
            if fault == "credential": self.environments[self.processes[0][0]] = previous.replace(b"private-issuer.key", b"foreign-key")
            if fault == "mtls": self.processes[0].remove("--enable-mtls")
            if fault == "placement": self.processes[0][-3] = "localhost:50005"
            with self.subTest(fault=fault), self.assertRaises(RuntimeError): self.observe()
            self.environments[json.loads(original)[0][0]] = previous

    def test_extra_namespace_process_or_registry_registration_refuses_discovery(self):
        self.observe()
        original = list(self.processes)
        self.processes.append(["29999", str(self.binary), "--app-id", "unowned"])
        self.environments["29999"] = ("NAMESPACE=" + self.isolation).encode()
        with self.assertRaises(RuntimeError): self.observe()
        self.processes = original
        with contextlib.closing(sqlite3.connect(self.registry)) as database, database: database.execute("INSERT INTO hosts VALUES ('foreign', '127.0.0.1:1234', 'foreign')")
        with self.assertRaises(RuntimeError): self.observe()

    def test_observation_failure_never_proves_discovery(self):
        self.write(self.private / "infrastructure-observed.json", self.capture())
        with patch.object(LAUNCHER, "sidecar_processes", side_effect=PermissionError("unobservable")), self.assertRaises(PermissionError):
            LAUNCHER.observe_discovery(self.configuration, self.registry, self.isolation)

    def test_canonical_public_pems_and_sqlite_are_private_through_their_owned_parent(self):
        for file in self.certificates.iterdir(): file.chmod(0o644)
        self.registry.chmod(0o644)
        self.assertEqual(len(self.observe()["ownedSidecars"]), 6)
        self.certificates.chmod(0o755)
        with self.assertRaises(RuntimeError): self.observe()

    def test_canonical_cleanup_accepts_stopped_container_runtime_ports_but_rejects_changed_config(self):
        resource = self.capture()["containers"][0]
        item = self.containers[resource["id"]]
        item["NetworkSettings"]["Ports"] = {}
        present, removed = {resource["id"]}, []
        def execute(command, **_kwargs):
            if command[:3] == ["docker", "inspect", "--format"]:
                return json.dumps({"id": item["Id"], "owner": self.isolation})
            if command[:2] == ["docker", "inspect"]: return json.dumps([item])
            if command[:2] == ["docker", "rm"]:
                present.remove(command[-1])
                removed.append(command[-1])
                return ""
            self.fail("Unexpected canonical cleanup command")
        for changed in (False, True):
            present.add(resource["id"])
            removed.clear()
            if changed: item["HostConfig"]["PortBindings"] = {}
            report = {"result": "passed", "cleanup": []}
            with self.subTest(changed=changed), patch.object(LAUNCHER, "execute", side_effect=execute), patch.object(LAUNCHER, "container_listing", side_effect=lambda: {identity: {"canonical"} for identity in present}):
                LAUNCHER.cleanup_resources(None, [resource], self.isolation, lambda *_args, **_kwargs: self.fail("Unowned host"), report)
            self.assertEqual(removed, [] if changed else [resource["id"]])
            self.assertEqual(report["result"], "failed" if changed else "passed")

    def cleanup_report(self):
        return {"result": "passed", "cleanup": [{"resource": "exact-apphost", "absenceVerified": True}], "ownedSidecarsAbsent": True}

    def test_cleanup_removes_only_canonical_paths_after_exact_absence(self):
        capture, report = self.capture(), self.cleanup_report()
        external = Path(tempfile.mkdtemp(prefix="supported-reads-external-"))
        self.addCleanup(lambda: LAUNCHER.shutil.rmtree(external, ignore_errors=True))
        with patch.object(LAUNCHER, "execute", side_effect=self.execute):
            LAUNCHER.cleanup_private_material(self.private, self.isolation, capture, True, report)
        self.assertTrue(report["privateMaterialRemoved"])
        self.assertFalse(self.run.exists())
        self.assertFalse(self.catalog.exists())
        self.assertTrue(external.exists())

    def test_partial_startup_mtls_receipt_without_catalog_retains_recovery_material(self):
        (self.private / "catalog-plane.json").unlink()
        report = self.cleanup_report()
        with self.assertRaises((RuntimeError, FileNotFoundError)):
            LAUNCHER.read_infrastructure_receipts(self.private, self.isolation)
        with patch.object(LAUNCHER, "execute", side_effect=self.execute):
            LAUNCHER.cleanup_private_material(self.private, self.isolation, None, True, report)
        self.assertTrue(report["recoveryMaterialRetained"])
        self.assertFalse(report["privateMaterialRemoved"])
        self.assertTrue(self.run.exists())
        self.assertTrue(self.certificates.exists())
        self.assertEqual(report["result"], "failed")

    def test_cleanup_accepts_sdk_disposal_of_catalog_after_captured_validation(self):
        capture, report = self.capture(), self.cleanup_report()
        LAUNCHER.shutil.rmtree(self.catalog)
        with patch.object(LAUNCHER, "execute", side_effect=self.execute):
            LAUNCHER.cleanup_private_material(self.private, self.isolation, capture, True, report)
        self.assertTrue(report["privateMaterialRemoved"])

    def test_cleanup_retains_recovery_on_missing_receipt_changed_ownership_or_failed_absence(self):
        capture = self.capture()
        for fault in ("no-capture", "sidecar", "host", "container", "observation", "changed", "private-removal"):
            report = self.cleanup_report()
            if fault == "sidecar": report["ownedSidecarsAbsent"] = False
            if fault == "host": report["cleanup"][0]["absenceVerified"] = False
            if fault == "changed": report["infrastructureOwnershipChanged"] = True
            with self.subTest(fault=fault), contextlib.ExitStack() as stack:
                stack.enter_context(patch.object(LAUNCHER, "execute", side_effect=OSError("unobservable") if fault == "observation" else None, return_value="unexpected-container" if fault == "container" else ""))
                if fault == "private-removal": stack.enter_context(patch.object(LAUNCHER.shutil, "rmtree", side_effect=OSError("removal failed")))
                LAUNCHER.cleanup_private_material(self.private, self.isolation, None if fault == "no-capture" else capture, True, report)
            self.assertTrue(self.private.exists())
            self.assertTrue(report["recoveryMaterialRetained"])
            self.assertFalse(report["privateMaterialRemoved"])
        (self.private / "mtls-plane.json").unlink()
        report = self.cleanup_report()
        with patch.object(LAUNCHER, "execute", return_value=""):
            LAUNCHER.cleanup_private_material(self.private, self.isolation, capture, True, report)
        self.assertTrue(report["recoveryMaterialRetained"])

    def test_cleanup_retains_replaced_canonical_directory_and_extra_issuer(self):
        capture = self.capture()
        (self.certificates.parent / "unexpected").mkdir(mode=0o700)
        report = self.cleanup_report()
        with patch.object(LAUNCHER, "execute", return_value=""):
            LAUNCHER.cleanup_private_material(self.private, self.isolation, capture, True, report)
        self.assertTrue(report["recoveryMaterialRetained"])
        self.assertTrue(self.run.exists())


class SupportedReadDelegatedIssuerTests(unittest.TestCase):
    def test_https_password_endpoint_issues_only_exact_bootstrap_human_and_rs256_signature(self):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            LAUNCHER.execute(["openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "1", "-subj", "/CN=localhost",
                "-addext", "subjectAltName=IP:127.0.0.1", "-keyout", str(private / "certificate.key"), "-out", str(private / "certificate.pem")])
            LAUNCHER.execute(["openssl", "genpkey", "-algorithm", "RSA", "-pkeyopt", "rsa_keygen_bits:2048", "-out", str(private / "user.pem")])
            issuer, password = f"https://127.0.0.1:{LAUNCHER.free_port()}/users", "synthetic-private-password"
            server, thread = LAUNCHER.issuer_server(private, issuer, password)
            self.addCleanup(server.server_close)
            self.addCleanup(server.shutdown)
            context = ssl.create_default_context(cafile=str(private / "certificate.pem"))
            valid = {"grant_type": "password", "client_id": "supported-read-bootstrap", "username": "actor-1", "password": password}
            def request(form, path="/token", content_type="application/x-www-form-urlencoded"):
                body = urllib.parse.urlencode(form).encode() if isinstance(form, dict) else form
                return urllib.request.urlopen(urllib.request.Request(issuer + path, data=body, headers={"Content-Type": content_type}), context=context, timeout=10)
            with request(valid) as response:
                self.assertEqual(response.headers["Cache-Control"], "no-store")
                credential = json.load(response)
            header, payload, signature = credential["access_token"].split(".")
            decode = lambda value: base64.urlsafe_b64decode(value + "=" * (-len(value) % 4))
            self.assertEqual(json.loads(decode(header))["alg"], "RS256")
            claims = json.loads(decode(payload))
            self.assertEqual((claims["sub"], claims["iss"], claims["aud"]), ("actor-1", issuer, "supported-read-users"))
            self.assertNotIn("tenant_id", claims)
            self.assertNotIn("eventstore:tenant", claims)
            self.assertEqual(claims["exp"] - claims["iat"], 120)
            self.assertEqual(claims["global_admin"], "true")
            self.assertEqual(claims["eventstore:permission"], ["global-administrators:bootstrap"])
            (private / "signature").write_bytes(decode(signature))
            (private / "unsigned").write_text(header + "." + payload)
            LAUNCHER.execute(["openssl", "pkey", "-in", str(private / "user.pem"), "-pubout", "-out", str(private / "user-public.pem")])
            self.assertIn("Verified OK", LAUNCHER.execute(["openssl", "dgst", "-sha256", "-verify", str(private / "user-public.pem"), "-signature", str(private / "signature"), str(private / "unsigned")]))
            for key in valid:
                invalid = dict(valid, **{key: "foreign"})
                with self.subTest(key=key), self.assertRaises(urllib.error.HTTPError) as failure: request(invalid)
                self.assertEqual(failure.exception.code, 400)
                failure.exception.close()
            for body in (b"password=", b"%FF=x", urllib.parse.urlencode(dict(valid, password="\u00e9")).encode(), urllib.parse.urlencode(valid).encode() + b"&username=foreign", b"x" * 4097):
                with self.subTest(body_length=len(body)), self.assertRaises(urllib.error.HTTPError) as failure: request(body)
                failure.exception.close()
            with self.assertRaises(urllib.error.HTTPError) as failure: request(valid, path="/other")
            failure.exception.close()
            with self.assertRaises(urllib.error.HTTPError) as failure: request(valid, content_type="application/json")
            failure.exception.close()
            with patch.object(LAUNCHER.subprocess, "run", side_effect=subprocess.TimeoutExpired("openssl", 5)), self.assertRaises(urllib.error.HTTPError) as failure:
                request(valid)
            self.assertEqual(failure.exception.code, 503)
            failure.exception.close()


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
        for cases in (matrix["cases"][:-1], [*matrix["cases"][:-1], "unrelated-row"], [*matrix["cases"][:-1], matrix["cases"][0]],
                      list(reversed(matrix["cases"])), [*matrix["cases"][2:], *matrix["cases"][:2]]):
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

class SupportedReadScenarioGroupTests(unittest.TestCase):
    def matrix(self, scenario):
        flags = {"readOnlyDomainBytesAndEtagsUnchanged", "projectsProcessRestartVerified", "duplicateDeliveryVerified", "fullReplayVerified",
                 "preRestartContinuationAcceptedAfterRestart", "freshPagingAgrees", "retainedCursorAcceptedImmediatelyBeforeRevocation",
                 "cursorRejectionWithinLifetime", "postRestartForeignAndAbsentDenialsVerified", "bothTenantDomainWatermarksUnchanged",
                 "actualDaprDiscovery", "actualOwnerActorHeads", "acceptedEquivalence", "productionReadiness"}
        enabled = {"readOnlyDomainBytesAndEtagsUnchanged", "freshPagingAgrees", "bothTenantDomainWatermarksUnchanged", "actualDaprDiscovery", "actualOwnerActorHeads"}
        enabled |= ({"projectsProcessRestartVerified", "preRestartContinuationAcceptedAfterRestart", "retainedCursorAcceptedImmediatelyBeforeRevocation", "cursorRejectionWithinLifetime", "postRestartForeignAndAbsentDenialsVerified"} if scenario == "main" else {"duplicateDeliveryVerified"} if scenario == "duplicate" else {"fullReplayVerified"})
        return {"version": 2, "qualificationGroup": scenario, "providerIsolationSha256": self.provider_sha(scenario), "allGroupCasesExecuted": True, "cases": list(LAUNCHER.SCENARIO_CASES[scenario]),
                "providerScope": "one-fresh-owned-runtime", **{flag: flag in enabled for flag in flags}}

    def replay(self, scenario):
        _, old = SupportedReadReplayEvidenceTests().valid_evidence()
        source = [value for value in old["observations"] if value["kind"] == "source"]
        if scenario == "duplicate":
            observations = [value for value in old["observations"] if value["kind"] in {"source", "duplicate"}
                            or value["kind"] == "state" and value["phase"] in {"duplicate", "duplicate-reads"}
                            or value["kind"] == "reads" and value["phase"] == "duplicate"]
        else:
            observations = []
            for item in source:
                observations.append(item)
                observations.extend(value for value in old["observations"] if value["kind"] == "invalid" and value["sourceSha256"] == item["sourceSha256"])
            observations.extend(value for value in old["observations"] if value["kind"] not in {"source", "duplicate", "invalid"}
                                and not (value["kind"] == "state" and value["phase"] in {"duplicate", "duplicate-reads"})
                                and not (value["kind"] == "reads" and value["phase"] == "duplicate"))
        return {"version": 2, "qualificationGroup": scenario, "providerIsolationSha256": self.provider_sha(scenario), "sourceBaselineVerified": True, "duplicateVerified": scenario == "duplicate",
                "replayVerified": scenario == "full-replay", "ownedDomainKeyCensus": old["ownedDomainKeyCensus"], "observations": observations}

    def provider_sha(self, scenario):
        return hashlib.sha256(("reads-" + hashlib.sha256(scenario.encode()).hexdigest()[:24]).encode()).hexdigest().upper()

    def assembly_path(self, scenario):
        return "/tmp/native-" + scenario + "/Hexalith.Projects.Integration.Tests.dll"

    def native(self, scenario, total=1, provider_sha=None, assembly_path=None, assembly_sha=None):
        root = ET.Element("assemblies")
        assembly = ET.SubElement(root, "assembly", {"name": assembly_path or self.assembly_path(scenario), "total": str(total), "passed": str(total), "failed": "0", "skipped": "0", "not-run": "0", "errors": "0"})
        collection = ET.SubElement(assembly, "collection")
        for _ in range(total):
            test = ET.SubElement(collection, "test", {"type": LAUNCHER.SCENARIO_TEST_TYPE, "method": LAUNCHER.SCENARIO_METHODS[scenario], "result": "Pass"})
            ET.SubElement(test, "output").text = "supported-read-scenario-v2|" + scenario + "|" + (provider_sha or self.provider_sha(scenario)) + "|" + (assembly_sha or "e" * 64)
        return root

    def discovery(self):
        return {"resolver": "sqlite", "version": "v1", "alphaTestInfrastructureOnly": True, "daprdSha256": ["a" * 64],
                "configurationSha256": {app: hashlib.sha256(app.encode()).hexdigest() for app in LAUNCHER.READ_APPS}, "registryIdentitySha256": "b" * 64,
                "ownedSidecars": [{"appId": app, "pid": index + 100, "startTicks": str(index + 200), "namespaceVerified": True,
                                   "registryAddressVerified": True, "mtlsIdentityVerified": True} for index, app in enumerate(sorted(LAUNCHER.READ_APPS))], "observationsRequired": 1}

    def modes(self):
        return {app: {"wrapperPid": index + 10, "wrapperStartTicks": str(index + 20), "pid": index + 30, "startTicks": str(index + 40),
                      "environment": "Development", "options": {**LAUNCHER.RUNTIME_FLAGS, "EventStore__RouteCatalog__ReuseHeldMetadataDispatch": "true" if app == "folders" else "false"},
                      "assemblySha256": "c" * 64} for index, app in enumerate(sorted(LAUNCHER.READ_APPS))}

    def profile(self):
        return {project: {"Configuration": "Debug", "Optimize": "false", "DefineConstants": "TRACE;DEBUG;NET10_0", "DebugType": "portable",
                          "UseHexalithProjectReferences": "true", "UseNuGetDeps": "false", "EnablePrivateCatalogInvocationProof": "true", "EnablePrivateOwnerSourceReadProof": "true"} for project in LAUNCHER.COMPILATION_PROJECTS}

    def test_owner_source_helper_requires_explicit_compilation_selection(self):
        for selected in (None, "false"):
            profile = self.profile()
            values = next(iter(profile.values()))
            if selected is None: values.pop("EnablePrivateOwnerSourceReadProof")
            else: values["EnablePrivateOwnerSourceReadProof"] = selected
            with self.subTest(selected=selected), self.assertRaises(RuntimeError): LAUNCHER.require_compilation_profile(profile)

    def report(self, scenario, frozen, regressions=False):
        index = LAUNCHER.SCENARIO_GROUPS.index(scenario)
        digest = lambda text: hashlib.sha256(text.encode()).hexdigest()
        names = [*LAUNCHER.MTLS_RESOURCES, "reads-" + digest(scenario)[:24] + "-redishost"]
        ids = {name: digest(scenario + name) for name in names}
        session = {"appHostPid": index + 1000, "appHostStartTicks": str(index + 2000), "cliPid": index + 3000, "cliStartTicks": str(index + 4000),
                   "configurationSha256": digest(scenario + "config"), "configurationPathSha256": digest(scenario + "config-path")}
        sources = {name: {"allSourceProjects": True, "rootAvailableReferences": 1, "sourceProjects": {"FixtureDependency": str(ROOT / "src/FixtureDependency.csproj")}}
                   for name in ["Hexalith.Projects.Server", "Hexalith.Projects.Workers", "Hexalith.Projects.AppHost", "Hexalith.Projects.ReadAuthority", "Hexalith.EventStore", "Hexalith.Tenants", "Hexalith.Folders", "Hexalith.Folders.Server", "Hexalith.Folders.Workers"]}
        mapping = {key: "/tmp/mapping-" + scenario + "/" + key for key in ["CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets"]}
        mapping.update(Configuration="Debug", HEXALITH_SOURCE_MAPPING_HASH=digest(scenario))
        report = {"result": "passed", "qualificationGroup": scenario, "providerIsolationSha256": self.provider_sha(scenario), "matrixCompleted": True, "ownedSidecarsAbsent": True, "preexistingResourcesPreserved": True,
                  "privateMaterialRemoved": True, "ownedSidecarsObservedBeforeStop": 6, "cleanup": [{"resource": "exact-apphost", "removed": True, "absenceVerified": True},
                      *({"resource": name, "containerId": ids[name], "removed": True, "absenceVerified": True} for name in names)],
                  "mtlsInfrastructure": [{"name": name, "id": ids[name], "imageId": "sha256:" + "d" * 64, "canonical": True} for name in LAUNCHER.MTLS_RESOURCES],
                  "ownedAppHostSession": session, "discovery": self.discovery(), "discoveryAfterReads": self.discovery(), "runtimeModes": self.modes(),
                  "sourceMappingEnvironment": mapping, "sourceMappingHash": mapping["HEXALITH_SOURCE_MAPPING_HASH"], "compilationProfile": self.profile(),
                  "liveHostSourceReferences": sources, "sourceManifestSha256": hashlib.sha256(json.dumps(frozen, sort_keys=True).encode()).hexdigest(),
                  "revisions": {".": "a" * 40, "references/Hexalith.EventStore": "b" * 40},
                  "tests": [{"total": "1", "passed": "1", "failed": "0", "skipped": "0"}]}
        if scenario == "main":
            mode = report["runtimeModes"]["projects"]
            before = {"resourceName": "projects", "pid": mode["pid"], "startTicks": mode["startTicks"], "launcherPid": mode["wrapperPid"],
                      "launcherStartTicks": mode["wrapperStartTicks"], "assemblySha256": mode["assemblySha256"], "binarySha256": "e" * 64, "runtimeMode": json.loads(json.dumps(mode))}
            after = {**before, "pid": before["pid"] + 100, "startTicks": "999", "launcherPid": before["launcherPid"] + 100, "launcherStartTicks": "998"}
            after["runtimeMode"] = {**mode, "pid": after["pid"], "startTicks": after["startTicks"], "wrapperPid": after["launcherPid"], "wrapperStartTicks": after["launcherStartTicks"]}
            report["projectsRestart"] = {"resource": "projects", "action": "restart", "completed": True, "stage": "completed", "oldProcessExited": True,
                                          "endpointsRediscovered": True, "appHostPid": session["appHostPid"], "appHostStartTicks": session["appHostStartTicks"],
                                          "configurationSha256": session["configurationSha256"], "before": before, "after": after, "discovery": self.discovery()}
            if regressions:
                report["regressions"] = {lane: [{"total": "2", "passed": "2", "failed": "0", "skipped": "0"}] for lane in ["Integration", "Server"]}
        else:
            report["projectionReplay"] = LAUNCHER.require_distributed_replay_evidence(self.matrix(scenario), self.replay(scenario), scenario)
        return report

    def write_group(self, parent, scenario, frozen, regressions=False):
        directory = parent / scenario
        directory.mkdir(mode=0o700)
        report = self.report(scenario, frozen, regressions)
        report.update(nativeTestAssemblyPath=self.assembly_path(scenario), testAssemblySha256="e" * 64, testAssemblyUnchanged=True)
        report["nativeTestCommand"] = ["dotnet", report["nativeTestAssemblyPath"], "-method", LAUNCHER.SCENARIO_TEST_TYPE + "." + LAUNCHER.SCENARIO_METHODS[scenario], "-xml", str(directory / "distributed.xml")]
        report["commands"] = [report["nativeTestCommand"]]
        for name, value in [("run.json", report), ("matrix.json", self.matrix(scenario)), ("source-manifest.json", frozen)]:
            (directory / name).write_text(json.dumps(value))
        ET.ElementTree(self.native(scenario)).write(directory / "distributed.xml")
        fingerprints = [{"keySha256": hashlib.sha256(str(index).encode()).hexdigest().upper(), "bytesSha256": "A" * 64, "etagSha256": "B" * 64} for index in range(6)]
        (directory / "provider-observer-control.json").write_text(json.dumps({"verified": True, "binaryAndEscapingBoundariesExact": True, "domainKeyCount": 2,
                                                                           "controlKeyCount": 4, "individual": fingerprints, "batched": fingerprints}))
        if scenario != "main":
            (directory / "projection-replay.json").write_text(json.dumps(self.replay(scenario)))
        else:
            (directory / "projects-restart-process.json").write_text(json.dumps(report["projectsRestart"]))
            _, replay = SupportedReadReplayEvidenceTests().valid_evidence()
            state = next(row["comparison"] for row in replay["observations"] if row["kind"] == "state" and row["phase"] == "duplicate")
            restart_state = {"bothTenantsCompared": True, "domainKeysBytesEtagsAndWatermarksUnchanged": True, "domainKeyCount": len(state["before"]),
                             "afterObservation": "observed", "failureType": None, "missingKeys": [],
                             **{key: state[key] for key in ("before", "after", "beforeWatermarks", "afterWatermarks")}}
            (directory / "projects-restart-state.json").write_text(json.dumps(restart_state))
            if regressions:
                for lane in ["integration", "server"]:
                    ET.ElementTree(self.native(scenario, 2)).write(directory / (lane + ".xml"))
        return directory

    def test_v2_groups_keep_exact_actual_ledgers_without_borrowed_flags(self):
        for group, expected in [("duplicate", (8, 0, 0, 0, 2)), ("full-replay", (0, 8, 8, 4, 9))]:
            with self.subTest(group=group):
                result = LAUNCHER.require_distributed_replay_evidence(self.matrix(group), self.replay(group), group)
                self.assertEqual(tuple(result[key] for key in ["duplicateAttempts", "invalidReplayAttempts", "authenticationRefusals", "rebuildAttempts", "persistedComparisons"]), expected)

    def test_group_native_identity_counts_and_case_nodes_are_not_summaries(self):
        for mode in ["wrong-method", "wrong-type", "zero", "duplicate", "failed", "skipped", "not-run", "errors", "malformed", "missing-node"]:
            root = self.native("duplicate");assembly = root.find("assembly");test = root.find(".//test")
            if mode == "wrong-method": test.set("method", LAUNCHER.SCENARIO_METHODS["full-replay"])
            elif mode == "wrong-type": test.set("type", "Other.Tests")
            elif mode == "zero": assembly.set("total", "0");assembly.set("passed", "0")
            elif mode == "duplicate": assembly.find("collection").append(ET.fromstring(ET.tostring(test)))
            elif mode == "failed": test.set("result", "Fail")
            elif mode == "skipped": assembly.set("skipped", "1")
            elif mode == "not-run": assembly.set("not-run", "1")
            elif mode == "errors": assembly.set("errors", "1")
            elif mode == "malformed": assembly.set("total", "not-a-count")
            else: assembly.remove(assembly.find("collection"))
            with self.subTest(mode=mode), self.assertRaises(RuntimeError): LAUNCHER.require_native_scenario(root.findall("assembly"), "duplicate", self.provider_sha("duplicate"), self.assembly_path("duplicate"), "e" * 64)
        root = self.native("main", 2);root.find("assembly").remove(root.find("assembly/collection"))
        with self.assertRaises(RuntimeError): LAUNCHER.require_complete_native_results(root.findall("assembly"))

    def test_v2_missing_duplicate_foreign_or_reversed_records_never_qualify(self):
        for group in ["duplicate", "full-replay"]:
            for mode in ["missing-source", "duplicate-source", "reversed", "wrong-head", "wrong-status", "wrong-scope", "wrong-watermark", "domain-etag", "missing-truth", "borrowed-flag", "unknown-group"]:
                replay = self.replay(group);rows = replay["observations"];source = next(row for row in rows if row["kind"] == "source")
                if mode == "missing-source": rows.remove(source)
                elif mode == "duplicate-source": rows.insert(0, json.loads(json.dumps(source)))
                elif mode == "reversed": rows.reverse()
                elif mode == "wrong-head": source["observedHead"] += 1
                elif mode == "wrong-status": next(row for row in rows if row["kind"] in {"duplicate", "invalid"})["outcomes"][0]["status"] = "Completed"
                elif mode == "wrong-scope": next(row for row in rows if row["kind"] in {"duplicate", "invalid"})["tenantSha256"] = "F" * 64
                elif mode == "wrong-watermark": next(row for row in rows if row["kind"] == "state")["comparison"]["afterWatermarks"][0]["globalPosition"] += 1
                elif mode == "domain-etag": next(row for row in rows if row["kind"] == "state")["comparison"]["after"][0]["etagSha256"] = "F" * 64
                elif mode == "missing-truth": rows.remove(next(row for row in rows if row["kind"] == "reads"))
                elif mode == "borrowed-flag": replay["duplicateVerified" if group == "full-replay" else "replayVerified"] = True
                else: replay["qualificationGroup"] = "foreign"
                with self.subTest(group=group, mode=mode), self.assertRaises(RuntimeError): LAUNCHER.require_distributed_replay_evidence(self.matrix(group), replay, group)

    def test_complete_census_rejects_empty_partial_foreign_or_replaced_registry(self):
        valid = self.report("duplicate", {"source": "a" * 64})
        for mode in ["empty", "partial", "foreign-peer", "pid-duplicate", "start-missing", "namespace", "registry-flag", "mtls", "config", "binary", "registry-replaced", "unrecorded-restart", "zero-observations"]:
            report = json.loads(json.dumps(valid));after = report["discoveryAfterReads"]
            if mode == "empty": report["discoveryAfterReads"] = {}
            elif mode == "partial": after["ownedSidecars"].pop()
            elif mode == "foreign-peer": after["ownedSidecars"][0]["appId"] = "foreign"
            elif mode == "pid-duplicate": after["ownedSidecars"][0]["pid"] = after["ownedSidecars"][1]["pid"]
            elif mode == "start-missing": after["ownedSidecars"][0].pop("startTicks")
            elif mode in ["namespace", "registry-flag", "mtls"]: after["ownedSidecars"][0][{"namespace": "namespaceVerified", "registry-flag": "registryAddressVerified", "mtls": "mtlsIdentityVerified"}[mode]] = False
            elif mode == "config": after["configurationSha256"].pop("folders")
            elif mode == "binary": after["daprdSha256"] = []
            elif mode == "registry-replaced": after["registryIdentitySha256"] = "f" * 64
            elif mode == "unrecorded-restart": after["ownedSidecars"][0]["pid"] += 10000
            else: after["observationsRequired"] = 0
            with self.subTest(mode=mode), self.assertRaises(RuntimeError): LAUNCHER.require_owned_group_report(report, "duplicate")
        LAUNCHER.require_owned_group_report(valid, "duplicate")

    def test_canonical_mtls_names_join_three_distinct_non_provider_cleanup_ids(self):
        for mode in ["duplicate", "swapped", "missing-cleanup-name", "provider-as-mtls"]:
            report = self.report("duplicate", {"source": "a" * 64})
            rows = report["mtlsInfrastructure"]
            if mode == "duplicate": rows[1]["id"] = rows[0]["id"]
            elif mode == "swapped": rows[0]["id"], rows[1]["id"] = rows[1]["id"], rows[0]["id"]
            elif mode == "missing-cleanup-name": report["cleanup"][1]["resource"] = "foreign"
            else: rows[0]["id"] = report["cleanup"][-1]["containerId"]
            with self.subTest(mode=mode), self.assertRaises(RuntimeError): LAUNCHER.require_owned_group_report(report, "duplicate")

    def test_same_scenario_other_provider_or_assembly_artifacts_are_rejected(self):
        for mode in ["matrix", "replay", "native-provider", "native-path", "native-assembly", "native-missing-marker", "native-duplicate-marker", "command", "binary-changed"]:
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as directory:
                root = Path(directory);frozen = {"source": "a" * 64};group = self.write_group(root, "duplicate", frozen)
                if mode in ["matrix", "replay"]:
                    path = group / ("matrix.json" if mode == "matrix" else "projection-replay.json");value = json.loads(path.read_text());value["providerIsolationSha256"] = "F" * 64;path.write_text(json.dumps(value))
                elif mode.startswith("native"):
                    native = self.native("duplicate", provider_sha="F" * 64 if mode == "native-provider" else None, assembly_path="/tmp/other/Tests.dll" if mode == "native-path" else None, assembly_sha="f" * 64 if mode == "native-assembly" else None)
                    if mode == "native-missing-marker": native.find(".//test").remove(native.find(".//output"))
                    elif mode == "native-duplicate-marker": native.find(".//output").text *= 2
                    ET.ElementTree(native).write(group / "distributed.xml")
                else:
                    path = group / "run.json";value = json.loads(path.read_text())
                    if mode == "command": value["nativeTestCommand"][1] = "/tmp/other/Tests.dll"
                    else: value["testAssemblyUnchanged"] = False
                    path.write_text(json.dumps(value))
                with self.assertRaises(RuntimeError): LAUNCHER.read_qualified_scenario(group, "duplicate", frozen, False)

    def test_only_exact_owned_restart_census_can_change_a_sidecar_identity(self):
        report = self.report("main", {"source": "a" * 64});peer = next(row for row in report["discoveryAfterReads"]["ownedSidecars"] if row["appId"] == "projects")
        peer.update(pid=9999, startTicks="7777");report["projectsRestart"]["discovery"] = json.loads(json.dumps(report["discoveryAfterReads"]))
        LAUNCHER.require_owned_group_report(report, "main")
        for mode in ["receipt-missing", "foreign-session", "old-process", "changed-binary", "changed-option", "wrong-census"]:
            invalid = json.loads(json.dumps(report))
            if mode == "receipt-missing": invalid.pop("projectsRestart")
            elif mode == "foreign-session": invalid["projectsRestart"]["appHostPid"] += 1
            elif mode == "old-process": invalid["projectsRestart"]["after"] = invalid["projectsRestart"]["before"]
            elif mode == "changed-binary": invalid["projectsRestart"]["after"]["assemblySha256"] = "f" * 64
            elif mode == "changed-option": invalid["projectsRestart"]["after"]["runtimeMode"]["options"][next(iter(LAUNCHER.RUNTIME_FLAGS))] = "changed"
            else: invalid["projectsRestart"]["discovery"] = self.discovery()
            with self.subTest(mode=mode), self.assertRaises(RuntimeError): LAUNCHER.require_owned_group_report(invalid, "main")

    def test_owned_cleanup_mode_mapping_and_graph_are_all_required(self):
        valid = self.report("duplicate", {"source": "a" * 64})
        for mode in ["cleanup-missing", "container-missing", "recovery", "external", "private", "source-fallback", "optimized", "no-debug", "production", "wrong-peer-option", "mapping-missing", "mapping-hash"]:
            invalid = json.loads(json.dumps(valid))
            if mode == "cleanup-missing": invalid["cleanup"][0]["absenceVerified"] = False
            elif mode == "container-missing": invalid["cleanup"].pop()
            elif mode == "recovery": invalid["recoveryMaterialRetained"] = True
            elif mode == "external": invalid["preexistingResourcesPreserved"] = False
            elif mode == "private": invalid["privateMaterialRemoved"] = False
            elif mode == "source-fallback": next(iter(invalid["liveHostSourceReferences"].values()))["allSourceProjects"] = False
            elif mode == "optimized": next(iter(invalid["compilationProfile"].values()))["Optimize"] = "true"
            elif mode == "no-debug": next(iter(invalid["compilationProfile"].values()))["DefineConstants"] = "TRACE"
            elif mode == "production": invalid["runtimeModes"]["folders"]["environment"] = "Production"
            elif mode == "wrong-peer-option": invalid["runtimeModes"]["projects"]["options"]["EventStore__RouteCatalog__ReuseHeldMetadataDispatch"] = "true"
            elif mode == "mapping-missing": invalid["sourceMappingEnvironment"].pop("AfterMicrosoftNETSdkTargets")
            else: invalid["sourceMappingHash"] = "f" * 64
            with self.subTest(mode=mode), self.assertRaises(RuntimeError): LAUNCHER.require_owned_group_report(invalid, "duplicate")

    def test_aggregate_requires_three_fresh_exact_native_groups_and_current_graph(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);frozen = {"source": "a" * 64}
            groups = [LAUNCHER.read_qualified_scenario(self.write_group(root, group, frozen, group == "main"), group, frozen, group == "main") for group in LAUNCHER.SCENARIO_GROUPS]
            result = LAUNCHER.require_aggregate_scenarios(groups, True)
            self.assertEqual(result["cases"], list(LAUNCHER.MATRIX_CASES));self.assertEqual(len(result["groups"]), 3)
            self.assertNotEqual(self.report("duplicate", frozen)["sourceMappingHash"], self.report("full-replay", frozen)["sourceMappingHash"])
            for mode in ["missing", "duplicate", "reversed", "unknown", "shared-provider", "shared-process", "source", "graph", "mode", "no-regressions", "summary-only"]:
                changed = json.loads(json.dumps(groups))
                if mode == "missing": changed.pop()
                elif mode == "duplicate": changed[1] = changed[0]
                elif mode == "reversed": changed.reverse()
                elif mode == "unknown": changed[0]["group"] = "foreign"
                elif mode == "shared-provider": changed[1]["providerId"] = changed[0]["providerId"]
                elif mode == "shared-process": changed[1]["sessionIdentity"] = changed[0]["sessionIdentity"]
                elif mode == "source": changed[1]["sourceManifestSha256"] = "f" * 64
                elif mode == "graph": next(iter(changed[1]["sourceDependencyGraph"].values()))["FixtureDependency"] = str(ROOT / "src/other.csproj")
                elif mode == "mode": next(iter(changed[1]["compilationProfile"].values()))["DefineConstants"] += ";OTHER"
                elif mode == "no-regressions": changed[-1]["fullRegressionsCompleted"] = False
                else: changed[1]["native"] = [{"passed": "1"}]
                with self.subTest(mode=mode), self.assertRaises(RuntimeError): LAUNCHER.require_aggregate_scenarios(changed, True)

    def test_group_files_cannot_be_replaced_after_native_completion(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);frozen = {"source": "a" * 64};group = self.write_group(root, "duplicate", frozen)
            LAUNCHER.read_qualified_scenario(group, "duplicate", frozen, False)
            for mode in ["native", "source", "summary", "symlink"]:
                target = group / ("distributed.xml" if mode == "native" else "source-manifest.json" if mode == "source" else "run.json")
                original = target.read_bytes()
                try:
                    if mode == "native": ET.ElementTree(self.native("main")).write(target)
                    elif mode == "source": target.write_text(json.dumps({"other": "a" * 64}))
                    elif mode == "summary":
                        value = json.loads(original);value["tests"][0]["passed"] = "0";target.write_text(json.dumps(value))
                    else:
                        other = root / "replacement.json";other.write_bytes(original);target.unlink();target.symlink_to(other)
                    with self.subTest(mode=mode), self.assertRaises(RuntimeError): LAUNCHER.read_qualified_scenario(group, "duplicate", frozen, False)
                finally:
                    if target.is_symlink(): target.unlink()
                    target.write_bytes(original)

class SupportedReadScenarioCoordinatorTests(unittest.TestCase):
    def setUp(self):
        self.data = SupportedReadScenarioGroupTests()
        self.frozen = {"source": "a" * 64}

    def args(self):
        return SimpleNamespace(timeout=2, regressions=True, artifacts=None)

    def packets(self, root):
        for group in LAUNCHER.SCENARIO_GROUPS:
            self.data.write_group(root, group, self.frozen, group == "main")

    def child(self, code=0):
        child = Mock();child.pid = 12345;child.poll.return_value = code;child.wait.return_value = code
        return child

    def test_coordinator_runs_only_three_sequential_fresh_groups_and_main_regressions(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);self.packets(root);children = [self.child() for _ in LAUNCHER.SCENARIO_GROUPS]
            with patch.object(LAUNCHER, "run_launcher_controls", return_value={"passed": 1}), patch.object(LAUNCHER, "source_manifest", return_value=self.frozen), \
                 patch.object(LAUNCHER.subprocess, "Popen", side_effect=children) as spawn, \
                 patch.object(LAUNCHER, "capture_launcher_identity", side_effect=lambda child, command: {"pid": child.pid, "startTicks": "10", "command": command}), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(LAUNCHER.coordinate_scenarios(self.args(), root), 0)
            self.assertEqual(spawn.call_count, 3)
            self.assertTrue(all(call.kwargs["start_new_session"] for call in spawn.call_args_list))
            self.assertEqual([call.args[0][call.args[0].index("--scenario") + 1] for call in spawn.call_args_list], list(LAUNCHER.SCENARIO_GROUPS))
            self.assertEqual(["--regressions" in call.args[0] for call in spawn.call_args_list], [False, False, True])
            matrix = json.loads((root / "matrix.json").read_text());report = json.loads((root / "run.json").read_text())
            self.assertEqual(matrix["cases"], list(LAUNCHER.MATRIX_CASES));self.assertTrue(report["matrixCompleted"])
            self.assertTrue(all(row["complete"] and row["cleanupCompletionVerified"] for row in report["launches"]))

    def test_failed_child_with_preexisting_passing_files_never_starts_the_next_group(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);self.packets(root);child = self.child(1)
            with patch.object(LAUNCHER, "run_launcher_controls", return_value={"passed": 1}), patch.object(LAUNCHER, "source_manifest", return_value=self.frozen), \
                 patch.object(LAUNCHER.subprocess, "Popen", return_value=child) as spawn, patch.object(LAUNCHER, "capture_launcher_identity", return_value={"pid": child.pid, "startTicks": "10", "command": []}), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(LAUNCHER.coordinate_scenarios(self.args(), root), 1)
            self.assertEqual(spawn.call_count, 1);self.assertFalse((root / "matrix.json").exists())
            report = json.loads((root / "run.json").read_text());self.assertEqual(report["groups"], [])
            self.assertEqual(report["launches"][0]["exitCode"], 1)

    def test_source_drift_after_one_passed_group_blocks_every_later_start(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);self.packets(root);child = self.child()
            with patch.object(LAUNCHER, "run_launcher_controls", return_value={"passed": 1}), patch.object(LAUNCHER, "source_manifest", side_effect=[self.frozen, self.frozen, {"changed": "b" * 64}]), \
                 patch.object(LAUNCHER.subprocess, "Popen", return_value=child) as spawn, patch.object(LAUNCHER, "capture_launcher_identity", return_value={"pid": child.pid, "startTicks": "10", "command": []}), contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(LAUNCHER.coordinate_scenarios(self.args(), root), 1)
            self.assertEqual(spawn.call_count, 1);self.assertFalse((root / "matrix.json").exists())
            self.assertEqual(len(json.loads((root / "run.json").read_text())["groups"]), 1)

    def spawn_dummy(self, directory):
        script = textwrap.dedent('''
            from pathlib import Path
            import signal,sys,time,json
            root=Path(sys.argv[1]);root.mkdir(exist_ok=True)
            def close(signum,_frame):
                (root/'cleaning').write_text(str(signum));time.sleep(.1)
                (root/'run.json').write_text(json.dumps({'privateMaterialRemoved':True,'ownedSidecarsAbsent':True,'preexistingResourcesPreserved':True,'cleanup':[{'absenceVerified':True}]}))
                (root/'cleaned').write_text('done');raise SystemExit(1)
            signal.signal(signal.SIGTERM,close);signal.signal(signal.SIGINT,close)
            (root/'ready').write_text('ready')
            while True:time.sleep(.02)
        ''')
        command = [sys.executable, "-u", "-c", script, str(directory)]
        child = subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, start_new_session=True)
        identity = LAUNCHER.capture_launcher_identity(child, command)
        deadline = time.monotonic() + 3
        while not (directory / "ready").exists() and time.monotonic() < deadline: time.sleep(.01)
        self.assertTrue((directory / "ready").exists())
        return child, identity

    def finish_dummy(self, child):
        if child.poll() is None: child.kill()
        child.wait(timeout=3)

    def test_exact_interruption_waits_for_real_owned_child_cleanup(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);child, identity = self.spawn_dummy(root)
            try:
                self.assertEqual(os.getsid(child.pid), child.pid)
                start = time.monotonic();LAUNCHER.forward_owned_launcher_signal(child, identity, signal.SIGTERM)
                self.assertEqual(LAUNCHER.wait_owned_launcher(child, identity, {"requested": True}, cleanup_wait_seconds=2), 1)
                self.assertGreaterEqual(time.monotonic() - start, .08);self.assertTrue((root / "cleaned").exists())
            finally: self.finish_dummy(child)

    def test_foreign_or_recycled_identity_is_never_signaled(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);child, identity = self.spawn_dummy(root)
            try:
                for changed in [{**identity, "startTicks": "other"}, {**identity, "command": ["foreign"]}, {**identity, "pid": child.pid + 1}]:
                    with self.assertRaises(RuntimeError): LAUNCHER.forward_owned_launcher_signal(child, changed, signal.SIGTERM)
                self.assertIsNone(child.poll());self.assertFalse((root / "cleaning").exists())
            finally: self.finish_dummy(child)

    def test_normal_exit_between_poll_and_identity_check_is_completed_not_unknown(self):
        child = Mock();child.poll.side_effect = [None, 0];child.wait.return_value = 0
        with patch.object(LAUNCHER, "owned_launcher_identity_matches", return_value=False):
            self.assertEqual(LAUNCHER.wait_owned_launcher(child, {}, {}), 0)
        child.send_signal.assert_not_called()

    def test_only_exact_kernel_exit_with_empty_command_line_is_observed(self):
        child = Mock(pid=123, args=["owned"])
        identity = {"pid": 123, "startTicks": "10", "command": ["owned"]}
        def snapshot(flags=4, started="10"):
            return "123 (owned child) " + " ".join(["R", "0", "0", "0", "0", "0", str(flags), *(["0"] * 12), started])
        for name, changed, uid, command, snapshots in [
                ("exact exit", identity, os.getuid(), b"", [snapshot(), snapshot()]),
                ("foreign pid", {**identity, "pid": 124}, os.getuid(), b"", [snapshot(), snapshot()]),
                ("changed capture command", {**identity, "command": ["foreign"]}, os.getuid(), b"", [snapshot(), snapshot()]),
                ("foreign owner", identity, os.getuid() + 1, b"", [snapshot(), snapshot()]),
                ("live empty argv", identity, os.getuid(), b"", [snapshot(0), snapshot(0)]),
                ("missing initial exit flag", identity, os.getuid(), b"", [snapshot(0), snapshot()]),
                ("missing final exit flag", identity, os.getuid(), b"", [snapshot(), snapshot(0)]),
                ("nonempty argv", identity, os.getuid(), b"foreign\0", [snapshot(), snapshot()]),
                ("different initial start", identity, os.getuid(), b"", [snapshot(started="11"), snapshot()]),
                ("recycled final start", identity, os.getuid(), b"", [snapshot(), snapshot(started="11")]),
                ("malformed proc stat", identity, os.getuid(), b"", ["malformed", snapshot()])]:
            with self.subTest(name=name):
                process = Mock();process.stat.return_value = SimpleNamespace(st_uid=uid)
                status = Mock();status.read_text.side_effect = snapshots
                arguments = Mock();arguments.read_bytes.return_value = command
                process.joinpath.side_effect = lambda part: status if part == "stat" else arguments
                with patch.object(LAUNCHER, "Path", return_value=process):
                    self.assertEqual(LAUNCHER.owned_launcher_exit_started(child, changed), name == "exact exit")
        for error in (FileNotFoundError(), PermissionError()):
            with patch.object(LAUNCHER, "Path", side_effect=error):
                self.assertFalse(LAUNCHER.owned_launcher_exit_started(child, identity))

    def test_kernel_exit_waits_for_actual_child_exit_without_signaling(self):
        for exit_code in (0, 1, 7):
            with self.subTest(exit_code=exit_code):
                child = Mock();child.poll.side_effect = [None, None, exit_code];child.wait.return_value = exit_code
                with patch.object(LAUNCHER, "owned_launcher_identity_matches", return_value=False), \
                     patch.object(LAUNCHER, "owned_launcher_exit_started", return_value=True) as observed, patch.object(LAUNCHER.time, "sleep"):
                    self.assertEqual(LAUNCHER.wait_owned_launcher(child, {}, {}), exit_code)
                observed.assert_called_once();child.wait.assert_called_once();child.send_signal.assert_not_called()

    def test_exit_finishing_during_kernel_observation_is_reaped(self):
        child = Mock();child.poll.side_effect = [None, None, 0];child.wait.return_value = 0
        with patch.object(LAUNCHER, "owned_launcher_identity_matches", return_value=False), \
             patch.object(LAUNCHER, "owned_launcher_exit_started", return_value=False):
            self.assertEqual(LAUNCHER.wait_owned_launcher(child, {}, {}), 0)
        child.wait.assert_called_once();child.send_signal.assert_not_called()

    def test_kernel_exit_observation_never_authorizes_an_unknown_child_signal(self):
        child = Mock();child.poll.return_value = None
        with patch.object(LAUNCHER, "owned_launcher_identity_matches", return_value=False), \
             patch.object(LAUNCHER, "owned_launcher_exit_started", return_value=True) as observed:
            with self.assertRaises(RuntimeError): LAUNCHER.forward_owned_launcher_signal(child, {}, signal.SIGTERM)
        observed.assert_not_called();child.send_signal.assert_not_called()

    def test_wait_refuses_live_foreign_recycled_or_changed_identity(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);child, identity = self.spawn_dummy(root)
            try:
                for changed in [{**identity, "startTicks": "other"}, {**identity, "command": ["foreign"]}, {**identity, "pid": child.pid + 1}]:
                    with self.assertRaises(RuntimeError): LAUNCHER.wait_owned_launcher(child, changed, {})
                self.assertIsNone(child.poll());self.assertFalse((root / "cleaning").exists())
            finally: self.finish_dummy(child)

    def test_unfinished_kernel_exit_still_has_a_finite_cleanup_bound(self):
        child = Mock();child.poll.return_value = None
        with patch.object(LAUNCHER, "owned_launcher_identity_matches", return_value=False), \
             patch.object(LAUNCHER, "owned_launcher_exit_started", return_value=True), patch.object(LAUNCHER.time, "monotonic", side_effect=[1, 1, 3]), \
             patch.object(LAUNCHER.time, "sleep"):
            with self.assertRaises(RuntimeError): LAUNCHER.wait_owned_launcher(child, {}, {}, cleanup_wait_seconds=1)
        child.wait.assert_not_called();child.send_signal.assert_not_called()

    def test_unproved_cleanup_timeout_preserves_material_and_does_not_kill_child(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);(root / "retained-material").write_text("owned-test-material");child, identity = self.spawn_dummy(root)
            try:
                with self.assertRaises(RuntimeError): LAUNCHER.wait_owned_launcher(child, identity, {"requested": True}, cleanup_wait_seconds=.02)
                self.assertIsNone(child.poll());self.assertTrue((root / "retained-material").exists());self.assertFalse((root / "cleaning").exists())
            finally: self.finish_dummy(child)

    def test_known_coordinator_exception_interrupts_exact_child_and_awaits_finally(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);childdir = root / "duplicate";childdir.mkdir();child, identity = self.spawn_dummy(childdir)
            original = Path.write_text;failed = False
            def writing(path, *args, **kwargs):
                nonlocal failed
                if path.name == "active-group.json" and not failed:
                    failed = True;raise OSError("injected coordinator receipt failure")
                return original(path, *args, **kwargs)
            try:
                with patch.object(LAUNCHER, "run_launcher_controls", return_value={"passed": 1}), patch.object(LAUNCHER, "source_manifest", return_value=self.frozen), \
                     patch.object(LAUNCHER.subprocess, "Popen", return_value=child) as spawn, patch.object(LAUNCHER, "capture_launcher_identity", return_value=identity), patch.object(Path, "write_text", writing), contextlib.redirect_stdout(io.StringIO()):
                    self.assertEqual(LAUNCHER.coordinate_scenarios(self.args(), root), 1)
                self.assertEqual(spawn.call_count, 1);self.assertTrue((childdir / "cleaned").exists())
                report = json.loads((root / "run.json").read_text());self.assertTrue(report["launches"][0]["cleanupCompletionVerified"])
                self.assertFalse(report.get("activeChildCompletionUnproved", False));self.assertFalse((root / "matrix.json").exists())
            finally: self.finish_dummy(child)

    def test_unknown_active_child_is_retained_without_adoption_or_later_start(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory);childdir = root / "duplicate";childdir.mkdir();child, identity = self.spawn_dummy(childdir)
            try:
                with patch.object(LAUNCHER, "run_launcher_controls", return_value={"passed": 1}), patch.object(LAUNCHER, "source_manifest", return_value=self.frozen), \
                     patch.object(LAUNCHER.subprocess, "Popen", return_value=child) as spawn, patch.object(LAUNCHER, "capture_launcher_identity", side_effect=RuntimeError("identity unknown")), contextlib.redirect_stdout(io.StringIO()):
                    self.assertEqual(LAUNCHER.coordinate_scenarios(self.args(), root), 1)
                self.assertEqual(spawn.call_count, 1);self.assertIsNone(child.poll());self.assertFalse((childdir / "cleaning").exists())
                report = json.loads((root / "run.json").read_text());self.assertTrue(report["activeChildCompletionUnproved"]);self.assertTrue(report["recoveryMaterialRetainedByChild"])
                self.assertTrue(json.loads((root / "active-group.json").read_text())["active"])
            finally: self.finish_dummy(child)



class BrowserReceiptControls(unittest.TestCase):
    def setUp(self):
        spec = importlib.util.spec_from_file_location("supported_browser_reads", ROOT / "tools/qualification/supported_browser_reads.py")
        self.browser = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.browser)

    def receipt(self, root, **overrides):
        attributes = dict(name="owned-tests.dll", total="1", passed="1", failed="0", skipped="0", errors="0", **{"not-run": "0"})
        attributes.update(overrides)
        document = ET.Element("assemblies")
        assembly = ET.SubElement(document, "assembly", attributes)
        case = ET.SubElement(ET.SubElement(assembly, "collection"), "test", dict(result="Pass",
            type="Hexalith.Projects.Integration.Tests.SupportedProjectBrowserSeedTests", method="SeedRealOidcBrowserFixture"))
        path = root / "seed.xml"
        ET.ElementTree(document).write(path)
        return path, document, case

    def test_exact_seed_case_is_required_and_fingerprinted(self):
        with tempfile.TemporaryDirectory() as temporary:
            path, _, _ = self.receipt(Path(temporary))
            result = self.browser.require_seed_receipt(path, "owned-tests.dll")
            self.assertEqual(result["passed"], 1)
            self.assertEqual(result["xmlSha256"], hashlib.sha256(path.read_bytes()).hexdigest())

    def test_empty_failed_skipped_error_and_unrun_seed_receipts_refuse(self):
        for override in [dict(total="0", passed="0"), dict(failed="1"), dict(skipped="1"), dict(errors="1"), {"not-run": "1"}]:
            with self.subTest(override=override), tempfile.TemporaryDirectory() as temporary:
                path, _, _ = self.receipt(Path(temporary), **override)
                with self.assertRaises(RuntimeError): self.browser.require_seed_receipt(path, "owned-tests.dll")

    def test_wrong_case_assembly_or_missing_case_refuses(self):
        for kind in ("method", "type", "result", "assembly", "missing", "duplicate"):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as temporary:
                path, document, case = self.receipt(Path(temporary))
                if kind in ("method", "type", "result"): case.set(kind, "unexpected")
                elif kind == "assembly": document.find("assembly").set("name", "other.dll")
                elif kind == "missing": document.find("assembly/collection").remove(case)
                else: document.find("assembly/collection").append(ET.Element("test", dict(case.attrib)))
                ET.ElementTree(document).write(path)
                with self.assertRaises(RuntimeError): self.browser.require_seed_receipt(path, "owned-tests.dll")

    def test_missing_and_malformed_xml_refuses(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "missing.xml"
            with self.assertRaises(RuntimeError): self.browser.require_seed_receipt(path, "owned-tests.dll")
            path.write_text("not xml")
            with self.assertRaises(RuntimeError): self.browser.require_seed_receipt(path, "owned-tests.dll")

    def test_command_refusal_retains_only_closed_reason_and_hashes(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "private.json"
            path.write_text(json.dumps(dict(status=403, reasonCode="insufficient_permission",
                reason="Not authorized for protected-tenant. The authenticated surface cannot invoke this operation.", token="protected-token")))
            result = self.browser.safe_command_refusal(path)
            self.assertEqual(result["closedReason"], "authenticated-surface-refused")
            self.assertEqual(result["reasonCode"], "insufficient_permission")
            self.assertNotIn("protected-", json.dumps(result))
            path.write_text(json.dumps(dict(reasonCode="protected-arbitrary-code", detail="protected-name")))
            self.assertNotIn("reasonCode", self.browser.safe_command_refusal(path))

    def test_manual_hold_refuses_missing_shared_or_reused_directories_before_writing_access(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            with self.assertRaises(RuntimeError): self.browser.manual_hold(root / "missing", {"password": "private-password"}, seconds=1)
            shared = root / "shared"; shared.mkdir(mode=0o755); os.chmod(shared, 0o755)
            with self.assertRaises(RuntimeError): self.browser.manual_hold(shared, {"password": "private-password"}, seconds=1)
            self.assertFalse((shared / "access.json").exists())
            reused = root / "reused"; reused.mkdir(mode=0o700); (reused / "release").touch()
            with self.assertRaises(RuntimeError): self.browser.manual_hold(reused, {"password": "private-password"}, seconds=1)
            self.assertFalse((reused / "access.json").exists())
            target = root / "target"; target.mkdir(mode=0o700)
            link = root / "link"; link.symlink_to(target, target_is_directory=True)
            with self.assertRaises(RuntimeError): self.browser.manual_hold(link, {"password": "private-password"}, seconds=1)
            self.assertFalse((target / "access.json").exists())

    def test_manual_hold_access_is_private_and_removed_after_release(self):
        with tempfile.TemporaryDirectory() as temporary:
            hold = Path(temporary) / "hold"; hold.mkdir(mode=0o700)
            now = [0.0]; observed = {}
            def sleep(seconds):
                access = hold / "access.json"
                observed["mode"] = access.stat().st_mode & 0o777
                observed["access"] = json.loads(access.read_text())
                now[0] += seconds
                (hold / "release").touch()
            receipt = self.browser.manual_hold(hold, {"password": "private-password", "username": "actor-1"}, seconds=60, poll=5, clock=lambda: now[0], sleep=sleep)
            self.assertEqual(observed["mode"], 0o600)
            self.assertEqual(observed["access"]["password"], "private-password")
            self.assertTrue(receipt["released"]); self.assertTrue(receipt["accessRemoved"]); self.assertFalse(receipt["countedAsAutomatedProof"])
            self.assertNotIn("private-password", json.dumps(receipt))
            self.assertEqual(sorted(path.name for path in hold.iterdir()), [])

    def test_manual_hold_limit_releases_and_removes_access_without_a_person(self):
        with tempfile.TemporaryDirectory() as temporary:
            hold = Path(temporary) / "hold"; hold.mkdir(mode=0o700)
            now = [0.0]
            def sleep(seconds): now[0] += seconds
            receipt = self.browser.manual_hold(hold, {"password": "private-password"}, seconds=12, poll=5, clock=lambda: now[0], sleep=sleep)
            self.assertFalse(receipt["released"]); self.assertTrue(receipt["accessRemoved"])
            self.assertGreaterEqual(receipt["heldSeconds"], 12)
            self.assertFalse((hold / "access.json").exists())

    def test_browser_regressions_combination_refuses_before_allocation(self):
        with tempfile.TemporaryDirectory() as temporary:
            evidence = Path(temporary) / "unallocated"
            with patch.object(sys, "argv", ["launcher", "--browser", "--regressions", "--evidence", str(evidence)]), contextlib.redirect_stderr(io.StringIO()):
                with self.assertRaises(SystemExit) as failure: LAUNCHER.main()
            self.assertEqual(failure.exception.code, 2)
            self.assertFalse(evidence.exists())


class BrowserReviewCorrectionsTests(unittest.TestCase):
    def setUp(self):
        spec = importlib.util.spec_from_file_location("supported_browser_reads", ROOT / "tools/qualification/supported_browser_reads.py")
        self.browser = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.browser)

    def test_post_seed_readiness_token_is_private_and_in_every_output_protection_collection(self):
        fixture = {"Browser": {key: key + "-credential" for key in ("Password", "ClientSecret", "TenantToken", "ForeignToken")}}
        with tempfile.TemporaryDirectory() as temporary:
            private = Path(temporary)
            fresh = "eyJabcdefgh.freshreadtoken.abcdefgh"
            path = private / "browser-readiness-credential.json"
            path.write_text(json.dumps({"accessToken": fresh, "acquiredAfterSeeding": True}))
            path.chmod(0o600)
            values = self.browser.protected_browser_values(fixture, private)
            self.assertIn(fresh, values)
            self.assertTrue(set(self.browser.PROTECTED_FIXTURE_MARKERS).issubset(values))
            self.assertNotIn(fresh, self.browser.sanitize_output("seed failure " + fresh, values))
            observation = self.browser._LogObservation(values)
            observation.accept(fresh.encode())
            self.assertFalse(observation.receipt()["protectedMarkerAbsent"])
            path.chmod(0o644)
            with self.assertRaises(RuntimeError):
                self.browser.protected_browser_values(fixture, private)

    def test_wrong_issuer_discovery_uses_delay_and_existing_deadline(self):
        now = [0.0]
        calls, sleeps = [], []
        def open_url(*args, **kwargs):
            calls.append(args[0])
            return io.StringIO(json.dumps({"issuer": "https://another-issuer.invalid"}))
        def sleep(seconds):
            sleeps.append(seconds)
            now[0] += seconds
        with self.assertRaisesRegex(RuntimeError, "OIDC readiness"):
            self.browser.wait_for_oidc("https://expected.invalid", None, seconds=.5,
                clock=lambda: now[0], sleep=sleep, opener=open_url)
        self.assertEqual(len(calls), 3)
        self.assertEqual(sleeps, [.25, .25])

    def test_correct_discovery_finishes_without_polling_delay(self):
        self.browser.wait_for_oidc("https://expected.invalid", None,
            opener=lambda *args, **kwargs: io.StringIO('{"issuer":"https://expected.invalid"}'),
            sleep=lambda _: self.fail("A matching issuer must finish"))

    def test_retained_assertion_output_redacts_every_fixture_marker_and_arbitrary_jwt(self):
        markers = self.browser.PROTECTED_FIXTURE_MARKERS
        expected = {"Synthetic project project-a", "Synthetic project project-b", "Foreign synthetic name",
                    "Owned folder label", "Legacy project legacy-a", "Legacy project legacy-b",
                    "Synthetic descriptive metadata", "Restricted legacy description", "Restricted legacy setup reference"}
        self.assertEqual(set(markers), expected)
        token = "eyJabcdefgh." + "A" * 100000 + ".abcdefgh"
        raw = "<failure>" + "\n".join(markers) + " private-password " + token + "</failure>"
        sanitized = self.browser.sanitize_output(raw, [*markers, "private-password"])
        for marker in [*markers, "private-password", token]:
            self.assertNotIn(marker, sanitized)
        self.assertIn("[redacted-jwt]", sanitized)
        ET.fromstring(sanitized)
        with tempfile.TemporaryDirectory() as temporary:
            for name in ("browser.log", "browser.xml", "browser-seed.log", "browser-seed.xml"):
                path = Path(temporary) / name
                self.browser.write_sanitized_output(path, raw, [*markers, "private-password"])
                self.assertEqual(path.read_text(), sanitized)

    def test_standard_keycloak_scopes_keep_real_identity_and_role_mappers(self):
        realm = json.loads((ROOT / "src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json").read_text())
        scopes = {scope["name"]: scope for scope in realm["clientScopes"]}
        profile = {mapper["name"]: mapper for mapper in scopes["profile"]["protocolMappers"]}
        self.assertEqual(profile["username"]["config"]["claim.name"], "preferred_username")
        self.assertEqual(profile["given name"]["config"]["claim.name"], "given_name")
        self.assertEqual(profile["family name"]["config"]["claim.name"], "family_name")
        self.assertEqual(profile["full name"]["protocolMapper"], "oidc-full-name-mapper")
        self.assertEqual(profile["full name"]["config"]["id.token.claim"], "true")
        self.assertEqual(len(scopes["email"]["protocolMappers"]), 2)
        self.assertEqual({m["protocolMapper"] for m in scopes["roles"]["protocolMappers"]},
            {"oidc-usermodel-realm-role-mapper", "oidc-usermodel-client-role-mapper", "oidc-audience-resolve-mapper"})
        for name in ("basic", "web-origins", "acr"):
            self.assertTrue(scopes[name]["protocolMappers"])

    def test_supported_only_exec_removes_only_its_two_owned_temporary_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            binaries, allocations = root / "bin", root / "allocations"
            binaries.mkdir(); allocations.mkdir()
            for name in ("aspire", "jq", "npm", "npx", "python3"):
                path = binaries / name
                path.write_text("#!/bin/sh\nexit 0\n"); path.chmod(0o700)
            unrelated = allocations / "pre-existing"; unrelated.write_text("preserve")
            env = dict(os.environ, PATH=str(binaries) + ":" + os.environ["PATH"],
                TMPDIR=str(allocations), E2E_SUPPORTED_READS_ONLY="1")
            result = subprocess.run(["bash", str(ROOT / "tests/e2e/run-live-apphost.sh")],
                env=env, capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(list(allocations.iterdir()), [unrelated])
            self.assertEqual(unrelated.read_text(), "preserve")


class BrowserTelemetryCoverageTests(unittest.TestCase):
    def setUp(self):
        spec = importlib.util.spec_from_file_location('supported_browser_reads', ROOT / 'tools/qualification/supported_browser_reads.py')
        self.browser = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.browser)

    def test_protected_marker_split_across_chunks_is_detected_without_retaining_text(self):
        observation = self.browser._LogObservation(['private-client-secret'])
        observation.accept(b'{"resourceName":"ui","content":"private-client-')
        observation.accept(b'secret"}')
        receipt = observation.receipt()
        self.assertFalse(receipt['protectedMarkerAbsent'])
        self.assertNotIn('private-client-secret', json.dumps(receipt))
        self.assertEqual(receipt['logEntries'], 1)

    def test_generic_jwt_detection_survives_chunks_and_long_payload_without_token_storage(self):
        observation = self.browser._LogObservation(['known-marker'])
        observation.accept(b'eyJabcdefgh.')
        observation.accept(b'A' * 100000)
        observation.accept(b'.abcdefgh')
        self.assertFalse(observation.receipt()['jwtShapedCredentialAbsent'])
        self.assertTrue(observation.receipt()['protectedMarkerAbsent'])

    def test_ordinary_stream_hash_and_entry_count_survive_split_json_keys(self):
        observation = self.browser._LogObservation(['private-marker'])
        chunks = [b'{"resource', b'Name":"projects","content":"Current"}\n', b'{"resourceName":"ui","content":"Unavailable"}\n']
        for chunk in chunks: observation.accept(chunk)
        receipt = observation.receipt()
        self.assertEqual(receipt['sha256'], hashlib.sha256(b''.join(chunks)).hexdigest())
        self.assertEqual(receipt['logEntries'], 2)
        self.assertEqual(receipt['capturedBytes'], sum(map(len, chunks)))
        self.assertTrue(receipt['protectedMarkerAbsent'])
        self.assertTrue(receipt['jwtShapedCredentialAbsent'])

    def test_owned_reader_groups_and_descendants_are_removed_and_stderr_is_scanned(self):
        actual_popen = subprocess.Popen
        child = "import subprocess,sys,time; print('{\"resourceName\":\"owned-test\",\"content\":\"safe\"}',flush=True); sys.stderr.write('private-marker'); sys.stderr.flush(); subprocess.Popen([sys.executable,'-c','import time; time.sleep(30)']); time.sleep(30)"
        def reader_process(command, **options):
            self.assertTrue(options['start_new_session'])
            return actual_popen([sys.executable, '-c', child], **options)
        monitor = self.browser._BrowserTelemetry(ROOT, ['private-marker'], LAUNCHER.test_group_members)
        interval = {}
        try:
            with patch.object(subprocess, 'Popen', side_effect=reader_process): monitor.start()
            interval['startedAtUtc'] = monitor.now()
            interval['endedAtUtc'] = monitor.now()
        finally:
            receipt = monitor.finish(interval)
        self.assertTrue(receipt['completeIntervalObserved'])
        self.assertFalse(receipt['protectedMarkerAbsent'])
        for value in receipt['resources'].values():
            self.assertTrue(value['cleanup']['descendantsAbsent'])
            self.assertFalse(value['stderr']['protectedMarkerAbsent'])
            self.assertFalse(LAUNCHER.test_group_members(value['cleanup']['groupId']))

    def test_missing_streams_cannot_claim_browser_interval_coverage(self):
        monitor = self.browser._BrowserTelemetry(ROOT, ['private-marker'], lambda group: {})
        with patch.object(self.browser.time, 'sleep'):
            receipt = monitor.finish({'startedAtUtc': '2026-11-06T00:00:00+00:00', 'endedAtUtc': '2026-11-06T00:00:01+00:00'})
        self.assertFalse(receipt['completeIntervalObserved'])


if __name__ == "__main__":
    unittest.main()
