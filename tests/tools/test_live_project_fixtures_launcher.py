"""Fail-closed observations for the isolated compatibility runner."""
import importlib.util
import base64
import json
import os
from pathlib import Path
import sys
import subprocess
import tempfile
import unittest
from unittest.mock import patch

TOOLS = Path(__file__).resolve().parents[2] / "tools/qualification"
sys.path.insert(0, str(TOOLS))
SPEC = importlib.util.spec_from_file_location("live_project_fixtures", TOOLS / "run_live_project_fixtures.py")
LAUNCHER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(LAUNCHER)


class DisabledRuntimeObservationTests(unittest.TestCase):
    def test_missing_runtime_process_shape_cannot_establish_disabled_profile(self):
        graph = {"resources": [{"displayName": "projects", "properties": {}}]}
        with patch.object(LAUNCHER.support, "owns_current_apphost", return_value=True):
            with self.assertRaisesRegex(RuntimeError, "launcher is missing"):
                LAUNCHER.disabled_process_bindings(graph, {"_isolation": "fixtures-owned"})

    def test_replaced_apphost_cannot_establish_disabled_profile(self):
        with patch.object(LAUNCHER.support, "owns_current_apphost", return_value=False):
            with self.assertRaisesRegex(RuntimeError, "ownership changed"):
                LAUNCHER.disabled_process_bindings({"resources": []}, {})

    def test_ordinary_launcher_without_actual_projects_process_cannot_pass(self):
        graph = {"resources": [{"displayName": "projects", "properties": {"executable.pid": os.getpid()}}]}
        with patch.object(LAUNCHER.support, "owns_current_apphost", return_value=True), \
                patch.object(LAUNCHER.support, "helper_descendants", return_value={}):
            with self.assertRaisesRegex(RuntimeError, "missing or ambiguous"):
                LAUNCHER.disabled_process_bindings(graph, {"_isolation": "fixtures-owned"})


class DiagnosticObservationTests(unittest.TestCase):
    def test_span_observation_keeps_actual_timing_and_discards_header_body_and_query_values(self):
        raw = json.dumps({"spans": [{"traceId": "a" * 32, "spanId": "b" * 16, "parentSpanId": "c" * 16,
            "name": "POST /api/v1/projects/proposals/confirm", "durationMs": 17600,
            "attributes": {"http.request.method": "POST", "http.response.status_code": 202,
                "url.full": "http://localhost/api/v1/projects/proposals/confirm?token=private-query",
                "Authorization": "private-authorization", "body": "private-body"}}]})
        value = LAUNCHER.safe_span_observations(raw)
        self.assertEqual(value[0]["durationms"], 17600)
        self.assertEqual(value[0]["operation"], "proposal-confirm")
        self.assertEqual(value[0]["httpStatus"], 202)
        self.assertNotIn("private-", json.dumps(value))

    def test_malformed_span_identity_cannot_establish_observed_timing(self):
        self.assertEqual(LAUNCHER.safe_span_observations('{"spans":[{"spanId":"private-invalid","durationMs":10}]}'), [])

    def test_redis_json_observer_refuses_mutations_before_dispatch(self):
        with self.assertRaisesRegex(RuntimeError, "only KEYS and HMGET"):
            LAUNCHER.redis_json("owned", ["SET", "projection", "data"], lambda _: self.fail("A write was dispatched"))


