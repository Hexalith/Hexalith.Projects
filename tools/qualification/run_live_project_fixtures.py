#!/usr/bin/env python3
"""Own the isolated compatibility fixture matrix; retain sanitized evidence only."""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import secrets
import shutil
import signal
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request
import urllib.error
import urllib.parse
import xml.etree.ElementTree as ET

# Disposable runner imports must not create bytecode outputs in the source checkout.
sys.dont_write_bytecode = True

import run_supported_project_reads as support
from supported_browser_reads import prepare_oidc, sanitize_output

ROOT = support.ROOT
APPS = ("eventstore", "tenants", "projects", "projects-workers")


def command_evidence(command):
    """Keep provider read operations reviewable without retaining source keys or tenant patterns."""
    if command[:2] == ["docker", "exec"] and command[3:6] == ["redis-cli", "--raw", "EVAL"]:
        return [*command[:8], *(value if value in ("KEYS", "HMGET", "data", "version")
            else "sha256:" + hashlib.sha256(value.encode()).hexdigest() for value in command[8:])]
    return command


def private_container_port(run):
    """Choose a free non-ephemeral port also absent from Docker's published inventory."""
    import re
    ports = run(["docker", "ps", "--format", "{{.Ports}}"])
    published = {int(value) for value in re.findall(r":(\d+)->", ports)}
    # Releasing bind(0) before Docker starts lets an outbound connection claim that ephemeral
    # port in the intervening turn. Select below the actual host's outbound allocation range.
    lower = int(Path("/proc/sys/net/ipv4/ip_local_port_range").read_text().split()[0])
    if lower <= 10000:
        raise RuntimeError("A private non-ephemeral container port range is unavailable.")
    for _ in range(128):
        port = 10000 + secrets.randbelow(lower - 10000)
        if port in published:
            continue
        with socket.socket() as candidate:
            try:
                candidate.bind(("127.0.0.1", port))
            except OSError:
                continue
        return port
    raise RuntimeError("No private container port was available within the allocation bound.")


def disabled_process_bindings(graph, owned):
    """Require the actual owned Projects server environment; missing observations never pass."""
    if not support.owns_current_apphost(owned):
        raise RuntimeError("The disabled-profile AppHost ownership changed.")
    resource = next(item for item in graph["resources"] if item.get("displayName") == "projects")
    launcher = resource.get("properties", {}).get("executable.pid")
    if type(launcher) is not int or launcher <= 0:
        raise RuntimeError("The actual disabled-profile Projects launcher is missing.")
    processes = {launcher, *support.helper_descendants(launcher)}
    observed = []
    for pid in processes:
        try:
            process = Path("/proc", str(pid))
            arguments = process.joinpath("cmdline").read_bytes().split(b"\0")
            names = [Path(argument.decode()).name for argument in arguments if argument]
            if not names or not (names[0] == "Hexalith.Projects.Server"
                    or names[0] == "dotnet" and len(names) > 1 and names[1] == "Hexalith.Projects.Server.dll"):
                continue
            environment = dict(item.split(b"=", 1) for item in process.joinpath("environ").read_bytes().split(b"\0") if b"=" in item)
            if environment.get(b"NAMESPACE") != owned["_isolation"].encode() or not environment.get(b"DAPR_HTTP_PORT"):
                raise RuntimeError("The actual Projects process does not belong to the isolated Dapr cohort.")
            bindings = [key.decode() for key in environment if key.lower().startswith(b"services__")
                and any(role in key.lower() for role in (b"conversations", b"folders", b"memories"))]
            if bindings:
                raise RuntimeError("The disabled compatibility profile exposed fixture sibling bindings.")
            observed.append({"pid": pid, "startTicks": support.process_start(pid), "fixtureSiblingBindings": bindings})
        except FileNotFoundError:
            continue
    if len(observed) != 1 or observed[0]["startTicks"] is None:
        raise RuntimeError("The actual disabled-profile Projects process is missing or ambiguous.")
    return observed[0]


def field(value, name):
    """Accept serializer casing only; absent or ambiguous persisted fields fail closed."""
    keys = [key for key in value if key.casefold() == name.casefold()]
    if len(keys) != 1:
        raise RuntimeError("A persisted fixture field is missing or ambiguous: " + name)
    return value[keys[0]]


def redis_json(redis_id, command, run):
    """Read supported Redis6 commands as JSON without requiring a newer redis-cli."""
    if not command or command[0] not in ("KEYS", "HMGET"):
        raise RuntimeError("The persisted observer permits only KEYS and HMGET reads.")
    return json.loads(run(["docker", "exec", redis_id, "redis-cli", "--raw", "EVAL",
        "return cjson.encode(redis.call(unpack(ARGV)))", "0", *command]))


def verify_phase_result(return_code, output, junit_path):
    """Bind the outer final reporter summary to actual nonempty JUnit cases and process success."""
    import re
    summaries = re.findall(r"(?m)^\[zero-live-skip\] collected=(\d+) skipped=(\d+) status=([a-z]+)\s*$", output)
    try:
        root = ET.parse(junit_path).getroot()
        counts = {key: int(root.attrib[key]) for key in ("tests", "failures", "skipped", "errors")}
        cases = root.findall(".//testcase")
        collected, skipped, status = summaries[-1]
        good = (return_code == 0 and status == "passed" and int(collected) == counts["tests"] == len(cases) > 0
            and int(skipped) == counts["skipped"] == counts["failures"] == counts["errors"] == 0
            and not any(case.find(tag) is not None for case in cases for tag in ("failure", "error", "skipped")))
    except (OSError, ET.ParseError, KeyError, IndexError, ValueError):
        good = False
    if not good:
        raise RuntimeError("The phase did not pass with matching nonempty JUnit cases and zero failures/skips.")
    # A reporter self-check inside the suite can intentionally print an empty nested probe. Only
    # the final outer report is authoritative, corroborated above by its actual JUnit and exit code.
    # Planned flaky attempts in the retry phase are recorded by JUnit separately from final failures;
    # that phase additionally requires both verified real retry receipts below.
    return {"exitCode": return_code, "collected": counts["tests"], "failures": 0, "errors": 0, "zeroSkips": True}


def runtime_projection_configuration(graph, owned):
    """Observe the real gateway's isolated inline-delivery settings before source bootstrap."""
    if not support.owns_current_apphost(owned):
        raise RuntimeError("Gateway configuration observation lost AppHost ownership.")
    resource = next(item for item in graph["resources"] if item.get("displayName") == "eventstore")
    launcher = resource.get("properties", {}).get("executable.pid")
    if type(launcher) is not int or launcher <= 0:
        raise RuntimeError("The actual gateway launcher is missing.")
    expected = ["EventStore__Projections__DefaultRefreshIntervalMs", *(
        "EventStore__Projections__Domains__" + name + "__RefreshIntervalMs" for name in ("projects", "tenants", "global-administrators"))]
    observations = []
    for pid in {launcher, *support.helper_descendants(launcher)}:
        try:
            process = Path("/proc", str(pid))
            arguments = process.joinpath("cmdline").read_bytes().split(b"\0")
            names = [Path(arg.decode()).name for arg in arguments if arg]
            if not names or not (names[0] == "Hexalith.EventStore"
                    or names[0] == "dotnet" and len(names) > 1 and names[1] == "Hexalith.EventStore.dll"):
                continue
            env = dict(item.split(b"=", 1) for item in process.joinpath("environ").read_bytes().split(b"\0") if b"=" in item)
            values = {key: env.get(key.encode(), b"").decode() for key in expected}
            values["EventStore__Projections__WorkersEnabled"] = env.get(b"EventStore__Projections__WorkersEnabled", b"").decode()
            if any(values[key] != "0" for key in expected) or values["EventStore__Projections__WorkersEnabled"] != "false":
                raise RuntimeError("The private gateway did not select inline delivery for the bootstrap and fixture domains.")
            observations.append(values)
        except FileNotFoundError:
            continue
    if len(observations) != 1:
        raise RuntimeError("The actual gateway process is missing or ambiguous.")
    return observations[0]


def runtime_worker_concurrency(owned):
    """Bind the fixture-only callback limit to the actual owned daprd process."""
    if not support.owns_current_apphost(owned):
        raise RuntimeError("Worker concurrency observation lost AppHost ownership.")
    observed = []
    for pid, peer in support.owned_sidecar_census(owned["_isolation"]).items():
        if peer["appId"] != "projects-workers":
            continue
        arguments = peer["arguments"]
        values = [arguments[index + 1] for index, value in enumerate(arguments[:-1]) if value == "--app-max-concurrency"]
        values += [value.split("=", 1)[1] for value in arguments if value.startswith("--app-max-concurrency=")]
        if values != ["1"] or support.process_start(int(pid)) != peer["start"]:
            raise RuntimeError("The actual private callback host did not select the supported concurrency limit.")
        observed.append({"pid": int(pid), "startTicks": peer["start"], "appId": "projects-workers", "appMaxConcurrency": 1})
    if len(observed) != 1:
        raise RuntimeError("The owned callback sidecar is missing or ambiguous.")
    return observed[0]


