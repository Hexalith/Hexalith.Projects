"""Retain observed verification and blocked comparison cells; never manufacture activation acceptance."""
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

root = Path.cwd()
destination = root / "_bmad-output/implementation-artifacts/evidence/epic6"
evidence = destination / "6.7"
operations = ["list", "open", "conversation-start", "context", "explain", "resolve-conversation", "resolve-attachments"]
scenarios = ["complete", "partial", "unavailable", "absent-denied", "foreign-tenant", "stale-authority", "historical-pending-folderless", "cursor-scope-continuation-expiry", "descriptive-audit", "conversation-binding", "no-domain-writes"]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(repository, *arguments):
    return subprocess.check_output(["git", "-C", str(repository), *arguments], text=True).strip()


native = {}
for name, filename in {
    "Domain": "Domain.xml", "Contracts": "Contracts-verified2.xml", "Client": "Client-verified2.xml",
    "Server": "Server-owned-verified2.xml", "UI": "UI-verified2.xml", "EventStore.Generators": "generator-full.xml",
}.items():
    path = evidence / filename
    assemblies = list(ET.parse(path).iter("assembly"))
    counts = {field: sum(int(assembly.get(field, "0")) for assembly in assemblies) for field in ("total", "passed", "failed", "skipped", "errors", "not-run")}
    tests = list(ET.parse(path).iter("test"))
    if counts["total"] < 1 or counts["total"] != len(tests) or counts["passed"] != counts["total"] or any(test.get("result") != "Pass" for test in tests) or any(counts[field] for field in ("failed", "skipped", "errors", "not-run")):
        raise SystemExit("Final verification is incomplete: " + filename)
    native[name] = {"path": "6.7/" + filename, "sha256": sha(path), **counts}

server_hash = native["Server"]["sha256"]
server_tests = [test.get("name", "") for test in ET.parse(evidence / "Server-owned-verified2.xml").iter("test") if test.get("result") == "Pass"]
test_prefix = "Hexalith.Projects.Server.Tests.Queries.SupportedProjectReadHttpTests."
cases = []
for operation in operations:
    for scenario in scenarios:
        test_name = test_prefix + "SevenAuthenticatedReadsRetainIdenticalPersistedHistoryAndExposeUnresolvedComparisonCells"
        executed = scenario in ("complete", "no-domain-writes", "stale-authority", "foreign-tenant", "absent-denied")
        if scenario in ("stale-authority", "foreign-tenant", "absent-denied"):
            test_name = test_prefix + 'AuthenticatedForeignTenantAndStaleAuthorityNeverPassCutoverComparison(operation: "' + operation + '")'
        if scenario == "cursor-scope-continuation-expiry" and operation == "list":
            executed = True
            test_name = test_prefix + "AuthenticatedRuntimeHistoryRetainsIndependentKeysExactPositionsAndScopedCursorBehavior"
        if scenario == "historical-pending-folderless" and operation in ("list", "open", "context"):
            executed = True
            test_name = test_prefix + "AuthenticatedHistoricalPendingRecordsStayQuarantinedWithoutHistoryRepair"
        if executed and test_name not in server_tests:
            method = test_name.split("(", 1)[0]
            candidates = [name for name in server_tests if name.split("(", 1)[0] == method and ('operation: "' + operation + '"') in name.replace(chr(92), "")]
            if len(candidates) != 1:
                raise SystemExit("A comparison observation has no unique passing native test: " + test_name)
            test_name = candidates[0]
        cases.append({"operation": operation, "scenario": scenario, "executed": False, "observationExecuted": executed, "passed": False,
            "skipped": False, "canonicalOutput": False,
            "independentKeys": operation in ("list", "open") and scenario == "complete",
            "exactPositions": operation in ("list", "open") and scenario == "complete",
            "cursorScope": operation == "list" and scenario == "cursor-scope-continuation-expiry",
            "isolation": scenario in ("foreign-tenant", "absent-denied"),
            "supportedOnlyAssertions": executed,
            "nativeXmlHash": server_hash, "nativeTestName": test_name if executed else None,
            "status": "blocked-comparison-contract" if executed else "incomplete-comparison-cell",
            "testControlPassDoesNotMeanEquivalence": True})

focused = {}
for name, filename in {"ServerFinalPreamble": "Server-owned-final-preamble.xml", "ClientFinalGeneration": "Client-final-generation.xml"}.items():
    path = evidence / filename
    assemblies = list(ET.parse(path).iter("assembly"))
    counts = {field: sum(int(assembly.get(field, "0")) for assembly in assemblies) for field in ("total", "passed", "failed", "skipped", "errors", "not-run")}
    tests = list(ET.parse(path).iter("test"))
    if counts["total"] < 1 or counts["total"] != len(tests) or counts["passed"] != counts["total"] or any(test.get("result") != "Pass" for test in tests) or any(counts[field] for field in ("failed", "skipped", "errors", "not-run")):
        raise SystemExit("Final focused verification is incomplete: " + filename)
    focused[name] = {"path": "6.7/" + filename, "sha256": sha(path), **counts}