class PhaseEvidenceVerificationTests(unittest.TestCase):
    def verify(self, output, *, exit_code=0, tests=2, failures=0, skipped=0, cases=2, extra=""):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "phase.xml"
            path.write_text(f'<testsuites tests="{tests}" failures="{failures}" errors="0" skipped="{skipped}">'
                + ''.join('<testcase name="case">' + extra + '</testcase>' for _ in range(cases)) + '</testsuites>')
            return LAUNCHER.verify_phase_result(exit_code, output, path)

    def test_empty_nested_self_probe_does_not_replace_final_outer_evidence(self):
        value = self.verify('[zero-live-skip] collected=0 skipped=0 status=passed\n[zero-live-skip] collected=2 skipped=0 status=passed\n')
        self.assertEqual(value["collected"], 2)

    def test_empty_final_outer_collection_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, "phase"):
            self.verify('[zero-live-skip] collected=0 skipped=0 status=passed\n', tests=0, cases=0)

    def test_missing_final_reporter_observation_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, "phase"):
            self.verify('2 passed\n')

    def test_reporter_and_actual_case_count_must_match(self):
        with self.assertRaisesRegex(RuntimeError, "phase"):
            self.verify('[zero-live-skip] collected=3 skipped=0 status=passed\n')

    def test_nonzero_process_exit_cannot_pass_on_reporter_text(self):
        with self.assertRaisesRegex(RuntimeError, "phase"):
            self.verify('[zero-live-skip] collected=2 skipped=0 status=passed\n', exit_code=1)

    def test_actual_failure_or_skip_cannot_pass_a_success_summary(self):
        for values in ({"failures": 1}, {"skipped": 1}, {"extra": '<failure message="failed"/>'}):
            with self.subTest(values=values), self.assertRaisesRegex(RuntimeError, "phase"):
                self.verify('[zero-live-skip] collected=2 skipped=0 status=passed\n', **values)

    def test_planned_flaky_attempt_is_separate_from_final_junit_failure(self):
        value = self.verify('[zero-live-skip] collected=2 skipped=0 status=passed\n', extra='<flakyFailure message="deliberate retry probe"/>')
        self.assertTrue(value["zeroSkips"])

    def test_malformed_junit_cannot_be_treated_as_an_empty_success(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "phase.xml"
            path.write_text('<testsuites')
            with self.assertRaisesRegex(RuntimeError, "phase"):
                LAUNCHER.verify_phase_result(0, '[zero-live-skip] collected=2 skipped=0 status=passed\n', path)


class PersistedCleanupObservationTests(unittest.TestCase):
    def document(self, *, restore=False, non_lifecycle=False):
        events = []
        for project in ("project-one", "project-two"):
            for event, lifecycle in (("ProjectCreated", "Active"), ("ProjectArchived", "Archived")):
                payload = json.dumps({"tenantId": "fixture-tenant", "projectId": project, "lifecycle": lifecycle}).encode()
                events.append({"tenantId": "fixture-tenant", "eventTypeName": LAUNCHER.PROJECT_EVENT_PREFIX + event,
                    "payloadBase64": base64.b64encode(payload).decode()})
                if non_lifecycle and project == "project-one" and event == "ProjectCreated":
                    events.append({"tenantId": "fixture-tenant", "eventTypeName": LAUNCHER.PROJECT_EVENT_PREFIX + "ProjectSetupUpdated",
                        "payloadBase64": base64.b64encode(json.dumps({"tenantId": "fixture-tenant", "projectId": project}).encode()).decode()})
        if restore:
            events.append({"tenantId": "fixture-tenant", "eventTypeName": LAUNCHER.PROJECT_EVENT_PREFIX + "ProjectRestored", "payloadBase64": base64.b64encode(
                json.dumps({"tenantId": "fixture-tenant", "projectId": "project-one", "lifecycle": "Active"}).encode()).decode()})
        for index, event in enumerate(events, 1):
            event["sequence"] = index * 10
        return {"tenantId": "fixture-tenant", "replayConflict": False, "malformedEvidence": False, "watermark": len(events) * 10, "events": events}

    def provider(self, *, extra_head=None, malformed=False, heads=None, document=None, histories=None):
        document = document if document is not None else self.document()
        source = {}
        for event in document["events"]:
            project = json.loads(base64.b64decode(event["payloadBase64"]))["projectId"]
            source.setdefault(project, []).append((event["eventTypeName"], event["sequence"]))
        if extra_head is not None:
            source["unjournaled-project"] = [("ProjectCreated", 1000 + index) for index in range(extra_head)]
        source.update(histories or {})
        metadata, envelopes = {}, {}
        for project, history in source.items():
            prefix = "fixture-tenant:projects:" + project
            metadata[prefix + ":metadata"] = [json.dumps({"currentSequence": len(history)}), "9"]
            for sequence, (name, position) in enumerate(history, 1):
                envelopes[prefix + ":events:" + str(sequence)] = {
                    "TenantId": "fixture-tenant", "Domain": "projects", "AggregateId": project, "SequenceNumber": sequence,
                    "GlobalPosition": position, "EventTypeName": name if name.startswith(LAUNCHER.PROJECT_EVENT_PREFIX) else LAUNCHER.PROJECT_EVENT_PREFIX + name,
                    "MessageId": project + "-" + str(sequence), "AggregateType": "Project", "MetadataVersion": 1,
                    "SerializationFormat": "json", "version": "9", "dataSha1": "a" * 40}
        if heads is not None:
            for project, head in zip(("project-one", "project-two"), heads):
                metadata["fixture-tenant:projects:" + project + ":metadata"] = [json.dumps({"currentSequence": head}), "9"]
        if malformed:
            metadata["fixture-tenant:projects:unobserved:metadata"] = [False, False]
        def run(command):
            if command[8] == "HMGET":
                return json.dumps([json.dumps(document), "9"])
            if command[8] == "KEYS":
                return json.dumps(list(metadata if command[9].endswith(":metadata") else envelopes))
            keys = command[8:]
            if "dataSha1" in command[6]:
                return json.dumps([envelopes.get(key, {"error": "missing-envelope"}) for key in keys])
            return json.dumps([metadata[key] for key in keys])
        run.metadata, run.envelopes, run.document = metadata, envelopes, document
        return run

    def rejected(self, provider, reason):
        with self.assertRaisesRegex(RuntimeError, reason) as failure:
            LAUNCHER.persisted_cleanup("owned", "fixture-tenant", provider)
        observation = failure.exception.cleanup_observation
        self.assertEqual(observation["result"], "failed")
        retained = json.dumps(observation)
        for marker in ("fixture-tenant", "project-one", "project-two", "unjournaled-project", "PayloadBase64", "private-value"):
            self.assertNotIn(marker, retained)
        return observation

    def test_independent_physical_rereads_require_nonempty_archived_state_and_issue_no_writes(self):
        commands, provider = [], self.provider()
        def run(command):
            commands.append(command)
            return provider(command)
        result = LAUNCHER.persisted_cleanup("owned-container", "fixture-tenant", run)
        self.assertEqual(result["projectCount"], 2)
        self.assertTrue(result["allArchived"])
        self.assertEqual(result["committedSourceProjectCount"], 2)
        self.assertEqual(result["sourceProjectedEventCount"], 4)
        self.assertEqual(result["independentProviderRereads"], 2)
        self.assertEqual(sum("dataSha1" in command[6] for command in commands), 2)
        self.assertTrue(all(command[4:6] == ["--raw", "EVAL"] for command in commands))
        self.assertNotIn("project-one", json.dumps(result))

    def test_provider_command_receipts_hash_source_keys_without_changing_dispatched_reads(self):
        commands, provider = [], self.provider()
        def run(command):
            commands.append(command)
            return provider(command)
        LAUNCHER.persisted_cleanup("owned", "fixture-tenant", run)
        retained = json.dumps([LAUNCHER.command_evidence(command) for command in commands])
        self.assertNotIn("fixture-tenant", retained)
        self.assertNotIn("project-one", retained)
        self.assertIn('"HMGET"', retained)
        self.assertIn('"KEYS"', retained)
        self.assertIn("sha256:", retained)
        self.assertTrue(any("project-one" in " ".join(command) for command in commands))

    def test_source_ahead_and_journal_ahead_fail_complete_key_coverage(self):
        for heads in ((3, 2), (1, 2), (3, 1)):
            self.rejected(self.provider(heads=heads), "source-event-key-gap-or-unobserved-head")

    def test_complete_source_coverage_counts_non_lifecycle_project_events(self):
        document = self.document(non_lifecycle=True)
        result = LAUNCHER.persisted_cleanup("owned", "fixture-tenant", self.provider(document=document))
        self.assertEqual(result["journalEventCount"], 5)
        self.assertEqual(result["sourceProjectedEventCount"], 5)
        self.assertTrue(result["allArchived"])
        # A complete source stream with an unprojected non-lifecycle state event still fails.
        self.rejected(self.provider(histories={"project-one": [("ProjectCreated", 10), ("ProjectSetupUpdated", 15), ("ProjectArchived", 20)]}),
            "source-and-journal-event-coverage-mismatch")

    def test_proven_rejection_only_stream_is_allowed_without_project_projection(self):
        provider = self.provider(histories={"unjournaled-project": [("ProjectCreationRejected", 50), ("ProjectArchiveRejected", 60)]})
        result = LAUNCHER.persisted_cleanup("owned", "fixture-tenant", provider)
        self.assertEqual(result["rejectionOnlyStreamCount"], 1)
        self.assertEqual(result["sourceRejectionEventCount"], 2)
        self.assertEqual(result["sourceProjectedEventCount"], result["journalEventCount"])
        self.assertEqual(result["projectCount"], 2)
        self.assertNotIn("unjournaled-project", json.dumps(result))

    def test_created_project_rejections_occupy_source_sequences_but_are_not_required_in_journal(self):
        provider = self.provider(histories={"project-one": [("ProjectCreationRejected", 5), ("ProjectCreated", 10),
            ("ProjectArchiveRejected", 15), ("ProjectArchived", 20), ("ProjectArchiveRejected", 25)]})
        result = LAUNCHER.persisted_cleanup("owned", "fixture-tenant", provider)
        self.assertTrue(result["allArchived"])
        self.assertEqual(result["sourceRejectionEventCount"], 3)
        self.assertEqual(result["rejectionOnlyStreamCount"], 0)
        self.assertEqual(result["sourceProjectedEventCount"], 4)

    def test_absent_projection_is_not_allowed_for_created_or_mixed_created_rejection_histories(self):
        for history in ([("ProjectCreated", 50)], [("ProjectCreated", 50), ("ProjectArchiveRejected", 60)],
                [("ProjectCreationRejected", 50), ("ProjectCreated", 60)]):
            self.rejected(self.provider(histories={"unjournaled-project": history}), "source-created-project-missing-journal")

    def test_chatbot_state_changes_are_not_misclassified_as_rejections(self):
        for name in ("ProjectChatBotAuthorityGranted", "ProjectChatBotAuthorityRevoked"):
            self.assertIn(LAUNCHER.PROJECT_EVENT_PREFIX + name, LAUNCHER.PROJECT_STATE_EVENTS)
            self.rejected(self.provider(histories={"project-one": [("ProjectCreated", 10), (name, 15), ("ProjectArchived", 20)]}),
                "source-and-journal-event-coverage-mismatch")
            self.rejected(self.provider(histories={"unjournaled-project": [(name, 50)]}), "source-state-event-without-unique-creation")

    def test_concrete_event_catalog_matches_owning_contract_interfaces(self):
        for interface, catalog in (("IProjectEvent", LAUNCHER.PROJECT_STATE_EVENTS), ("IRejectionEvent", LAUNCHER.PROJECT_REJECTION_EVENTS)):
            actual = {LAUNCHER.PROJECT_EVENT_PREFIX + path.stem for path in (LAUNCHER.ROOT / "src/Hexalith.Projects.Contracts/Events").glob("*.cs")
                if ": " + interface + ";" in path.read_text()}
            self.assertEqual(catalog, actual)
        self.assertFalse(LAUNCHER.PROJECT_STATE_EVENTS & LAUNCHER.PROJECT_REJECTION_EVENTS)

    def test_every_state_event_must_match_its_actual_journal_type_and_global_position(self):
        for replacement in ({"EventTypeName": LAUNCHER.PROJECT_EVENT_PREFIX + "ProjectRestored"}, {"GlobalPosition": 25}):
            provider = self.provider()
            provider.envelopes["fixture-tenant:projects:project-one:events:2"].update(replacement)
            self.rejected(provider, "source-and-journal-event-coverage-mismatch")

    def test_source_identity_sequence_and_rejection_order_must_match_complete_stream(self):
        for replacement in ({"TenantId": "private-value"}, {"Domain": "other"}, {"AggregateId": "other"}, {"SequenceNumber": 9}):
            provider = self.provider()
            provider.envelopes["fixture-tenant:projects:project-one:events:1"].update(replacement)
            self.rejected(provider, "source-envelope-(?:identity|sequence)-mismatch")
        self.rejected(self.provider(histories={"unjournaled-project": [("ProjectCreationRejected", 60), ("ProjectArchiveRejected", 50)]}),
            "malformed-source-envelope-metadata")

    def test_missing_unknown_and_malformed_envelopes_fail_with_metadata_only_diagnostics(self):
        for replacement in ({"error": "missing-envelope"}, {"error": "malformed-envelope"}, {"EventTypeName": "private-value"},
                {"SequenceNumber": True}, {"version": ""}, {"dataSha1": "invalid"}):
            provider = self.provider()
            provider.envelopes["fixture-tenant:projects:project-one:events:1"].update(replacement)
            observation = self.rejected(provider, "Physical Project cleanup proof failed")
            self.assertTrue(observation["streams"][0]["keySha256"])
            self.assertIn("head", observation["streams"][0])
        provider = self.provider()
        del provider.envelopes["fixture-tenant:projects:project-one:events:1"]
        observation = self.rejected(provider, "source-event-key-gap-or-unobserved-head")
        self.assertEqual(observation["missingEventKeyCount"], 1)

    def test_event_keys_without_a_metadata_head_fail_even_when_journal_is_archived(self):
        provider = self.provider()
        provider.envelopes["fixture-tenant:projects:unobserved:events:1"] = dict(next(iter(provider.envelopes.values())))
        self.rejected(provider, "source-event-key-gap-or-unobserved-head")

    def test_source_bytes_versions_and_key_inventory_must_be_stable_across_rereads(self):
        for change in ("version", "dataSha1", "inventory"):
            provider, reads = self.provider(), [0]
            def run(command):
                if "dataSha1" in command[6]:
                    reads[0] += 1
                    if reads[0] == 2:
                        event = provider.envelopes["fixture-tenant:projects:project-one:events:1"]
                        if change == "inventory":
                            # Retain the read response, but change the final inventory independently.
                            result = provider(command)
                            provider.envelopes["fixture-tenant:projects:new-key:events:1"] = event
                            return result
                        event[change] = "10" if change == "version" else "b" * 40
                return provider(command)
            self.rejected(run, "unstable-source-bytes-versions-or-inventory")

    def test_source_head_bytes_and_versions_must_remain_stable(self):
        provider, reads = self.provider(), [0]
        def run(command):
            if command[8] not in ("HMGET", "KEYS") and "dataSha1" not in command[6]:
                reads[0] += 1
                if reads[0] == 2:
                    provider.metadata["fixture-tenant:projects:project-one:metadata"][1] = "10"
            return provider(command)
        self.rejected(run, "unstable-source-bytes-versions-or-inventory")

    def test_only_positively_observed_empty_source_head_is_allowed_outside_journal(self):
        result = LAUNCHER.persisted_cleanup("owned", "fixture-tenant", self.provider(extra_head=0))
        self.assertEqual(result["positivelyEmptySourceCount"], 1)
        self.rejected(self.provider(malformed=True), "missing-source-head-bytes-or-version")
        provider = self.provider()
        provider.metadata["fixture-tenant:projects:project-one:metadata"] = [json.dumps({"currentSequence": True}), "9"]
        self.rejected(provider, "invalid-or-unbounded-source-head")

    def test_head_and_key_bounds_reject_before_unbounded_envelope_reads(self):
        provider = self.provider(heads=(LAUNCHER.MAX_SOURCE_HEAD + 1, 2))
        self.rejected(provider, "invalid-or-unbounded-source-head")
        provider = self.provider()
        def run(command):
            if command[8] == "KEYS":
                return json.dumps([str(index) for index in range(LAUNCHER.MAX_SOURCE_STREAMS + 1)])
            return provider(command)
        self.rejected(run, "invalid-or-unbounded-source-inventory")

    def test_a_later_restore_cannot_be_mistaken_for_successful_archive_cleanup(self):
        self.rejected(self.provider(document=self.document(restore=True)), "created-project-not-physically-archived")

    def test_a_projection_version_change_between_physical_rereads_cannot_pass(self):
        provider, versions = self.provider(), iter(("9", "10"))
        def run(command):
            if command[8] == "HMGET":
                return json.dumps([json.dumps(provider.document), next(versions)])
            return provider(command)
        self.rejected(run, "unstable-journal")

    def test_boolean_archived_value_is_malformed_evidence(self):
        document = self.document()
        payload = {"tenantId": "fixture-tenant", "projectId": "project-one", "lifecycle": True}
        document["events"][1]["payloadBase64"] = base64.b64encode(json.dumps(payload).encode()).decode()
        self.rejected(self.provider(document=document), "malformed-journal-lifecycle")

    def test_missing_project_source_metadata_cannot_be_hidden_by_an_equal_key_count(self):
        provider = self.provider()
        del provider.metadata["fixture-tenant:projects:project-one:metadata"]
        provider.metadata["fixture-tenant:projects:unrelated:metadata"] = [json.dumps({"currentSequence": 0}), "9"]
        self.rejected(provider, "missing-project-source-metadata")


class PrivateBoundaryTests(unittest.TestCase):
    def test_compatibility_boundary_clears_inherited_lane_selection(self):
        inherited = {"E2E_SUPPORTED_READ_APPHOST": "1", "E2E_SUPPORTED_READS_ONLY": "1", "E2E_SHARED_APPHOST": "1",
            "SUPPORTED_PROJECT_BROWSER_ONLY": "1", "KEEP": "value"}
        env = LAUNCHER.private_environment(inherited)
        self.assertEqual(env, {"KEEP": "value"})
        self.assertEqual(inherited["E2E_SUPPORTED_READ_APPHOST"], "1")

    def test_managed_controls_require_real_nonempty_passing_output(self):
        with tempfile.TemporaryDirectory() as directory:
            report, commands = {}, []
            def run(command):
                commands.append(command)
                return "Ran 12 tests in 0.1s\n\nOK\n"
            LAUNCHER.run_negative_controls(run, report, Path(directory))
            self.assertIn("test_live_project_fixtures_launcher.py", commands[0])
            self.assertEqual(report["launcherControls"]["tests"], 12)
            for output in ("OK\n", "Ran 0 tests in 0s\nOK\n", "Ran 2 tests in 1s\nFAILED\n"):
                with self.subTest(output=output), self.assertRaises(RuntimeError):
                    LAUNCHER.run_negative_controls(lambda _: output, {}, Path(directory))

    def test_every_phase_forbids_focused_tests_and_playwright_rejects_a_real_focus(self):
        command = LAUNCHER.phase_command([], [], Path("owned-output"))
        self.assertIn("--forbid-only", command)
        playwright = LAUNCHER.ROOT / "tests/e2e/node_modules/@playwright/test"
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "focused.spec.cjs").write_text("const {test}=require(" + json.dumps(str(playwright)) + "); test.only('must reject focus',()=>{});\n")
            (root / "config.cjs").write_text("module.exports={testDir:__dirname,reporter:'list'};\n")
            result = LAUNCHER.support.run_test_process(["node", str(playwright / "cli.js"), "test", "--config", str(root / "config.cjs"),
                "--forbid-only", "--output", str(root / "results")], env=dict(os.environ), timeout=20, cleanup=[])
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("test.only", result.stdout)

    def test_local_default_is_private_and_ci_default_keeps_package_runner(self):
        script = LAUNCHER.ROOT / "tests/e2e/run-live-apphost.sh"
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            spy = root / "python3"
            spy.write_text("#!/bin/sh\nprintf '%s\n' \"$@\"\n")
            spy.chmod(0o700)
            env = dict(os.environ, PATH=str(root) + os.pathsep + os.environ["PATH"], CI="false")
            for key in ("E2E_SHARED_APPHOST", "E2E_SUPPORTED_READS_ONLY", "KEYCLOAK_CLIENT_ID"):
                env.pop(key, None)
            local = subprocess.run(["bash", str(script)], env=env, capture_output=True, text=True, timeout=10)
            self.assertEqual(local.returncode, 0)
            self.assertIn("run_live_project_fixtures.py", local.stdout)
            env["CI"] = "true"
            ci = subprocess.run(["bash", str(script)], env=env, capture_output=True, text=True, timeout=10)
            self.assertNotEqual(ci.returncode, 0)
            self.assertIn("KEYCLOAK_CLIENT_ID", ci.stderr)
            self.assertNotIn("run_live_project_fixtures.py", ci.stdout)