def delivery_observation(redis_id, run):
    """Retain delivery status and reason metadata without source payloads or identities."""
    import re
    keys = redis_json(redis_id, ["KEYS", "*"], run)
    selected = sorted(key for key in keys if key.startswith(("projection-activation:", "projection-checkpoints",
        "projection-delivery-", "projection-rebuild", "eventstore||ProjectionLifecycleActor||")))
    if len(selected) > 4096:
        raise RuntimeError("The owned delivery observation exceeds its bound.")
    script = """local rows={} for _,key in ipairs(KEYS) do
      local kind=redis.call('TYPE',key).ok local data=false
      if kind=='hash' then data=redis.call('HGET',key,'data') elseif kind=='string' then data=redis.call('GET',key) end
      table.insert(rows,{key=key,kind=kind,data=data}) end return cjson.encode(rows)"""
    rows = json.loads(run(["docker", "exec", redis_id, "redis-cli", "--raw", "EVAL", script, str(len(selected)), *selected])) if selected else []
    observations = []
    for row in rows:
        key, raw = row["key"], row["data"]
        entry = {"keySha256": hashlib.sha256(key.encode()).hexdigest(), "present": isinstance(raw, str), "providerType": row["kind"]}
        if isinstance(raw, str):
            entry["dataSha256"] = hashlib.sha256(raw.encode()).hexdigest()
            document = json.loads(raw)
            if isinstance(document, dict):
                items = next((value for name, value in document.items() if name.casefold() == "items"), None)
                if isinstance(items, list):
                    entry["itemCount"] = len(items)
                    entry["items"] = []
                    for item in items:
                        metadata = {}
                        for name, value in item.items():
                            if name.casefold() in ("attempt", "headsequence", "revision") and type(value) is int:
                                metadata[name] = value
                            elif name.casefold() in ("lastreasoncode", "reasoncode", "status") and isinstance(value, str):
                                metadata[name] = value if re.fullmatch(r"[A-Za-z0-9_:-]{1,128}", value) else "unrecognized"
                            elif name.casefold() in ("pendingroutes", "terminalroutes") and isinstance(value, list):
                                metadata[name + "Count"] = len(value)
                        entry["items"].append(metadata)
        observations.append(entry)
    return observations


def safe_span_observations(raw):
    """Retain timing/parentage and closed operation categories; discard arbitrary OTEL attributes."""
    import re
    result = []
    def walk(value):
        if isinstance(value, list):
            for item in value:
                walk(item)
        elif isinstance(value, dict):
            fields = {name.casefold(): item for name, item in value.items()}
            if "spanid" in fields:
                row = {}
                for key in ("traceid", "spanid", "parentspanid"):
                    item = fields.get(key)
                    if isinstance(item, str) and re.fullmatch(r"[a-fA-F0-9]{16,32}", item):
                        row[key] = item
                for key in ("timestamp", "starttime", "starttimeutc", "endtime", "endtimeutc", "duration", "durationms", "durationmilliseconds"):
                    item = fields.get(key)
                    if type(item) in (int, float) or isinstance(item, str) and re.fullmatch(r"[0-9TZ:+. -]{1,64}", item):
                        row[key] = item
                for key in ("status", "statuscode", "kind"):
                    item = fields.get(key)
                    if type(item) is int or item in ("Ok", "Error", "Unset", "Server", "Client", "Internal", "Producer", "Consumer"):
                        row[key] = item
                name = fields.get("name")
                if isinstance(name, str):
                    row["nameSha256"] = hashlib.sha256(name.encode()).hexdigest()
                for key in ("resourcename", "servicename", "source"):
                    if fields.get(key) in (*APPS, *(app + "-dapr-cli" for app in APPS), "projects-ui"):
                        row["resource"] = fields[key]
                attrs = fields.get("attributes", {})
                if isinstance(attrs, dict):
                    method = attrs.get("http.request.method", attrs.get("http.method"))
                    if method in ("GET", "POST", "PUT", "DELETE", "PATCH", "CONNECT"):
                        row["httpMethod"] = method
                    status = attrs.get("http.response.status_code", attrs.get("http.status_code"))
                    if type(status) is int or isinstance(status, str) and re.fullmatch(r"[1-5][0-9]{2}", status):
                        row["httpStatus"] = int(status)
                    target = attrs.get("url.full", attrs.get("http.url", attrs.get("http.route", "")))
                    if isinstance(target, str) and target:
                        path = urllib.parse.urlparse(target).path
                        row["routeSha256"] = hashlib.sha256(path.encode()).hexdigest()
                        for fragment, category in (("/proposals/confirm", "proposal-confirm"), ("/commands/status/", "command-status"),
                            ("/api/v1/commands", "command-submit"), ("/project/v2", "owner-projection"), ("/process", "domain-process"),
                            ("/conversations/", "conversation"), ("/folders/", "folder"), ("/_blazor", "browser-circuit")):
                            if fragment in path:
                                row["operation"] = category
                                break
                if "traceid" in row and "spanid" in row and len(row["traceid"]) == 32 and len(row["spanid"]) == 16:
                    result.append(row)
            for item in value.values():
                if isinstance(item, (dict, list)):
                    walk(item)
    walk(json.loads(raw))
    return result


def proposal_observations(path, fixture, endpoints, run):
    """Observe accepted child commands and actual owned timings, never mutate source or retain bodies."""
    import re
    import ssl
    if not path.exists():
        return {"operationCount": 0, "coverage": "NoProposalOperationsRecorded"}
    records = [json.loads(line) for line in path.read_text().splitlines() if line]
    if len(records) > 64:
        raise RuntimeError("Proposal diagnostic operation count exceeds its bound.")
    data = urllib.parse.urlencode({"grant_type": "password", "client_id": "supported-read-bootstrap", "username": "actor-1",
        "password": fixture["Browser"]["Password"], "scope": "openid"}).encode()
    context = ssl.create_default_context(cafile=fixture["TrustBundle"])
    with urllib.request.urlopen(urllib.request.Request(fixture["Browser"]["TokenEndpoint"], data,
            {"Content-Type": "application/x-www-form-urlencoded"}), context=context, timeout=10) as response:
        token = json.load(response)["access_token"]
    result = []
    for record in records:
        trace = record["traceId"]
        if not isinstance(trace, str) or not re.fullmatch(r"[a-f0-9]{32}", trace):
            raise RuntimeError("Proposal diagnostic trace identity is malformed.")
        row = {key: record.get(key) for key in ("traceId", "httpStatus", "failureType", "durationMs", "startedAtUtc", "finishedAtUtc")}
        row["projectIdentitySha256"] = hashlib.sha256(record["projectId"].encode()).hexdigest()
        root = record.get("rootIdempotencyKey")
        children = ["create"] + (["folder"] if record.get("folderRequested") else []) + ["file:" + value for value in record["fileReferenceIds"]]
        row["childCommands"] = []
        if root:
            for child in children:
                message = child.split(":", 1)[0] + "-" + hashlib.sha256((root + "\n" + child).encode()).hexdigest()[:40]
                request = urllib.request.Request(endpoints["eventstore"] + "/api/v1/commands/status/" + message,
                    headers={"Authorization": "Bearer " + token, "X-Hexalith-Tenant": fixture["Tenant"]})
                status = {"stage": child.split(":", 1)[0], "messageIdentitySha256": hashlib.sha256(message.encode()).hexdigest()}
                try:
                    with urllib.request.urlopen(request, timeout=5) as response:
                        status["httpStatus"] = response.status
                        body = json.load(response)
                    outcome = body.get("status")
                    if isinstance(outcome, str) and re.fullmatch(r"[A-Za-z]{1,32}", outcome):
                        status["commandStatus"] = outcome
                except urllib.error.HTTPError as failure:
                    status["httpStatus"] = failure.code
                except (OSError, ValueError):
                    status["observationFailed"] = True
                row["childCommands"].append(status)
        try:
            raw = run(["aspire", "otel", "spans", "--apphost", str(support.APPHOST), "--trace-id", trace,
                "--limit", "2000", "--format", "Json", "--non-interactive"], timeout=20)
            row["spans"] = safe_span_observations(raw)
            row["spanCoverage"] = "Observed" if row["spans"] else "Missing"
            row["spanLimit"] = 2000
            row["spanCoveragePossiblyTruncated"] = len(row["spans"]) >= 2000
        except Exception as failure:
            row["spanObservationFailure"] = type(failure).__name__
        result.append(row)
    observation = {"operationCount": len(result), "operations": result}
    try:
        raw = run(["aspire", "otel", "spans", "projects-ui", "--apphost", str(support.APPHOST), "--limit", "2000",
            "--format", "Json", "--non-interactive"], timeout=20)
        observation["uiSpans"] = safe_span_observations(raw)
        observation["uiSpanCoverage"] = "Observed" if observation["uiSpans"] else "Missing"
    except Exception as failure:
        observation["uiSpanObservationFailure"] = type(failure).__name__
    try:
        raw = run(["aspire", "logs", "projects-ui", "--apphost", str(support.APPHOST), "--tail", "10000",
            "--format", "Json", "--non-interactive"], timeout=20)
        console = json.loads(raw).get("logs", [])
        facts = []
        for entry in console:
            content = entry.get("content", "")
            if not isinstance(content, str) or "circuit" not in content.casefold():
                continue
            lowered = content.casefold()
            kind = next((label for fragment, label in (("disconnected", "disconnected"), ("disposed", "disposed"),
                ("disposing", "disposing"), ("closed", "closed"), ("opened", "opened"), ("created", "created"),
                ("retained", "retained")) if fragment in lowered), None)
            if kind:
                fact = {"kind": kind}
                timestamp = entry.get("timestamp")
                if isinstance(timestamp, str) and re.fullmatch(r"[0-9TZ:+. -]{1,64}", timestamp):
                    fact["timestamp"] = timestamp
                facts.append(fact)
        observation["uiCircuitConsole"] = {"rows": len(console), "limit": 10000, "facts": facts,
            "coverage": "Observed" if facts else "Missing", "activeCountInferred": False}
    except Exception as failure:
        observation["uiCircuitObservationFailure"] = type(failure).__name__
    return observation


