"""Read only the three source-bound component-test traces from the stopped dashboard."""

import datetime
import hashlib
import json
from pathlib import Path
import sqlite3
import xml.etree.ElementTree as ET

packet = Path(__file__).resolve().parent
run = packet.parent / "live-63"
database = Path.home() / ".aspire/dashboard/runs/20261104T164547335Z/dashboard.db"
xml_path = run / "main/server.xml"
method = "GatewayCallerAuthorityAndCurrentFolderAclControlListOpenAndSafeFailures"
tests = [node for node in ET.parse(xml_path).getroot().findall(".//test")
         if node.get("name", "").endswith("." + method)]
assert len(tests) == 1 and tests[0].get("result") == "Pass"
test = tests[0]
prefix = "Actual real-owner denial observations: "
observations = [json.loads(line.split(prefix, 1)[1])
                for line in (test.findtext("output") or "").splitlines()
                if prefix in line]
assert len(observations) == 1
observed = observations[0]
start = datetime.datetime.fromisoformat(test.get("start-rtf"))
finish = datetime.datetime.fromisoformat(test.get("finish-rtf"))
epoch = datetime.datetime(1, 1, 1, tzinfo=datetime.timezone.utc)
before = database.stat()
connection = sqlite3.connect(database.as_uri() + "?mode=ro")
connection.execute("PRAGMA query_only=ON")
sql = ("SELECT s.kind,s.start_time_ticks,s.end_time_ticks,r.resource_name "
       "FROM telemetry_spans s JOIN telemetry_resources r "
       "ON r.resource_id=s.resource_id WHERE s.trace_id=?")
rows = []
for label, field in (("denied", "DeniedTrace"), ("absent", "AbsentTrace"),
                     ("foreign-only", "ForeignOnlyTrace")):
    trace = observed[field]
    spans = connection.execute(sql, (trace,)).fetchall()
    assert spans
    times = [(epoch + datetime.timedelta(microseconds=item[1] / 10),
              epoch + datetime.timedelta(microseconds=item[2] / 10)) for item in spans]
    assert all(start <= first <= last <= finish for first, last in times)
    rows.append({"outcome": label, "traceIdFromNativeXml": trace,
                 "exportedSpanCount": len(spans),
                 "spanKinds": sorted({item[0] for item in spans}),
                 "resources": sorted({item[3] for item in spans}),
                 "allSpansInsideExactNativeTestWindow": True,
                 "outerServerSpanPresent": any(item[0] == 2 for item in spans)})
connection.close()
after = database.stat()
assert (before.st_ino, before.st_size, before.st_mtime_ns) == (
    after.st_ino, after.st_size, after.st_mtime_ns)
with database.open("rb") as stream:
    digest = hashlib.file_digest(stream, "sha256").hexdigest()
result = {"observedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
          "database": str(database), "databaseSha256": digest,
          "databaseMetadataUnchanged": True, "queryOnly": True, "sql": sql,
          "nativeXml": "../live-63/main/server.xml",
          "nativeXmlSha256": hashlib.sha256(xml_path.read_bytes()).hexdigest(),
          "nativeMethod": method, "nativeTestResult": test.get("result"),
          "sourceManifestSha256": json.loads((run / "run.json").read_text())["sourceManifestSha256"],
          "observations": rows, "outerResponseTimingEstablished": False,
          "scope": "Exported native spans from the source-bound real-owner component test; not outer request durations or a timing-acceptance sample.",
          "wholeChainPrivacyAccepted": False}
(packet / "parent-persisted-traces.json").write_text(json.dumps(result, indent=2) + "\n")
print(json.dumps({"counts": {row["outcome"]: row["exportedSpanCount"] for row in rows},
                  "outerResponseTimingEstablished": False, "databaseMetadataUnchanged": True}))