class ApplicationCensusTests(unittest.TestCase):
    def test_uncaptured_tenants_native_host_and_managed_assembly_are_discovered_only_in_owned_namespace(self):
        for arguments, namespace, expected in ((["Hexalith.Tenants"], "owned", True),
                (["dotnet", "/private/Hexalith.Tenants.dll"], "owned", True),
                (["Hexalith.Tenants.Server"], "owned", False), (["Hexalith.Tenants"], "foreign", False)):
            with self.subTest(arguments=arguments, namespace=namespace), tempfile.TemporaryDirectory() as directory:
                root = Path(directory); process = root / "123"; process.mkdir()
                (process / "cmdline").write_bytes(b"\0".join(argument.encode() for argument in arguments) + b"\0")
                (process / "environ").write_bytes(b"NAMESPACE=" + namespace.encode() + b"\0")
                def path(*parts):
                    return root if parts == ("/proc",) else Path(*parts)
                with patch.object(LAUNCHER, "Path", side_effect=path), \
                        patch.object(LAUNCHER.support, "process_start", return_value="immutable-start"):
                    observed = LAUNCHER.application_census("owned")
                self.assertEqual(bool(observed), expected)
                if expected:
                    self.assertEqual(observed["123"], {"pid": 123, "startTicks": "immutable-start", "resource": "Hexalith.Tenants"})