# These concrete catalogs mirror IProjectEvent/IRejectionEvent in the owning Contracts assembly.
# Supported-read "Irrelevant" also includes state events; it is deliberately not this rejection set.
PROJECT_EVENT_PREFIX = "Hexalith.Projects.Contracts.Events."
PROJECT_STATE_EVENTS = frozenset(PROJECT_EVENT_PREFIX + name for name in (
    "FileReferenceLinked", "FileReferenceUnlinked", "MemoryLinked", "MemoryUnlinked", "ProjectArchived",
    "ProjectChatBotAuthorityGranted", "ProjectChatBotAuthorityRevoked", "ProjectCreated", "ProjectFolderCreationPending",
    "ProjectFolderSet", "ProjectResolutionConfirmed", "ProjectRestored", "ProjectSetupUpdated"))
PROJECT_REJECTION_EVENTS = frozenset(PROJECT_EVENT_PREFIX + name for name in (
    "ProjectArchiveRejected", "ProjectChatBotAuthorityRejected", "ProjectCreationRejected", "ProjectReferenceLinkRejected",
    "ProjectReferenceUnlinkRejected", "ProjectResolutionConfirmationRejected", "ProjectRestoreRejected", "ProjectSetupUpdateRejected"))
MAX_SOURCE_STREAMS, MAX_SOURCE_HEAD, MAX_SOURCE_EVENTS = 4096, 4096, 16384


def source_envelope_metadata(redis_id, keys, run):
    """Read bounded exact event keys; Redis returns metadata and byte fingerprints, never payloads."""
    script = """local rows={}
    local function field(doc,wanted)
      local count=0 local found=false
      for name,value in pairs(doc) do if string.lower(name)==string.lower(wanted) then count=count+1 found=value end end
      if count~=1 then return false end return found
    end
    for _,key in ipairs(KEYS) do
      local stored=redis.call('HMGET',key,'data','version') local raw=stored[1] local row={}
      if not raw or not stored[2] or stored[2]=='' then row.error='missing-envelope'
      elseif string.len(raw)>1048576 then row.error='envelope-byte-bound'
      else
        local ok,doc=pcall(cjson.decode,raw)
        if not ok or type(doc)~='table' then row.error='malformed-envelope'
        else
          row.version=stored[2] row.dataSha1=redis.sha1hex(raw)
          for _,name in ipairs({'TenantId','Domain','AggregateId','SequenceNumber','GlobalPosition',
            'EventTypeName','MessageId','AggregateType','MetadataVersion','SerializationFormat'}) do row[name]=field(doc,name) end
          local payload=field(doc,'Payload')
          if type(payload)~='string' or payload=='' or string.len(payload)%4~=0
            or not string.match(payload,'^[A-Za-z0-9+/]*=?=?$') then row.error='malformed-envelope' end
        end
      end
      table.insert(rows,row)
    end return cjson.encode(rows)"""
    rows = []
    for offset in range(0, len(keys), 128):
        batch = keys[offset:offset + 128]
        observed = json.loads(run(["docker", "exec", redis_id, "redis-cli", "--raw", "EVAL", script, str(len(batch)), *batch]))
        if not isinstance(observed, list) or len(observed) != len(batch):
            raise RuntimeError("Source envelope metadata rows are unobserved.")
        rows.extend(observed)
    return rows


def persisted_cleanup(redis_id, tenant, run):
    """Prove physical archival and complete source coverage; preserve only safe failure diagnostics."""
    diagnostics = {"stage": "journal", "streams": []}
    try:
        return _persisted_cleanup(redis_id, tenant, run, diagnostics)
    except Exception as cause:
        diagnostics.update(result="failed", reason=diagnostics.get("reason", "malformed-or-unobserved-" + diagnostics["stage"]))
        failure = RuntimeError("Physical Project cleanup proof failed: " + diagnostics["reason"] + ".")
        failure.cleanup_observation = diagnostics
        raise failure from cause