legacy_security = []
for operation in operations:
    test_name = next(name for name in server_tests if name.split("(", 1)[0] == test_prefix + "AuthenticatedForeignTenantAndStaleAuthorityNeverPassCutoverComparison" and ('operation: "' + operation + '"') in name.replace(chr(92), ""))
    legacy_security.append({"operation": operation, "executed": False, "passed": False,
        "currentAuthority": True, "safeMetadata": operation in ("resolve-conversation", "resolve-attachments"),
        "safeDenial": False, "noDomainWrites": True,
        "nativeXmlHash": server_hash, "nativeTestName": test_name,
        "observationExecuted": True, "status": "incomplete-security-receipt",
        "blockedBy": ["legacy-required-descriptive-name"] if operation in ("list", "open") else
            ["native-conversation-session-binding", "safe-v1-unavailability-envelope"] if operation in ("conversation-start", "context", "explain") else
            ["safe-v1-unavailability-envelope"],
        "limits": "Local stale-authority and no-domain-write observations do not establish the complete legacy denial/inspection/binding matrix."})

blockers = [
    {"id": "native-conversation-session-binding", "operations": ["conversation-start", "context", "explain"],
     "current": "Queries carry ProjectId only; no session/opened-Project proof contract is installed.",
     "expected": "Named Conversation native membership, or an authoritative opened-session Project.",
     "proposal": "Approve native-membership-only admission through the existing conversation-detail contract; null membership/session fallback remains Unavailable. No amendment applied."},
    {"id": "sequence-is-not-version", "operations": ["list", "open", "conversation-start", "context", "explain"],
     "current": "Legacy persisted Sequence is GlobalPosition 101; supported ProjectVersion is original aggregate SequenceNumber 2.",
     "expected": "No aliasing of distinct positions; exact global position and original aggregate version checked independently.",
     "proposal": "Approve legacy Sequence to original GlobalPosition mapping while asserting supported ProjectVersion against independently captured source SequenceNumber. No amendment applied."},
    {"id": "legacy-required-descriptive-name", "operations": ["list", "open"],
     "current": "V1 Project/ProjectListItem require tenant_sensitive non-null name with minLength 1; supported default omits names and separately audits inspection.",
     "expected": "Safe Metadata default and durable audit before descriptive disclosure.",
     "proposal": "Owner approves an additive legacy inspection contract with preserved overloads, or keeps these rollback slices unavailable. No disclosure difference normalized."},
    {"id": "safe-v1-unavailability-envelope", "operations": ["conversation-start", "context", "explain", "resolve-conversation", "resolve-attachments"],
     "current": "Repaired legacy stale/unsupported reads return existing 503; supported context returns 200 Unavailable or advisory returns 200 empty/incomplete with null result.",
     "expected": "Neither path grants context, suggestions or exhaustive NoMatch from missing authority/coverage.",
     "proposal": "Approve only these specific status/envelope pairs after independent nondisclosure checks; retain every AD-32/coverage field and both statuses. No amendment applied."},
]

source_files = {}
for repository, prefix in ((root, ""), (root / "references/Hexalith.EventStore", "references/Hexalith.EventStore/")):
    paths = set(git(repository, "diff", "--name-only").splitlines()) | set(git(repository, "ls-files", "--others", "--exclude-standard").splitlines())
    for relative in sorted(paths):
        path = repository / relative
        if path.is_file() and (relative.startswith(("src/", "tests/", "docs/")) or relative == ".gitattributes" or relative == "_bmad-output/implementation-artifacts/spec-6-7-cut-over-supported-reads-while-preserving-compatibility-and-rollback.md"):
            source_files[prefix + relative] = sha(path)
(evidence / "source-files.json").write_text(json.dumps(source_files, indent=2) + "\n")

checks = []
for command in (["python3", "tools/planning/validate_production_authority.py", "--story-id", "6.7"],
                ["python3", "tools/planning/validate_production_authority.py", "--validate-index"],
                ["git", "diff", "--check"], ["git", "-C", "references/Hexalith.EventStore", "diff", "--check"]):
    result = subprocess.run(command, cwd=root, capture_output=True, text=True)
    checks.append({"command": command, "exitCode": result.returncode, "output": result.stdout + result.stderr})
    if result.returncode:
        raise SystemExit(result.stdout + result.stderr)
(evidence / "static-checks.json").write_text(json.dumps(checks, indent=2) + "\n")