class SessionCleanupTests(unittest.TestCase):
    def report(self):
        return {"result": "passed", "cleanup": []}

    def session(self, name="private"):
        session = LAUNCHER.new_session(name)
        session["apphost"] = {"_isolation": "owned"}
        return session

    def stopped(self, _owned, _containers, _isolation, _run, report, **_kwargs):
        report["ownedSidecarsAbsent"] = True
        report["cleanup"].append({"resource": "exact-apphost", "absenceVerified": True})

    def test_capture_and_fallback_failure_still_attempt_every_known_teardown_and_retain_identity(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); private = root / "private"; private.mkdir(); evidence = root / "evidence"; evidence.mkdir()
            config = private / "fixture.json"; config.write_text('{"secret":"private-value"}')
            owned = [{"name": "known-redis", "id": "a" * 64}, {"name": "known-oidc", "id": "b" * 64}]
            report = self.report(); calls = []
            def cleanup(_apphost, resources, *_args):
                calls.extend(item["id"] for item in resources)
                raise RuntimeError("engine unavailable")
            with patch.object(LAUNCHER.support, "resource_graph", side_effect=RuntimeError("describe failed")), \
                    patch.object(LAUNCHER.support, "cleanup_resources", side_effect=cleanup):
                capture = LAUNCHER.recover_infrastructure(private, "owned", owned,
                    lambda _: (_ for _ in ()).throw(RuntimeError("census failed")), report)
                LAUNCHER.finalize_private_cleanup(private, config, "owned", capture, [self.session()], owned, None, evidence, report)
            self.assertEqual(calls, ["a" * 64, "b" * 64])
            self.assertEqual(report["result"], "failed")
            self.assertTrue(private.exists())
            recovery = Path(report["recoveryIdentity"]).read_text()
            self.assertIn("a" * 64, recovery)
            self.assertNotIn("private-value", recovery)
            self.assertIn("fallbackCensusFailure", report)

    def test_second_session_never_inherits_first_session_absence(self):
        first = self.session(); first.update(ownedSidecarsAbsent=True, applicationsAbsent=True, absenceVerified=True)
        second = LAUNCHER.new_session("ordinary")
        report = self.report(); report["ownedSidecarsAbsent"] = True
        with patch.object(LAUNCHER, "application_census", return_value={}), \
                patch.object(LAUNCHER.support, "container_listing", return_value={}), \
                patch.object(LAUNCHER.support, "cleanup_resources"):
            LAUNCHER.cleanup_session(second, "owned", None, report, seconds=0)
        self.assertFalse(second["absenceVerified"])
        self.assertFalse(second["ownedSidecarsAbsent"])
        self.assertEqual(report["result"], "failed")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); private = root / "private"; private.mkdir()
            LAUNCHER.finalize_private_cleanup(private, private / "config.json", "owned", {}, [first, second], [], None, root, report)
            recovery = json.loads(Path(report["recoveryIdentity"]).read_text())
            self.assertEqual(recovery["sessions"][1]["name"], "ordinary")
            self.assertFalse(recovery["sessions"][1]["absenceVerified"])
            self.assertTrue(private.exists())

    def test_partial_start_container_and_application_identities_are_captured_before_stop(self):
        session, report = self.session("ordinary"), self.report()
        graph = {"resources": [{"displayName": "security", "resourceType": "Container", "properties": {"container.id": "c" * 64}},
            {"displayName": "projects", "properties": {"executable.pid": 123}}]}
        stopped = [False]
        def cleanup(*args, **kwargs):
            self.assertIn("c" * 64, session["containers"])
            self.assertEqual(session["applications"]["123"]["startTicks"], "original-start")
            stopped[0] = True
            self.stopped(*args, **kwargs)
        with patch.object(LAUNCHER.support, "owns_current_apphost", return_value=True), \
                patch.object(LAUNCHER.support, "resource_graph", return_value=graph), \
                patch.object(LAUNCHER.support, "helper_descendants", return_value=[]), \
                patch.object(LAUNCHER.support, "process_start", side_effect=lambda _: None if stopped[0] else "original-start"), \
                patch.object(LAUNCHER, "application_census", return_value={}), \
                patch.object(LAUNCHER.support, "container_listing", return_value={}), \
                patch.object(LAUNCHER.support, "cleanup_resources", side_effect=cleanup):
            LAUNCHER.cleanup_session(session, "owned", None, report, seconds=0)
        self.assertTrue(session["absenceVerified"])
        self.assertTrue(session["applicationsAbsent"])

    def test_surviving_owned_application_fails_even_when_apphost_and_sidecars_disappear(self):
        session, report = self.session(), self.report()
        graph = {"resources": [{"displayName": "live-fixture-authority", "properties": {"executable.pid": 123}}]}
        with patch.object(LAUNCHER.support, "owns_current_apphost", return_value=True), \
                patch.object(LAUNCHER.support, "resource_graph", return_value=graph), \
                patch.object(LAUNCHER.support, "helper_descendants", return_value=[]), \
                patch.object(LAUNCHER.support, "process_start", return_value="original-start"), \
                patch.object(LAUNCHER, "application_census", return_value={}), \
                patch.object(LAUNCHER.support, "container_listing", return_value={}), \
                patch.object(LAUNCHER.support, "cleanup_resources", side_effect=self.stopped):
            LAUNCHER.cleanup_session(session, "owned", None, report, seconds=0)
        self.assertTrue(session["ownedSidecarsAbsent"])
        self.assertFalse(session["applicationsAbsent"])
        self.assertEqual(session["remainingApplications"]["123"]["startTicks"], "original-start")
        self.assertEqual(report["result"], "failed")
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); private = root / "private"; private.mkdir()
            LAUNCHER.finalize_private_cleanup(private, private / "config.json", "owned", {}, [session], [], None, root, report)
            self.assertIn("original-start", Path(report["recoveryIdentity"]).read_text())
            self.assertTrue(private.exists())

    def test_missing_authority_process_identity_cannot_be_covered_by_other_applications(self):
        session = LAUNCHER.new_session("private", ["projects", "live-fixture-authority"])
        session["apphost"] = {"_isolation": "owned"}
        graph = {"resources": [{"displayName": "projects", "properties": {"executable.pid": 123}},
            {"displayName": "live-fixture-authority", "properties": {}}]}
        with patch.object(LAUNCHER.support, "owns_current_apphost", return_value=True), \
                patch.object(LAUNCHER.support, "helper_descendants", return_value=[]), \
                patch.object(LAUNCHER.support, "process_start", return_value="original-start"), \
                patch.object(LAUNCHER, "application_census", return_value={}):
            LAUNCHER.capture_session(session, graph)
        self.assertFalse(session["captureVerified"])
        self.assertEqual(session["unresolvedApplications"], ["live-fixture-authority"])

    def test_private_helper_retention_and_exception_both_write_durable_metadata(self):
        for raises in (False, True):
            with self.subTest(raises=raises), tempfile.TemporaryDirectory() as directory:
                root = Path(directory); private = root / "private"; private.mkdir()
                session = self.session(); session.update(absenceVerified=True, ownedSidecarsAbsent=True, applicationsAbsent=True)
                report = self.report()
                def helper(*args):
                    if raises:
                        raise OSError("removal failed")
                    args[-1].update(privateMaterialRemoved=False, recoveryMaterialRetained=True)
                with patch.object(LAUNCHER.support, "cleanup_private_material", side_effect=helper):
                    LAUNCHER.finalize_private_cleanup(private, private / "config.json", "owned", {"captured": True}, [session], [], None, root, report)
                self.assertEqual(report["result"], "failed")
                self.assertTrue(Path(report["recoveryIdentity"]).exists())
                self.assertTrue(private.exists())