def _persisted_cleanup(redis_id, tenant, run, diagnostics):
    key = "projects:projection-journal:" + tenant
    def reject(reason, stream=None):
        diagnostics["reason"] = reason
        if stream is not None:
            stream["coverageReason"] = reason
        raise RuntimeError(reason)

    def observe():
        raw = redis_json(redis_id, ["HMGET", key, "data", "version"], run)
        if not isinstance(raw, list) or len(raw) != 2 or not all(isinstance(value, str) and value for value in raw):
            reject("missing-journal-bytes-or-version")
        document = json.loads(raw[0])
        if field(document, "TenantId") != tenant or field(document, "ReplayConflict") or field(document, "MalformedEvidence"):
            reject("foreign-or-failed-journal")
        projects, coverage, creations, archives = {}, {}, 0, 0
        events = field(document, "Events")
        if not isinstance(events, list) or len(events) > MAX_SOURCE_EVENTS:
            reject("journal-event-bound")
        for envelope in events:
            event = field(envelope, "EventTypeName")
            if event not in PROJECT_STATE_EVENTS:
                reject("unknown-or-rejection-journal-event")
            position = field(envelope, "Sequence")  # This journal stores global positions, not aggregate sequence numbers.
            if type(position) is not int or position <= 0:
                reject("invalid-journal-position")
            payload = json.loads(base64.b64decode(field(envelope, "PayloadBase64"), validate=True))
            if field(envelope, "TenantId") != tenant or field(payload, "TenantId") != tenant:
                reject("foreign-journal-event")
            project = field(payload, "ProjectId")
            if not isinstance(project, str) or not project or len(project) > 128 or ":" in project:
                reject("invalid-journal-project-identity")
            previous = coverage.setdefault(project, [])
            if previous and position <= previous[-1][0]:
                reject("nonmonotonic-journal-position")
            previous.append((position, event))
            name = event.removeprefix(PROJECT_EVENT_PREFIX)
            if name not in ("ProjectCreated", "ProjectArchived", "ProjectRestored"):
                if project not in projects:
                    reject("journal-event-without-creation-prefix")
                continue
            lifecycle = field(payload, "Lifecycle")
            if not isinstance(lifecycle, str) or lifecycle.casefold() not in ("active", "archived"):
                reject("malformed-journal-lifecycle")
            if name == "ProjectCreated":
                creations += 1
                projects[project] = lifecycle
            else:
                if project not in projects:
                    reject("journal-lifecycle-without-creation-prefix")
                projects[project] = lifecycle
                archives += name == "ProjectArchived"
        if (len(projects) <= 1 or creations != len(projects) or archives < len(projects)
                or any(value.casefold() != "archived" for value in projects.values())):
            reject("created-project-not-physically-archived")
        return {"projectCount": len(projects), "createdEventCount": creations, "archivedEventCount": archives,
            "allArchived": True, "watermark": field(document, "Watermark"), "journalSha256": hashlib.sha256(raw[0].encode()).hexdigest(),
            "versionSha256": hashlib.sha256(raw[1].encode()).hexdigest(), "journalEventCount": len(events),
            "sourceCoverage": {tenant + ":projects:" + project + ":metadata": entries for project, entries in coverage.items()},
            "projectIdentitySha256": sorted(hashlib.sha256(value.encode()).hexdigest() for value in projects)}

    first, second = observe(), observe()
    if first != second or type(first["watermark"]) is not int or first["watermark"] <= 0:
        reject("unstable-journal")
    coverage = first.pop("sourceCoverage")
    diagnostics["journalEventCount"] = first["journalEventCount"]
    pattern = "*" + tenant + ":projects:*:metadata"
    event_pattern = "*" + tenant + ":projects:*:events:*"
    diagnostics["stage"] = "source-inventory"
    def inventory(pattern, bound):
        keys = redis_json(redis_id, ["KEYS", pattern], run)
        if (not isinstance(keys, list) or len(keys) > bound or len(set(keys)) != len(keys)
                or not all(isinstance(key, str) and len(key) <= 2048 for key in keys)):
            reject("invalid-or-unbounded-source-inventory")
        return sorted(keys)
    keys = inventory(pattern, MAX_SOURCE_STREAMS)
    event_keys = inventory(event_pattern, MAX_SOURCE_EVENTS)
    diagnostics.update(sourceMetadataCount=len(keys), sourceEventKeyCount=len(event_keys))
    if any(sum(key.endswith(suffix) for key in keys) != 1 for suffix in coverage):
        reject("missing-project-source-metadata")
    script = "local rows={} for _,key in ipairs(KEYS) do table.insert(rows,redis.call('HMGET',key,'data','version')) end return cjson.encode(rows)"
    def source_observation():
        diagnostics["sourceReread"] = diagnostics.get("sourceReread", 0) + 1
        diagnostics["stage"] = "source-heads"
        rows = []
        for offset in range(0, len(keys), 128):
            batch = keys[offset:offset + 128]
            observed = json.loads(run(["docker", "exec", redis_id, "redis-cli", "--raw", "EVAL", script, str(len(batch)), *batch]))
            if not isinstance(observed, list) or len(observed) != len(batch):
                reject("unobserved-source-heads")
            rows.extend(observed)
        streams, expected_keys, fingerprints = [], [], []
        diagnostics["streams"] = []
        for key, row in zip(keys, rows):
            safe = {"keySha256": hashlib.sha256(key.encode()).hexdigest(), "observedEnvelopeCount": 0, "knownEventTypeCounts": {}}
            diagnostics["streams"].append(safe)
            if not isinstance(row, list) or len(row) != 2 or not all(isinstance(value, str) and value for value in row):
                reject("missing-source-head-bytes-or-version", safe)
            document = json.loads(row[0])
            if not isinstance(document, dict):
                reject("malformed-source-metadata", safe)
            head = field(document, "CurrentSequence")
            if type(head) is not int or not 0 <= head <= MAX_SOURCE_HEAD:
                reject("invalid-or-unbounded-source-head", safe)
            safe["head"] = head
            if (any(name.casefold() == "retainedfloor" and (type(value) is not int or value != 1) for name, value in document.items())
                    or any(name.casefold() == "sourceerasurepending" and value is not False for name, value in document.items())):
                reject("trimmed-or-erasing-source", safe)
            prefix, separator, tail = key.rpartition(tenant + ":projects:")
            if not separator or not tail.endswith(":metadata"):
                reject("invalid-source-key-identity", safe)
            project = tail.removesuffix(":metadata")
            if not project or ":" in project or len(project) > 128:
                reject("invalid-source-key-identity", safe)
            journal = coverage.get(tenant + ":projects:" + project + ":metadata")
            if head == 0 and journal is not None:
                reject("projected-project-has-empty-source", safe)
            source_keys = [key.removesuffix("metadata") + "events:" + str(sequence) for sequence in range(1, head + 1)]
            expected_keys.extend(source_keys)
            if len(expected_keys) > MAX_SOURCE_EVENTS:
                reject("source-event-bound", safe)
            streams.append((project, head, journal, source_keys, safe))
            fingerprints.append(hashlib.sha256(json.dumps([key, *row]).encode()).hexdigest())
        if sorted(expected_keys) != event_keys:
            missing = sorted(set(expected_keys) - set(event_keys))
            extra = sorted(set(event_keys) - set(expected_keys))
            diagnostics["missingEventKeyCount"], diagnostics["unexpectedEventKeyCount"] = len(missing), len(extra)
            diagnostics["uncoveredEventKeySha256"] = sorted(hashlib.sha256(key.encode()).hexdigest() for key in [*missing, *extra])
            reject("source-event-key-gap-or-unobserved-head")
        diagnostics["stage"] = "source-envelopes"
        event_rows = dict(zip(expected_keys, source_envelope_metadata(redis_id, expected_keys, run)))
        committed = empty = rejection_only = rejection_count = projected_count = 0
        positions, messages = set(), set()
        for project, head, journal, source_keys, safe in streams:
            projected, created, rejected, aggregate_type, last_position = [], 0, 0, None, 0
            for sequence, event_key in enumerate(source_keys, 1):
                event = event_rows[event_key]
                safe["eventKeySha256"] = hashlib.sha256(event_key.encode()).hexdigest()
                safe["expectedSequence"] = sequence
                if not isinstance(event, dict):
                    reject("malformed-source-envelope", safe)
                if event.get("error"):
                    reject(event["error"] if event["error"] in ("missing-envelope", "envelope-byte-bound", "malformed-envelope") else "malformed-source-envelope", safe)
                if (not isinstance(event.get("version"), str) or not event["version"]
                        or not isinstance(event.get("dataSha1"), str) or len(event["dataSha1"]) != 40
                        or any(character not in "0123456789abcdef" for character in event["dataSha1"])):
                    reject("unobserved-envelope-bytes-or-version", safe)
                event_type = field(event, "EventTypeName")
                if not isinstance(event_type, str) or event_type not in PROJECT_STATE_EVENTS | PROJECT_REJECTION_EVENTS:
                    if isinstance(event_type, str):
                        safe["unknownEventTypeSha256"] = hashlib.sha256(event_type.encode()).hexdigest()
                    reject("unknown-source-event-type", safe)
                safe["knownEventTypeCounts"][event_type] = safe["knownEventTypeCounts"].get(event_type, 0) + 1
                if (field(event, "TenantId") != tenant or field(event, "Domain") != "projects"
                        or field(event, "AggregateId") != project):
                    reject("source-envelope-identity-mismatch", safe)
                if type(field(event, "SequenceNumber")) is not int or field(event, "SequenceNumber") != sequence:
                    reject("source-envelope-sequence-mismatch", safe)
                position, message, kind = field(event, "GlobalPosition"), field(event, "MessageId"), field(event, "AggregateType")
                if (type(position) is not int or not 0 < position < 2 ** 53 or position in positions
                        or position <= last_position or not isinstance(message, str) or not message or len(message) > 128 or message in messages
                        or not isinstance(kind, str) or not kind or len(kind) > 128 or aggregate_type is not None and kind != aggregate_type
                        or field(event, "MetadataVersion") != 1 or type(field(event, "MetadataVersion")) is not int
                        or field(event, "SerializationFormat") != "json"):
                    reject("malformed-source-envelope-metadata", safe)
                aggregate_type, last_position = kind, position
                positions.add(position)
                messages.add(message)
                safe["observedEnvelopeCount"] += 1
                fingerprints.append(hashlib.sha256(json.dumps([event_key, event], sort_keys=True).encode()).hexdigest())
                if event_type in PROJECT_REJECTION_EVENTS:
                    rejected += 1
                    continue
                if event_type == PROJECT_EVENT_PREFIX + "ProjectCreated":
                    created += 1
                if created != 1:
                    reject("source-state-event-without-unique-creation", safe)
                projected.append((position, event_type))
            safe.update(projectEventCount=len(projected), rejectionEventCount=rejected, createdEventCount=created)
            if head == 0:
                empty += 1
                safe["coverageReason"] = "positively-empty"
            elif created == 0 and rejected == head and journal is None:
                rejection_only += 1
                safe["coverageReason"] = "proven-rejection-only"
            elif created != 1 or journal is None:
                reject("source-created-project-missing-journal", safe)
            elif projected != journal:
                reject("source-and-journal-event-coverage-mismatch", safe)
            else:
                safe["coverageReason"] = "created-project-archived-and-fully-covered"
            committed += head > 0
            rejection_count += rejected
            projected_count += len(projected)
        return {"committedSourceProjectCount": committed, "positivelyEmptySourceCount": empty,
            "createdSourceProjectCount": committed - rejection_only,
            "rejectionOnlyStreamCount": rejection_only, "sourceRejectionEventCount": rejection_count,
            "sourceProjectedEventCount": projected_count, "fingerprints": sorted(fingerprints)}
    source_first, source_second = source_observation(), source_observation()
    if source_first != source_second or inventory(pattern, MAX_SOURCE_STREAMS) != keys or inventory(event_pattern, MAX_SOURCE_EVENTS) != event_keys:
        reject("unstable-source-bytes-versions-or-inventory")
    diagnostics.update(result="passed", stage="complete")
    fingerprints = source_first.pop("fingerprints")
    return dict(first, independentProviderRereads=2, sourceMetadataCount=len(keys), **source_first,
        sourceEnvelopeAndHeadSha256=fingerprints, sourceMetadataKeySha256=sorted(hashlib.sha256(key.encode()).hexdigest() for key in keys),
        sourceCoverage=diagnostics)