packet = {"schema": "projects.read-cutover.v1", "evidenceRow": "nfr-10", "story": "6.7", "status": "blocked",
    "sourceRevision": git(root, "rev-parse", "HEAD"), "sourceDirty": True,
    "baselineCommit": "c6541a305a420078b16dceaba47f36468022850b",
    "eventStoreRevision": git(root / "references/Hexalith.EventStore", "rev-parse", "HEAD"),
    "normalizationRevision": "story67-read-normalization-2026-11-16-v1",
    "normalizationApproval": {"owner": "Jerome", "date": "2026-11-16", "selection": "apply recommended", "definesRulesOnly": True},
    "contractHash": sha(root / "src/Hexalith.Projects.Contracts/openapi/hexalith.projects.v1.yaml"),
    "contractParity": False, "identityCompatibility": False,
    "localContractAndHistoryControlsPassed": True, "publishedPackageAndCompleteGateAccepted": False,
    "nativeReceipts": list(native.values()) + list(focused.values()), "verification": native,
    "finalSourceFocusedVerification": focused,
    "verificationBoundary": "Broad suites precede the final missing-Tenant guard ordering and final OpenAPI wording/fingerprint regeneration. The final-source Server class verifies the guard; final ClientGenerationTests verify the regenerated final contract artifacts. The generator XML-comment addition is documentation-only.",
    "authenticatedObservations": {"path": "6.7/authenticated-observations.json", "sha256": sha(evidence / "authenticated-observations.json"), "acceptedEquivalence": False},
    "cases": cases, "requiredCells": len(cases), "acceptedComparisonCells": 0,
    "blockers": blockers, "legacySecurity": legacy_security, "supportedActivationAccepted": False, "rollbackAccepted": False,
    "independentRuntimeControls": [test_prefix + name for name in (
        "AuthenticatedRuntimeHistoryRetainsIndependentKeysExactPositionsAndScopedCursorBehavior",
        "AuthenticatedAdvisoryComparisonsPreserveSafeMetadataAndHonestUnavailableCoverage",
        "AuthenticatedHistoricalPendingRecordsStayQuarantinedWithoutHistoryRepair")],
    "effectivePrimaryMode": "Unavailable", "sourceManifest": {"path": "6.7/source-files.json", "sha256": sha(evidence / "source-files.json")},
    "commands": ["run-with-temp bash _bmad-output/implementation-artifacts/evidence/epic6/6.7/verify.sh",
                 "run-with-temp bash _bmad-output/implementation-artifacts/evidence/epic6/6.7/verify-final.sh",
                 "STORY67_ATTEMPT=-final-preamble STORY67_TEST_CLASS=Hexalith.Projects.Server.Tests.Queries.GetConversationStartSetupTests run-with-temp python3 _bmad-output/implementation-artifacts/evidence/epic6/6.7/verify-server.py",
                 "STORY67_ATTEMPT=-final-generation STORY67_TEST_CLASS=Hexalith.Projects.Client.Tests.ClientGenerationTests run-with-temp bash _bmad-output/implementation-artifacts/evidence/epic6/6.7/verify.sh .Client"],
    "auditPolicy": {"enabled": True, "disabled": False, "contextNuGetAuditFalseInstructionNotApplied": "The spec and baseline require preserving audits."},
    "limits": ["Controlled local JWT/owner fixtures and owned Dapr state.in-memory sidecar; no production or deployed acceptance.",
               "Every missing, mismatched or unapproved cell blocks activation; passing refusal controls are not equivalence receipts.",
               "EventStore typed export needs a package release before package-mode adoption; no versions changed or packages published.",
               "DW-63 remains open for Story 7.1; readiness, Epic 7/8 and AD-25 pins unchanged."]}
(destination / "6.7-read-cutover.json").write_text(json.dumps(packet, indent=2) + "\n")
rollback = {"story": "6.7", "sourceRevision": packet["sourceRevision"], "receiptSha256": sha(destination / "6.7-read-cutover.json"),
    "requestedMode": "LegacyRollback", "requiredOutcome": "Unavailable", "operationalRollbackExecuted": False,
    "reason": "No complete security-verified legacy receipt; list/open inspection and Conversation binding decisions remain unresolved.",
    "executedControl": "Hexalith.Projects.Client.Tests.ReadCutoverRoutingTests.ExplicitRollbackRequiresItsOwnSecurityProofAndFailureNeverChangesSelection",
    "controlXml": native["Client"], "persistedHistoryControlXml": native["Server"],
    "syntheticPositiveRollbackControlsAreNotOperationalAcceptance": True,
    "ownedSidecarCleanup": json.loads((evidence / "sidecar-cleanup-verified2.json").read_text()),
    "finalFocusedSidecarCleanup": json.loads((evidence / "sidecar-cleanup-final-preamble.json").read_text())}
(destination / "6.7-rollback.log").write_text(json.dumps(rollback, indent=2) + "\n")
print(json.dumps({"status": "blocked", "nativePassed": sum(item["passed"] for item in native.values()), "requiredCells": len(cases), "acceptedComparisonCells": 0}))