class RefusedOrdinaryStartupTests(unittest.TestCase):
    def refusal(self, output):
        failure = RuntimeError("Command failed (2): aspire start --apphost")
        failure.command_output = output
        return failure

    def test_named_custody_refusal_with_nothing_owned_verifies_the_ordinary_session(self):
        session, report = LAUNCHER.new_session("ordinary"), {"cleanup": []}
        failure = self.refusal("Unhandled exception. System.InvalidOperationException: " + LAUNCHER.ORDINARY_CATALOG_CUSTODY_REFUSAL)
        with patch.object(LAUNCHER.support, "find_owned_apphost", return_value=None), \
                patch.object(LAUNCHER.support, "owned_sidecar_census", return_value={}), \
                patch.object(LAUNCHER, "application_census", return_value={}):
            LAUNCHER.record_refused_ordinary_startup(failure, session, Path("config.json"), "fixtures-owned", report)
        self.assertTrue(session["absenceVerified"])
        self.assertTrue(report["normalStartupRefusedWithoutCatalogCustody"])
        self.assertEqual(report["cleanup"][-1]["resource"], "refused-ordinary-apphost")

    def test_any_other_start_failure_is_not_accepted(self):
        failure = self.refusal("AppHost process exited with code 134.")
        with self.assertRaises(RuntimeError) as raised:
            LAUNCHER.record_refused_ordinary_startup(failure, LAUNCHER.new_session("ordinary"), Path("config.json"), "fixtures-owned", {"cleanup": []})
        self.assertIs(raised.exception, failure)

    def test_refusal_that_leaves_an_owned_process_fails(self):
        session, report = LAUNCHER.new_session("ordinary"), {"cleanup": []}
        failure = self.refusal(LAUNCHER.ORDINARY_CATALOG_CUSTODY_REFUSAL)
        with patch.object(LAUNCHER.support, "find_owned_apphost", return_value=None), \
                patch.object(LAUNCHER.support, "owned_sidecar_census", return_value={"1": {"appId": "projects"}}), \
                patch.object(LAUNCHER, "application_census", return_value={}):
            with self.assertRaisesRegex(RuntimeError, "left owned processes"):
                LAUNCHER.record_refused_ordinary_startup(failure, session, Path("config.json"), "fixtures-owned", report)
        self.assertFalse(session["absenceVerified"])
        self.assertNotIn("normalStartupRefusedWithoutCatalogCustody", report)


if __name__ == "__main__":
    unittest.main()