def prepare_private_inputs(private, isolation, artifacts, owned, run, evidence):
    """Provision trust together with a genuinely new owned backend, never shared custody."""
    password = secrets.token_hex(32)
    (private / "password").write_text(password)
    run(["openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "1", "-subj", "/CN=localhost",
         "-addext", "subjectAltName=DNS:localhost,IP:127.0.0.1", "-addext", "keyUsage=digitalSignature,keyCertSign",
         "-keyout", str(private / "certificate.key"), "-out", str(private / "certificate.pem")])
    run(["openssl", "pkcs12", "-export", "-in", str(private / "certificate.pem"), "-inkey", str(private / "certificate.key"),
         "-out", str(private / "certificate.pfx"), "-passout", "file:" + str(private / "password")])
    for name in ("user", "owner", "tenant-owner"):
        run(["openssl", "genpkey", "-algorithm", "RSA", "-pkeyopt", "rsa_keygen_bits:2048", "-out", str(private / (name + ".pem"))])
        if name != "user":
            run(["openssl", "pkcs8", "-topk8", "-nocrypt", "-in", str(private / (name + ".pem")), "-outform", "DER", "-out", str(private / (name + ".der"))])
            run(["openssl", "pkey", "-in", str(private / (name + ".pem")), "-pubout", "-outform", "DER", "-out", str(private / (name + "-public.der"))])
    for path in private.iterdir():
        path.chmod(0o700 if path.is_dir() else 0o600)
    bootstrap_password = secrets.token_hex(32)
    browser = prepare_oidc(ROOT, private, isolation, owned, run, lambda: private_container_port(run), bootstrap_password,
        extra_permissions=("projects:create", "projects:archive", "projects:restore", "projects:update_setup",
            "projects:set_folder", "projects:link_file_reference", "projects:unlink_file_reference",
            "projects:link_memory", "projects:unlink_memory", "projects:confirm_resolution",
            "projects:link_conversation", "projects:move_conversation", "projects:unlink_conversation"))
    browser.update(Enabled=True, TokenEndpoint=browser["Authority"] + "/protocol/openid-connect/token")
    resource = {"name": isolation + "-redishost", "id": None, "cidfile": str(private / "redis.cid")}
    owned.append(resource)
    try:
        created = run(["docker", "create", "--cidfile", resource["cidfile"], "--name", resource["name"],
                       "--label", "hexalith.supported-reads=" + isolation, "-p",
                       "127.0.0.1:" + str(private_container_port(run)) + ":6379", "redis:6"]).strip()
    finally:
        if Path(resource["cidfile"]).exists():
            resource["id"] = Path(resource["cidfile"]).read_text().strip()
    if len(created) != 64 or created != resource["id"]:
        raise RuntimeError("The owned Redis creation identity is unproved.")
    run(["docker", "start", created])
    dependencies = {"RedisHost": run(["docker", "port", created, "6379/tcp"]).strip(),
                    "PlacementHostAddress": "127.0.0.1:" + str(support.free_port()),
                    "SchedulerHostAddress": "127.0.0.1:" + str(support.free_port())}
    (private / "catalog-invocations").mkdir(mode=0o700)
    source_reads = support.prepare_owner_source_read_directory(private)
    for name in APPS:
        (private / ("data-" + name)).mkdir(mode=0o700)
    discovery = private / "discovery.json"
    discovery.write_text(json.dumps({"apiVersion": "dapr.io/v1alpha1", "kind": "Configuration", "metadata": {"name": isolation},
        "spec": {"accessControl": {"defaultAction": "deny", "trustDomain": "public"},
            "nameResolution": {"component": "sqlite", "version": "v1", "configuration": {"connectionString": str(private / "discovery.sqlite")}}}}))
    discovery.chmod(0o600)
    encoded = lambda name: base64.b64encode((private / name).read_bytes()).decode()
    manifest = support.source_manifest()
    (evidence / "source-manifest.json").write_text(json.dumps(manifest, sort_keys=True, indent=2) + "\n")
    fixture = {"Enabled": True, "IsolationId": isolation, "Artifacts": str(artifacts), "UserIssuer": browser["Authority"],
        "UserKeyPath": str(private / "user.pem"), "CertificatePath": str(private / "certificate.pfx"), "CertificatePassword": password,
        "TrustBundle": str(private / "certificate.pem"), "BootstrapPassword": bootstrap_password,
        "DiscoveryConfiguration": str(discovery), "Browser": browser, "Tenant": isolation + "-tenant", "ForeignTenant": isolation + "-foreign",
        "CatalogDigestKey": base64.b64encode(secrets.token_bytes(32)).decode(), "ReceiptKey": base64.b64encode(secrets.token_bytes(32)).decode(),
        "EventStoreSecret": secrets.token_hex(32), "ProjectsSecret": secrets.token_hex(32), "TenantsSecret": secrets.token_hex(32),
        "OwnerPrivateKey": encoded("owner.der"), "OwnerPublicKey": encoded("owner-public.der"),
        "TenantOwnerPrivateKey": encoded("tenant-owner.der"), "TenantOwnerPublicKey": encoded("tenant-owner-public.der"),
        "InvocationDirectory": str(private / "catalog-invocations"), "OwnerSourceReadDirectory": str(source_reads),
        "WriterProtocolSourceCommit": run(["git", "-C", str(ROOT / "references/Hexalith.EventStore"), "rev-parse", "HEAD"]).strip(),
        "WriterProtocolSourceManifestSha256": hashlib.sha256((evidence / "source-manifest.json").read_bytes()).hexdigest()}
    return fixture, dependencies


def capture_infrastructure(private, isolation, graph):
    """Use the generic infrastructure observer with the compatibility lane's exact four receivers."""
    receipts = support.read_infrastructure_receipts(private, isolation)
    for app in APPS:
        path = Path(receipts["catalogDirectory"]) / (app + ".configuration.json")
        receipts["identities"][str(path)] = support.private_identity(path)
        specification = json.loads(path.read_text())["spec"]
        if (specification["nameResolution"] != {"component": "sqlite", "version": "v1", "configuration": {"connectionString": str(private / "discovery.sqlite")}}
                or specification["accessControl"].get("defaultAction") != "deny"):
            raise RuntimeError("The compatibility receiver is not bound to its private closed discovery plane.")
    resources = {name: next(item for item in graph["resources"] if item.get("displayName") == name) for name in support.MTLS_RESOURCES}
    containers = [support.inspect_mtls_container(resources[name].get("properties", {}).get("container.id", ""), name,
                    isolation, receipts, resources[name]) for name in support.MTLS_RESOURCES]
    return dict(receipts, containers=containers, resources=resources, registryPath=str(private / "discovery.sqlite"))


def private_environment(inherited):
    """Select compatibility readiness/journeys independently of the caller's other live lane."""
    env = support.sanitized_environment(inherited)
    for key in list(env):
        if key.startswith(("E2E_SUPPORTED_READ", "SUPPORTED_PROJECT_BROWSER_")) or key in (
                "E2E_SHARED_APPHOST", "E2E_FIXTURE_RETRY_PROBE", "E2E_FIXTURE_RETRY_DIRECTORY"):
            env.pop(key)
    return env


def phase_command(selectors, extra, output):
    return ["npm", "--prefix", "tests/e2e", "test", "--", "--workers=2", "--project=chromium", "--forbid-only",
        *selectors, *extra, "--reporter=list,junit,./reporters/zero-live-skip-reporter.ts", "--output", str(output)]


def run_negative_controls(run, report, evidence):
    """The managed entry point executes its fail-closed controls before creating private services."""
    command = [sys.executable, "-B", "-c",
        "import sys,unittest; suite=unittest.defaultTestLoader.discover('tests/tools',pattern=sys.argv[1]); "
        "result=unittest.TextTestRunner(stream=sys.stdout,verbosity=2).run(suite); sys.exit(not result.wasSuccessful())",
        "test_live_project_fixtures_launcher.py"]
    output = run(command)
    (evidence / "launcher-controls.log").write_text(sanitize_output(output, []))
    import re
    match = re.search(r"(?m)^Ran (\d+) tests? in .+$", output)
    if not match or int(match[1]) == 0 or not re.search(r"(?m)^OK\s*$", output):
        raise RuntimeError("The private launcher negative controls did not actually pass.")
    report["launcherControls"] = {"passed": True, "tests": int(match[1]), "exitCode": 0}


def new_session(name, expected_applications=()):
    return {"name": name, "expectedApplications": list(expected_applications), "startAttempted": True, "apphost": None, "applications": {}, "containers": {},
        "captureVerified": False, "ownedSidecarsAbsent": False, "applicationsAbsent": False, "absenceVerified": False}


def application_census(isolation):
    """Observe orphaned owned application processes independently of Aspire/daprd process absence."""
    names = {"Hexalith.EventStore", "Hexalith.Tenants", "Hexalith.Projects.Server", "Hexalith.Projects.Workers",
        "Hexalith.Projects.UI", "Hexalith.Projects.ReadAuthority", "Hexalith.Projects.E2E.Fixtures"}
    observed = {}
    for process in Path("/proc").iterdir():
        if not process.name.isdigit():
            continue
        try:
            if process.stat().st_uid != os.getuid():
                continue
            arguments = [Path(value.decode()).name for value in process.joinpath("cmdline").read_bytes().split(b"\0") if value]
            name = arguments[1].removesuffix(".dll") if arguments and arguments[0] == "dotnet" and len(arguments) > 1 else (arguments[0] if arguments else "")
            if name not in names:
                continue
            env = dict(item.split(b"=", 1) for item in process.joinpath("environ").read_bytes().split(b"\0") if b"=" in item)
            if env.get(b"NAMESPACE") != isolation.encode():
                continue
            start = support.process_start(int(process.name))
            if start is not None:
                observed[process.name] = {"pid": int(process.name), "startTicks": start, "resource": name}
        except FileNotFoundError:
            continue
    return observed


def capture_session(session, graph):
    """Capture immutable resource identities while this precise AppHost still owns its graph."""
    if not session["apphost"] or not support.owns_current_apphost(session["apphost"]):
        raise RuntimeError("Session ownership is unproven before resource capture.")
    resources = graph["resources"]
    unresolved = []
    for resource in resources:
        name, properties = resource.get("displayName"), resource.get("properties", {})
        pid = properties.get("executable.pid")
        if type(pid) is int and pid > 0:
            for child in {pid, *support.helper_descendants(pid)}:
                start = support.process_start(child)
                if start is not None:
                    session["applications"][str(child)] = {"pid": child, "startTicks": start, "resource": name}
        if resource.get("resourceType") == "Container":
            identity = properties.get("container.id")
            if isinstance(identity, str) and len(identity) == 64 and all(value in "0123456789abcdef" for value in identity):
                session["containers"][identity] = {"name": name, "id": identity}
            else:
                unresolved.append(name)
    for pid, item in application_census(session["apphost"]["_isolation"]).items():
        session["applications"].setdefault(pid, item)
    session["unresolvedContainers"] = unresolved
    captured_roles = {item["resource"] for item in session["applications"].values()}
    session["unresolvedApplications"] = sorted(set(session["expectedApplications"]) - captured_roles)
    session["captureVerified"] = bool(session["applications"]) and not unresolved and not session["unresolvedApplications"]


def cleanup_session(session, isolation, run, report, *, seconds=10):
    """Capture partial startup before stopping, and bind all absence evidence to this session."""
    local = {"result": "passed", "cleanup": [], "ownedSidecarsAbsent": False}
    try:
        if session["apphost"] is None:
            raise RuntimeError("The attempted AppHost session has no verified owner.")
        capture_session(session, support.resource_graph(run))
    except BaseException as failure:
        session["captureVerified"] = False
        session["captureFailure"] = type(failure).__name__
        try:
            for pid, item in application_census(isolation).items():
                session["applications"].setdefault(pid, item)
        except BaseException:
            session["applicationObservationFailed"] = True
    try:
        support.cleanup_resources(session["apphost"], [], isolation, run, local, sidecar_wait_seconds=seconds)
    except BaseException as failure:
        session["teardownFailure"] = type(failure).__name__
        local["result"] = "failed"
    session["ownedSidecarsAbsent"] = local["ownedSidecarsAbsent"]
    # Even a successful AppHost/sidecar stop cannot prove its application descendants exited.
    deadline = time.monotonic() + seconds
    try:
        while True:
            remaining = {pid: item for pid, item in session["applications"].items()
                if support.process_start(item["pid"]) == item["startTicks"]}
            remaining.update(application_census(isolation))
            containers = support.container_listing()
            remaining_containers = sorted(set(session["containers"]) & set(containers))
            if not remaining and not remaining_containers or time.monotonic() >= deadline:
                break
            time.sleep(.1)
        session["remainingApplications"] = remaining
        session["remainingContainers"] = remaining_containers
        session["applicationsAbsent"] = not remaining and bool(session["applications"])
        session["absenceVerified"] = (session["captureVerified"] and session["applicationsAbsent"]
            and not remaining_containers and session["ownedSidecarsAbsent"] and local["result"] == "passed"
            and any(item["resource"] == "exact-apphost" and item.get("absenceVerified") for item in local["cleanup"]))
    except BaseException as failure:
        session["absenceObservationFailure"] = type(failure).__name__
    report["cleanup"].extend(dict(item, session=session["name"]) for item in local["cleanup"])
    report["cleanup"].append({"resource": "owned-applications-and-containers", "session": session["name"],
        "absenceVerified": session["absenceVerified"]})
    if not session["absenceVerified"]:
        report["result"] = "failed"


def recover_infrastructure(private, isolation, owned, run, report):
    """Discovery failures cannot prevent cleanup of identities already captured at creation."""
    try:
        capture = capture_infrastructure(private, isolation, support.resource_graph(run))
        owned.extend(capture["containers"])
        return capture
    except BaseException as failure:
        report["infrastructureCaptureFailure"] = type(failure).__name__
    try:
        known = {item.get("id") for item in owned}
        for identity in run(["docker", "ps", "--all", "--no-trunc", "--filter", "label=hexalith.supported-reads=" + isolation, "--format", "{{.ID}}"]).splitlines():
            if identity not in known:
                owned.append({"name": "owned-infrastructure", "id": identity})
    except BaseException as failure:
        report["fallbackCensusFailure"] = type(failure).__name__
    report["infrastructureOwnershipUnproven"] = True
    return None


def retain_recovery(evidence, private, config_path, isolation, sessions, owned, report, infrastructure=None):
    recovery = {"sessions": sessions, "containers": owned, "isolation": isolation,
        "originalPrivateDirectory": str(private)}
    try:
        recovery["configurationSha256"] = hashlib.sha256(config_path.read_bytes()).hexdigest() if config_path.exists() else None
    except OSError as failure:
        recovery["configurationObservationFailure"] = type(failure).__name__
    if infrastructure:
        recovery["infrastructure"] = {key: infrastructure[key] for key in ("identities", "runDirectory", "certificateDirectory",
            "catalogDirectory", "custodyDirectory", "registryPath", "rootDigest") if key in infrastructure}
    # Partial receipts retain only closed path/digest metadata, explicitly without a custody claim.
    recovery["partialReceipts"] = []
    for name in ("mtls-plane.json", "catalog-plane.json"):
        try:
            receipt = json.loads((private / name).read_text())
            if receipt.get("isolationId") == isolation:
                recovery["partialReceipts"].append({"name": name, "verified": False,
                    **{key: value for key, value in receipt.items() if key in (
                        "runDirectory", "certificateDirectory", "directory", "custodyDirectory", "rootDigest") and isinstance(value, str)}})
        except (OSError, ValueError, AttributeError):
            pass
    path = evidence / "recovery-identity.json"
    path.write_text(json.dumps(recovery, indent=2) + "\n")
    path.chmod(0o600)
    report.update(result="failed", recoveryIdentity=str(path), recoveryMaterialRetained=True)


def finalize_private_cleanup(private, config_path, isolation, infrastructure, sessions, owned, run, evidence, report):
    """Attempt every known teardown and persist recovery for every unverified deletion outcome."""
    for resource in owned:
        try:
            support.cleanup_resources(None, [resource], isolation, run, report)
        except BaseException as failure:
            report["cleanup"].append({"resource": resource["name"], "absenceVerified": False, "failure": type(failure).__name__})
            report["result"] = "failed"
    report["ownedSidecarsAbsent"] = all(session["ownedSidecarsAbsent"] for session in sessions)
    report["ownedApplicationsAbsent"] = all(session["applicationsAbsent"] for session in sessions)
    verified = False
    try:
        verified = all(session["absenceVerified"] for session in sessions) and all(item.get("absenceVerified") for item in report["cleanup"])
        if verified and (not sessions or infrastructure is not None):
            if infrastructure:
                support.cleanup_private_material(private, isolation, infrastructure, bool(sessions), report)
            else:
                shutil.rmtree(private)
                report["privateMaterialRemoved"] = True
    except BaseException as failure:
        report["privateCleanupFailure"] = type(failure).__name__
    if not report.get("privateMaterialRemoved") or report.get("recoveryMaterialRetained") or not verified:
        retain_recovery(evidence, private, config_path, isolation, sessions, owned, report, infrastructure)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", type=Path, required=True, help="New destination for sanitized persistent results.")
    parser.add_argument("--timeout", type=int, default=600)
    parser.add_argument("--disabled-only", action="store_true", help="Start the actual isolated AppHost without compatibility fixture roles.")
    parser.add_argument("--fail-fast", action="store_true", help="Stop the matrix after its first failure for diagnosis; interrupted cases cannot qualify.")
    args = parser.parse_args()
    if args.timeout <= 0:
        parser.error("--timeout must be positive")
    evidence = args.evidence.resolve()
    evidence.mkdir(mode=0o700, parents=True, exist_ok=False)
    private = Path(tempfile.mkdtemp(prefix="projects-live-fixtures-"))
    private.chmod(0o700)
    artifacts = private / "artifacts"
    isolation = "fixtures-" + secrets.token_hex(12)
    env = private_environment(os.environ)
    env.update(ASPNETCORE_ENVIRONMENT="Development", DOTNET_ENVIRONMENT="Development", NAMESPACE=isolation,
        EnableKeycloak="false", KeycloakPersistent="false", UseHexalithProjectReferences="true", UseNuGetDeps="false",
        Projects__SupportedReads__Enabled="false", Projects__E2E__LiveFixtures="false" if args.disabled_only else "true",
        Projects__IsolatedLiveFixtures__Enabled="true", EnablePrivateOwnerSourceReadProof="true")
    for module in ("EventStore", "Tenants", "Folders", "Commons", "Conversations", "Memories", "FrontComposer", "Parties", "McpCli", "Builds", "Platform"):
        env["Hexalith" + module + "Root"] = str(ROOT / "references" / ("Hexalith." + module))
    env["HexalithTenantsBasePath"] = str(ROOT / "references/Hexalith.Tenants/src")
    report = {"result": "failed", "commands": [], "cleanup": [], "matrixCompleted": False,
              "profileDisabled": args.disabled_only, "sourceMode": "Debug", "sharedBackendChanged": False}
    owned, owned_apphost, infrastructure, started = [], None, None, False
    preexisting, fixture, config_path = None, None, private / "fixture.json"
    sessions, current_session = [], None
    protected = []

    def run(command, *, timeout=None):
        report["commands"].append(command_evidence(command))
        try:
            output = support.execute(command, env=env, timeout=timeout or args.timeout)
        except RuntimeError as failure:
            (evidence / ("command-" + str(len(report["commands"])) + ".log")).write_text(sanitize_output(getattr(failure, "command_output", ""), protected))
            raise
        if command[0] == "dotnet" or command[:2] == ["npm", "--prefix"]:
            (evidence / ("command-" + str(len(report["commands"])) + ".log")).write_text(sanitize_output(output, protected))
        return output

    def interrupted(signum, frame):
        raise KeyboardInterrupt

    previous = {sig: signal.signal(sig, interrupted) for sig in (signal.SIGTERM, signal.SIGINT)}
    try:
        run_negative_controls(run, report, evidence)
        if support.exact_apphost_sessions():
            raise RuntimeError("The selected AppHost already runs; the compatibility runner cannot adopt it.")
        preexisting = support.preexisting_resources()
        report["preexistingResources"] = preexisting
        print("[live-fixtures] preparing private Debug source mapping", flush=True)
        for key in ("CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets",
                    "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH", "Configuration"):
            env.pop(key, None)
        tooling = private / "tool-artifacts"
        run(["dotnet", "build",
            str(ROOT / "references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj"),
            "-c", "Debug", "-m:1", "--artifacts-path", str(tooling)])
        cli = list((tooling / "bin/Hexalith.Builds.Module.Cli").glob("debug*/Hexalith.Builds.Module.Cli.dll"))
        if len(cli) != 1:
            raise RuntimeError("The freshly built source preparation CLI is missing or ambiguous.")
        mapping = json.loads(run(["dotnet", str(cli[0]), "prepare-build", "--manifest", "module/hexalith.module-manifest.v2.json", "--mode", "source",
            "--workspace", str(private / "source-mapping"), "--output", "json"]))
        if mapping.get("status") != "prepared":
            raise RuntimeError("The supported source mapping is unavailable.")
        selected = mapping["buildPreparation"]["environment"]
        support.require_mapping_environment(selected)
        env.update(selected)
        report["sourceMappingHash"] = selected["HEXALITH_SOURCE_MAPPING_HASH"]
        fixture, dependencies = prepare_private_inputs(private, isolation, artifacts, owned, run, evidence)
        protected.extend(fixture["Browser"][key] for key in ("Password", "ClientSecret", "TenantToken", "ForeignToken"))
        config_path.write_text(json.dumps({"Projects": {"IsolatedLiveFixtures": fixture}, "Dapr": dependencies,
            "EnableKeycloak": False, "KeycloakPersistent": False}))
        config_path.chmod(0o600)
        env.update(Projects__IsolatedLiveFixtures__ConfigurationFile=str(config_path),
            # Reuse the existing exact process-identity observer. This path does not enable its owner profile.
            Projects__SupportedReads__ConfigurationFile=str(config_path), SUPPORTED_PROJECT_READ_CONFIG=str(config_path),
            PROJECTS_ISOLATED_LIVE_FIXTURES="1", SUPPORTED_PROJECT_READ_EVIDENCE=str(evidence), SSL_CERT_FILE=fixture["TrustBundle"],
            UseArtifactsOutput="true", ArtifactsPath=str(artifacts))
        print("[live-fixtures] building isolated AppHost and native fixture tests", flush=True)
        run(["dotnet", "build", str(support.TEST_PROJECT), "-c", "Debug", "-m:1", "-p:SupportedProjectReadDistributed=true", "--artifacts-path", str(artifacts)])
        assembly = artifacts / "bin/Hexalith.Projects.Integration.Tests/debug/Hexalith.Projects.Integration.Tests.dll"
        run(["dotnet", "build", "tests/Hexalith.Projects.Server.Tests/Hexalith.Projects.Server.Tests.csproj",
            "-c", "Debug", "-m:1", "--artifacts-path", str(artifacts)])
        server_tests = artifacts / "bin/Hexalith.Projects.Server.Tests/debug/Hexalith.Projects.Server.Tests.dll"
        run(["dotnet", str(server_tests), "-class", "*ProjectsRuntimeCatalogTests", "-class", "*ProjectsWorkerCatalogTests",
            "-xml", str(evidence / "catalog-guards.xml")])
        print("[live-fixtures] starting exact isolated AppHost", flush=True)
        started = True
        current_session = new_session("private", ["live-fixture-authority", *APPS, "projects-ui",
            *([] if args.disabled_only else ["conversations", "folders", "memories", "live-fixtures"])])
        sessions.append(current_session)
        owned_apphost = support.start_owned_apphost(run, config_path, isolation)
        current_session["apphost"] = owned_apphost
        capture_session(current_session, support.resource_graph(run))
        resources = ["live-fixture-authority", *APPS, "projects-ui", *(app + "-dapr-cli" for app in APPS), *support.MTLS_RESOURCES]
        if not args.disabled_only:
            resources.extend(("conversations", "folders", "memories", "live-fixtures"))
        for name in resources:
            run(["aspire", "wait", name, "--apphost", str(support.APPHOST), "--timeout", str(args.timeout), "--non-interactive"])
        graph = json.loads(run(["aspire", "describe", "--apphost", str(support.APPHOST), "--format", "Json", "--non-interactive"]))
        capture_session(current_session, graph)
        infrastructure = capture_infrastructure(private, isolation, graph)
        owned.extend(infrastructure["containers"])
        report["resources"] = [{"name": item.get("displayName"), "state": item.get("state"), "health": item.get("healthStatus")} for item in graph["resources"]]
        report["runtimeProjectionConfiguration"] = runtime_projection_configuration(graph, owned_apphost)
        report["runtimeWorkerConcurrency"] = runtime_worker_concurrency(owned_apphost)
        if args.disabled_only:
            names = {item.get("displayName") for item in graph["resources"]}
            if names.intersection({"conversations", "folders", "memories", "live-fixtures"}):
                raise RuntimeError("The disabled compatibility profile exposed fixture resources.")
            report["disabledProjectsProcess"] = disabled_process_bindings(graph, owned_apphost)
            report.update(profileDisabledRuntimeVerified=True, result="passed")
        else:
            endpoints = {name: support.resource_endpoint(graph, name) for name in ("eventstore", "projects", "eventstore-dapr-cli", "projects-dapr-cli")}
            endpoints["redis-container-id"] = next(item["id"] for item in owned if item["name"].endswith("-redishost"))
            (private / "endpoints.json").write_text(json.dumps(endpoints))
            env["SUPPORTED_PROJECT_READ_ENDPOINTS"] = str(private / "endpoints.json")
            print("[live-fixtures] seeding fresh source authority through authenticated contracts", flush=True)
            try:
                run(["dotnet", str(assembly), "-method", "Hexalith.Projects.Integration.Tests.IsolatedLiveFixtureSeedTests.SeedOwnedCompatibilityFixture",
                     "-xml", str(evidence / "seed.xml")])
            finally:
                try:
                    (evidence / "seed-delivery-observation.json").write_text(json.dumps(delivery_observation(endpoints["redis-container-id"], run), indent=2) + "\n")
                except Exception as observation_failure:
                    report["seedDeliveryObservationFailure"] = str(observation_failure) if isinstance(observation_failure, RuntimeError) else type(observation_failure).__name__
            tests = ET.parse(evidence / "seed.xml").getroot().findall(".//test")
            if len(tests) != 1 or tests[0].get("result") != "Pass":
                raise RuntimeError("The exact native compatibility seed did not pass.")
            env.update(E2E_LIVE_APPHOST="1", E2E_RUN_ID=isolation, KEYCLOAK_CLIENT_ID="supported-read-bootstrap", KEYCLOAK_REALM="hexalith",
                TEST_USER_USERNAME="actor-1", TEST_USER_PASSWORD=fixture["Browser"]["Password"],
                BASE_URL=support.resource_endpoint(graph, "projects-ui"), API_URL=endpoints["projects"], EVENTSTORE_API_URL=endpoints["eventstore"],
                KEYCLOAK_URL=fixture["Browser"]["Origin"], FIXTURE_API_URL=support.resource_endpoint(graph, "live-fixtures"),
                E2E_BROWSER_SESSION_FILE=str(private / "browser-session.json"))
            env.pop("TEST_TENANT_ID", None)
            env.pop("TEST_PRINCIPAL_ID", None)
            run(["npm", "--prefix", "tests/e2e", "run", "typecheck"])
            for phase, selectors, extra in (
                ("smoke", ["specs/live-apphost-startup.spec.ts", "specs/projects-authentication.spec.ts"], []),
                ("full", [], []),
                ("repeat", ["specs/live-fixtures-lifecycle.spec.ts"], ["--repeat-each", "2"]),
                ("retry", ["specs/live-fixtures-retry.spec.ts"], ["--retries", "1", "--repeat-each", "2"]),
            ):
                env.update(E2E_RUN_ID=isolation + "-" + phase, PLAYWRIGHT_JUNIT_OUTPUT_FILE=str(private / (phase + ".xml")))
                env["E2E_FIXTURE_RETRY_PROBE"] = "1" if phase == "retry" else "0"
                env["E2E_FIXTURE_RETRY_DIRECTORY"] = str(private / "retry-probes")
                proposal_path = private / (phase + "-proposal-observations.jsonl")
                env["E2E_PROPOSAL_OBSERVATIONS_FILE"] = str(proposal_path)
                print("[live-fixtures] collecting " + phase + " matrix with two workers", flush=True)
                command = phase_command(selectors, extra, private / (phase + "-output"))
                if args.fail_fast:
                    command.append("--max-failures=1")
                report["commands"].append(command)
                try:
                    result = support.run_test_process(command, env=env, timeout=args.timeout,
                        cleanup=report.setdefault("testProcessCleanup", []))
                except subprocess.TimeoutExpired as failure:
                    output = failure.stdout or ""
                    if isinstance(output, bytes):
                        output = output.decode(errors="replace")
                    (evidence / (phase + ".log")).write_text(sanitize_output(output, protected))
                    xml = private / (phase + ".xml")
                    if xml.exists():
                        (evidence / (phase + ".xml")).write_text(sanitize_output(xml.read_text(), protected))
                    raise RuntimeError("The " + phase + " matrix exceeded its bounded process-group deadline.") from failure
                text = result.stdout
                report.setdefault("phaseProcessResults", []).append({"name": phase, "exitCode": result.returncode})
                (evidence / (phase + ".log")).write_text(sanitize_output(text, protected))
                xml = private / (phase + ".xml")
                if xml.exists():
                    (evidence / (phase + ".xml")).write_text(sanitize_output(xml.read_text(), protected))
                if proposal_path.exists():
                    try:
                        (evidence / (phase + "-proposal-observations.json")).write_text(json.dumps(
                            proposal_observations(proposal_path, fixture, endpoints, run), indent=2) + "\n")
                    except Exception as observation_failure:
                        report.setdefault("proposalObservationFailures", {})[phase] = type(observation_failure).__name__
                verdict = verify_phase_result(result.returncode, text, xml)
                report.setdefault("phases", []).append(dict(name=phase, **verdict))
                if phase == "smoke":
                    # Smoke creates one API/UI Project beside the native warmup. Validate the real
                    # provider serialization early; the complete end-state check below still runs
                    # after the full, repeat and actual retry phases.
                    report["persistedSmokeProjectCleanup"] = persisted_cleanup(endpoints["redis-container-id"], fixture["Tenant"], run)
            probes = list((private / "retry-probes").glob("*.verified.json"))
            if len(probes) != 2:
                raise RuntimeError("Both actual retry/repeat attempts must prove prior Project archival and sibling removal.")
            report["retryIsolation"] = [json.loads(path.read_text()) for path in probes]
            report["persistedProjectCleanup"] = persisted_cleanup(endpoints["redis-container-id"], fixture["Tenant"], run)
            report["siblingCleanup"] = []
            for phase in ("smoke", "full", "repeat", "retry"):
                for role in ("live-fixtures", "conversations", "folders", "memories"):
                    address = support.resource_endpoint(graph, role) + "/_fixtures/runs/" + isolation + "-" + phase + "/observation"
                    with urllib.request.urlopen(address, timeout=10) as response:
                        observation = json.load(response)
                    if observation != {"graphCount": 0}:
                        raise RuntimeError("Sibling fixture metadata remains after the " + phase + " phase.")
                    report["siblingCleanup"].append({"phase": phase, "role": role, "graphCount": 0, "httpStatus": 200})
            report.update(result="passed", matrixCompleted=True)
        # Exercise the ordinary AppHost separately after stopping every private fixture process.
        # Private Redis remains owned until final teardown; normal startup performs no API writes.
        print("[live-fixtures] verifying ordinary startup with every fixture profile disabled", flush=True)
        cleanup_session(current_session, isolation, run, report)
        owned_apphost = None
        if not current_session["absenceVerified"] or report["result"] != "passed":
            raise RuntimeError("Private fixture teardown did not prove absence before ordinary startup.")
        env.update(Projects__IsolatedLiveFixtures__Enabled="false", Projects__SupportedReads__Enabled="false",
            Projects__E2E__LiveFixtures="false", EnableKeycloak="true", Dapr__RedisHost=dependencies["RedisHost"])
        current_session = new_session("ordinary", [*APPS, "projects-ui"])
        sessions.append(current_session)
        report["ownedSidecarsAbsent"] = False
        report["ownedApplicationsAbsent"] = False
        owned_apphost = support.start_owned_apphost(run, config_path, isolation)
        current_session["apphost"] = owned_apphost
        capture_session(current_session, support.resource_graph(run))
        for name in ("security", *APPS, "projects-ui", *(app + "-dapr-cli" for app in APPS)):
            run(["aspire", "wait", name, "--apphost", str(support.APPHOST), "--timeout", str(args.timeout), "--non-interactive"])
        normal = json.loads(run(["aspire", "describe", "--apphost", str(support.APPHOST), "--format", "Json", "--non-interactive"]))
        capture_session(current_session, normal)
        report["normalOwnedContainers"] = list(current_session["containers"].values())
        names = {item.get("displayName") for item in normal["resources"]}
        if names.intersection({"conversations", "folders", "memories", "live-fixtures", "live-fixture-authority", "supported-read-authority"}):
            raise RuntimeError("Ordinary AppHost startup exposed a fixture ingress or sibling host.")
        report["normalDisabledProjectsProcess"] = disabled_process_bindings(normal, owned_apphost)
        if not report["normalOwnedContainers"] or any(not isinstance(item["id"], str) or len(item["id"]) != 64 for item in report["normalOwnedContainers"]):
            raise RuntimeError("The exact ordinary AppHost container identities are missing.")
        report["normalDisabledResources"] = [{"name": item.get("displayName"), "state": item.get("state"), "health": item.get("healthStatus")}
            for item in normal["resources"]]
        report["normalProfileDisabledRuntimeVerified"] = True
        if support.source_manifest() != json.loads((evidence / "source-manifest.json").read_text()):
            raise RuntimeError("Source inputs changed during verification; repeat the matrix against stable sources.")
    except BaseException as failure:
        report["result"] = "failed"
        report["failure"] = str(failure) if isinstance(failure, RuntimeError) else type(failure).__name__
        if hasattr(failure, "cleanup_observation"):
            report["persistedCleanupFailure"] = failure.cleanup_observation
        if current_session and owned_apphost is None:
            try:
                owned_apphost = support.find_owned_apphost(config_path, isolation)
                current_session["apphost"] = owned_apphost
            except BaseException as ownership_failure:
                current_session["ownershipFailure"] = type(ownership_failure).__name__
        if owned_apphost is not None and support.owns_current_apphost(owned_apphost):
            for resource in (*APPS, "live-fixture-authority", *(app + "-dapr-cli" for app in APPS)):
                try:
                    output = run(["aspire", "logs", resource, "--apphost", str(support.APPHOST), "--tail", "100", "--format", "Json", "--non-interactive"], timeout=15)
                    (private / (resource + ".log")).write_text(output)
                    report.setdefault("runtimeLogFacts", {})[resource] = support.safe_log_facts(output)
                    import re
                    report.setdefault("bootstrapFailures", {})[resource] = sorted(set(re.findall(
                        r'Bootstrap failed: Reason=([a-z-]+), Phase=([a-z-]+), ExceptionType=([A-Za-z]+)', output)))
                except BaseException:
                    pass
    finally:
        try:
            if started and infrastructure is None:
                infrastructure = recover_infrastructure(private, isolation, owned, run, report)
            if current_session and not current_session["absenceVerified"]:
                cleanup_session(current_session, isolation, run, report)
            report["sessions"] = sessions
            if current_session and current_session["name"] == "ordinary":
                report["normalOwnedContainers"] = list(current_session["containers"].values())
            finalize_private_cleanup(private, config_path, isolation, infrastructure, sessions, owned, run, evidence, report)
            if preexisting is not None:
                after = support.preexisting_resources()
                report["preexistingResourcesPreserved"] = (all(after["apphosts"].get(pid) == path for pid, path in preexisting["apphosts"].items())
                    and set(preexisting["containers"]).issubset(after["containers"])
                    and all(after["sidecars"].get(pid) == value for pid, value in preexisting["sidecars"].items()))
                if not report["preexistingResourcesPreserved"]:
                    report["externalObservationsChanged"] = {
                        "apphosts": sorted(pid for pid, path in preexisting["apphosts"].items() if after["apphosts"].get(pid) != path),
                        "containers": sorted(set(preexisting["containers"]) - set(after["containers"])),
                        "sidecars": sorted(pid for pid, value in preexisting["sidecars"].items() if after["sidecars"].get(pid) != value),
                    }
                    # Other independently owned jobs can end during this run. Retain their raw
                    # disappearance verdict without treating it as a lifecycle action by this runner.
                    report["externalObservationChanged"] = True
        finally:
            for sig, handler in previous.items():
                signal.signal(sig, handler)
            (evidence / "run.json").write_text(json.dumps(report, indent=2) + "\n")
    print("[live-fixtures] " + report["result"] + "; evidence: " + str(evidence), flush=True)
    return 0 if report["result"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
