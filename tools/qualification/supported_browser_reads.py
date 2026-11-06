"""Real OIDC browser verification inside the existing owned supported-read runtime."""
from __future__ import annotations
import copy
import base64
import hashlib
import json
import os
import re
from pathlib import Path
import secrets
import signal
import ssl
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET


def prepare_oidc(root, private, isolation, owned, run, free_port, password):
    """Own an HTTPS Keycloak before AppHost startup; leave all existing private guards intact."""
    realm = json.loads((root / 'src/Hexalith.Projects.AppHost/KeycloakRealms/hexalith-realm.json').read_text())
    clients = {client['clientId']: client for client in realm['clients']}
    api = copy.deepcopy(clients['hexalith-eventstore'])
    api['clientId'] = 'supported-read-bootstrap'
    ui = copy.deepcopy(clients['hexalith-projects-ui'])
    client_secret = secrets.token_hex(32)
    ui['secret'] = client_secret
    # Tenant UI queries do not carry the setup client's global bootstrap marker.
    ui['protocolMappers'] = [mapper for mapper in ui['protocolMappers']
                             if mapper.get('config', {}).get('claim.name') not in ('global_admin', 'is_global_admin')]
    for client in (api, ui):
        client['protocolMappers'].append({'name': 'supported-read-users-audience', 'protocol': 'openid-connect',
            'protocolMapper': 'oidc-audience-mapper', 'consentRequired': False,
            'config': {'included.client.audience': 'supported-read-users', 'access.token.claim': 'true', 'id.token.claim': 'false'}})
    realm['clients'] = [api, ui]
    template = next(user for user in realm['users'] if user['username'] == 'admin-user')
    permissions = ['command:submit', 'query:read', 'replay:read', 'read_metadata', 'projects:read', 'projects:list',
                   'projects:inspect_descriptive_metadata', 'create_folder', 'manage_folder_access', 'manage_organization_acl',
                   'tenants:create', 'tenants:add-user', 'tenants:read', 'global-administrators:read']
    # Retain the native fixture's exact authority vocabulary rather than broadening a production role.
    permissions += ['admin:reconcile-global-administrators-delivery', 'global-administrators:bootstrap']
    realm['users'] = []
    for name, tenant in [('actor-1', isolation + '-tenant'), ('foreign-actor', isolation + '-foreign')]:
        user = copy.deepcopy(template)
        user.update(id=name, username=name, email=name + '@fixture.invalid', firstName='Fixture', lastName='Reader')
        user['credentials'] = [{'type': 'password', 'value': password, 'temporary': False}]
        user['attributes'] = {'tenants': [tenant], 'current_tenant': [tenant], 'domains': ['projects', 'folders', 'tenants', 'global-administrators'],
                              'permissions': permissions, 'global_admin': ['true'], 'projects_role': ['TenantProjectAdministrator']}
        realm['users'].append(user)
    realm_path = private / 'browser-realm.json'
    realm_path.write_text(json.dumps(realm))
    os.chmod(realm_path, 0o600)
    port = free_port()
    origin = f'https://127.0.0.1:{port}'
    authority = origin + '/realms/hexalith'
    name = isolation + '-oidc'
    resource = {'name': name, 'id': None, 'cidfile': str(private / 'oidc.cid')}
    owned.append(resource)
    previous = signal.pthread_sigmask(signal.SIG_BLOCK, {signal.SIGTERM, signal.SIGINT})
    try:
        created = run(['docker', 'create', '--cidfile', resource['cidfile'], '--name', name, '--label',
                       'hexalith.supported-reads=' + isolation, '-p', f'127.0.0.1:{port}:8443',
                       '-v', str(realm_path) + ':/opt/keycloak/data/import/fixture.json:ro',
                       '-v', str(private / 'certificate.pem') + ':/opt/keycloak/conf/server.crt:ro',
                       '-v', str(private / 'certificate.key') + ':/opt/keycloak/conf/server.key:ro',
                       'quay.io/keycloak/keycloak:26.6', 'start-dev', '--import-realm', '--http-enabled=false',
                       '--https-certificate-file=/opt/keycloak/conf/server.crt', '--https-certificate-key-file=/opt/keycloak/conf/server.key',
                       '--hostname=' + origin]).strip()
    finally:
        try:
            if Path(resource['cidfile']).exists(): resource['id'] = Path(resource['cidfile']).read_text().strip()
        finally:
            signal.pthread_sigmask(signal.SIG_SETMASK, previous)
    if len(created) != 64 or created != resource['id']:
        raise RuntimeError('OIDC container ownership is unproven.')
    run(['docker', 'start', created])
    context = ssl.create_default_context(cafile=str(private / 'certificate.pem'))
    deadline = time.monotonic() + 120
    while True:
        try:
            with urllib.request.urlopen(authority + '/.well-known/openid-configuration', context=context, timeout=3) as response:
                if json.load(response)['issuer'] == authority: break
        except (OSError, ValueError, KeyError):
            if time.monotonic() >= deadline: raise RuntimeError('Owned HTTPS OIDC readiness failed.') from None
            time.sleep(0.25)
    tokens = {}
    subjects = {}
    for name in ('actor-1', 'foreign-actor'):
        body = urllib.parse.urlencode({'grant_type': 'password', 'client_id': 'supported-read-bootstrap', 'username': name,
                                       'password': password, 'scope': 'openid'}).encode()
        request = urllib.request.Request(authority + '/protocol/openid-connect/token', body,
                                         {'Content-Type': 'application/x-www-form-urlencoded'})
        with urllib.request.urlopen(request, context=context, timeout=10) as response:
            tokens[name] = json.load(response)['access_token']
        encoded = tokens[name].split('.')[1]
        claims = json.loads(base64.urlsafe_b64decode(encoded + '=' * (-len(encoded) % 4)))
        subjects[name] = claims.get('sub')
        audiences = claims.get('aud', [])
        if isinstance(audiences, str): audiences = [audiences]
        if not {'hexalith-eventstore', 'supported-read-users'}.issubset(set(audiences)):
            raise RuntimeError('The real OIDC fixture credential omitted a required supported-read audience.')
        if not isinstance(subjects[name], str) or not subjects[name].strip():
            raise RuntimeError('The real OIDC credential omitted its stable subject.')
        permissions = claims.get('eventstore:permission', [])
        if (not isinstance(permissions, list) or not {'projects:read', 'projects:list', 'global-administrators:bootstrap'}.issubset(set(permissions))
                or str(claims.get('global_admin', '')).lower() != 'true'
                or claims.get('projects:role') != 'TenantProjectAdministrator'
                or claims.get('eventstore:surface') != 'ui'):
            raise RuntimeError('The real OIDC fixture credential omitted required bootstrap/read authority.')
        if not {'projects.read', 'projects.list'}.issubset(set(claims.get('scope', '').split())):
            raise RuntimeError('The real OIDC credential omitted the required read scopes.')
    return {'Authority': authority, 'Origin': origin, 'ClientSecret': client_secret, 'Password': password,
            'ImageId': run(['docker', 'inspect', '--format', '{{.Image}}', created]).strip(),
            'TenantToken': tokens['actor-1'], 'ForeignToken': tokens['foreign-actor'],
            'PrimaryActor': subjects['actor-1'], 'ForeignActor': subjects['foreign-actor'],
            'RealmSha256': hashlib.sha256(realm_path.read_bytes()).hexdigest()}


def safe_command_refusal(path):
    """Retain closed public refusal facts, never a protected ProblemDetails body."""
    raw = Path(path).read_bytes()
    result = {'responseBytes': len(raw), 'responseSha256': hashlib.sha256(raw).hexdigest()}
    try:
        body = json.loads(raw)
    except (ValueError, TypeError):
        return result
    if not isinstance(body, dict): return result
    reason = body.get('reason', body.get('detail'))
    if isinstance(reason, str):
        result['reasonSha256'] = hashlib.sha256(reason.encode()).hexdigest()
        if 'The authenticated surface cannot invoke this operation.' in reason:
            result['closedReason'] = 'authenticated-surface-refused'
    code = body.get('reasonCode')
    if code in {'tenant_missing', 'tenant_mismatch', 'tenant_ambiguous', 'principal_not_member',
                'insufficient_role', 'insufficient_permission', 'authentication_required', 'authorization_failed'}:
        result['reasonCode'] = code
    return result


def require_seed_receipt(path, assembly):
    """Reject empty, skipped, failed, wrong-method and wrong-assembly native receipts."""
    try:
        root = ET.parse(path).getroot()
    except (OSError, ET.ParseError):
        raise RuntimeError('The native browser seed XML is missing or invalid.') from None
    assemblies = root.findall('assembly')
    if len(assemblies) != 1:
        raise RuntimeError('The native browser seed must execute exactly one assembly and case.')
    observed = assemblies[0]
    expected = {'name': str(assembly), 'total': '1', 'passed': '1', 'failed': '0', 'skipped': '0', 'errors': '0', 'not-run': '0'}
    cases = observed.findall('.//test')
    if (any(observed.get(key) != value for key, value in expected.items()) or len(cases) != 1
            or observed.findall('./errors/error') or root.findall('./errors/error')
            or cases[0].get('result') != 'Pass'
            or cases[0].get('type') != 'Hexalith.Projects.Integration.Tests.SupportedProjectBrowserSeedTests'
            or cases[0].get('method') != 'SeedRealOidcBrowserFixture'):
        raise RuntimeError('The native browser seed did not execute its exact passing case without errors or skips.')
    return {'total': 1, 'passed': 1, 'failed': 0, 'skipped': 0, 'errors': 0,
            'xmlSha256': hashlib.sha256(Path(path).read_bytes()).hexdigest()}


class _LogObservation:
    """Scan a stream in memory; receipts contain no log text or credential material."""
    def __init__(self, protected):
        self._protected = tuple(value.encode() for value in protected)
        self._carry = b''
        self._overlap = max(32, *(len(value) for value in self._protected))
        self._hash = hashlib.sha256()
        self._jwt_stage = 0
        self._jwt_length = 0
        self._jwt_prefix = 0
        self.captured_bytes = 0
        self.entries = 0
        self.protected_absent = True
        self.jwt_absent = True

    def accept(self, data):
        self._hash.update(data)
        self.captured_bytes += len(data)
        window = self._carry + data
        self.protected_absent &= all(value not in window for value in self._protected)
        self.entries += sum(match.end() > len(self._carry) for match in re.finditer(rb'"resourceName"\s*:', window))
        self._carry = window[-self._overlap:]
        # Only counters/stages survive chunk boundaries, including arbitrarily long JWT payloads.
        for value in data:
            base64url = 65 <= value <= 90 or 97 <= value <= 122 or 48 <= value <= 57 or value in (45, 95)
            if self._jwt_stage:
                if base64url:
                    self._jwt_length += 1
                    if self._jwt_stage == 3 and self._jwt_length >= 8:
                        self.jwt_absent = False
                elif value == 46 and self._jwt_stage < 3 and self._jwt_length >= (11 if self._jwt_stage == 1 else 8):
                    self._jwt_stage += 1
                    self._jwt_length = 0
                else:
                    self._jwt_stage = 0
                    self._jwt_prefix = 1 if value == 101 else 0
            elif value == (101, 121, 74)[self._jwt_prefix]:
                self._jwt_prefix += 1
                if self._jwt_prefix == 3:
                    self._jwt_stage = 1
                    self._jwt_length = 3
                    self._jwt_prefix = 0
            else:
                self._jwt_prefix = 1 if value == 101 else 0

    def receipt(self):
        return {'capturedBytes': self.captured_bytes, 'logEntries': self.entries, 'sha256': self._hash.hexdigest(),
                'protectedMarkerAbsent': bool(self.protected_absent), 'jwtShapedCredentialAbsent': self.jwt_absent}


class _BrowserTelemetry:
    """Own continuous CLI readers established before any browser request; discard raw bytes."""
    def __init__(self, root, protected, group_members):
        self.group_members = group_members
        self.root = root
        self.protected = protected
        self.streams = {}
        self.ready_at = None

    @staticmethod
    def now():
        from datetime import datetime, timezone
        return datetime.now(timezone.utc).isoformat()

    def start(self):
        import subprocess
        import threading
        for resource in ('eventstore', 'projects', 'projects-ui'):
            command = ['aspire', 'logs', resource, '--apphost', str(self.root / 'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'),
                       '--format', 'Json', '--tail', '1', '--follow', '--non-interactive']
            process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, cwd=self.root, start_new_session=True)
            stream = {'process': process, 'stdout': _LogObservation(self.protected), 'stderr': _LogObservation(self.protected),
                      'leaderStartTicks': self.group_members(process.pid).get(process.pid), 'readFailed': False, 'startedAtUtc': self.now(), 'command': command}
            self.streams[resource] = stream
            def consume_stdout(current=stream):
                try:
                    while data := current['process'].stdout.read1(65536): current['stdout'].accept(data)
                except OSError:
                    current['readFailed'] = True
            def consume_stderr(current=stream):
                try:
                    while data := current['process'].stderr.read1(8192):
                        current['stderr'].accept(data)
                except OSError:
                    current['readFailed'] = True
            stream['threads'] = [threading.Thread(target=consume_stdout), threading.Thread(target=consume_stderr)]
            for thread in stream['threads']: thread.start()
        deadline = time.monotonic() + 15
        while True:
            if any(value['process'].poll() is not None for value in self.streams.values()):
                raise RuntimeError('A browser telemetry reader exited before interval coverage was established.')
            if all(value['stdout'].entries > 0 for value in self.streams.values()):
                self.ready_at = self.now()
                return
            if time.monotonic() >= deadline:
                raise RuntimeError('Browser telemetry readers did not establish structured live coverage.')
            time.sleep(0.05)

    def finish(self, interval):
        import subprocess
        time.sleep(0.5)
        stopped_at = self.now()
        receipts = {}
        for resource, value in self.streams.items():
            process = value['process']
            # The unreaped leader reserved this fresh group for this reader alone.
            observed = self.group_members(process.pid)
            alive = process.poll() is None
            if observed:
                try: os.killpg(process.pid, signal.SIGKILL)
                except ProcessLookupError: pass
            try: process.wait(timeout=10)
            except subprocess.TimeoutExpired: value['readFailed'] = True
            deadline = time.monotonic() + 10
            while self.group_members(process.pid) and time.monotonic() < deadline: time.sleep(0.01)
            absent = not self.group_members(process.pid)
            for thread in value['threads']: thread.join(timeout=3)
            continuous = bool(self.ready_at and alive and absent and not value['readFailed'] and all(not thread.is_alive() for thread in value['threads']))
            if all(not thread.is_alive() for thread in value['threads']):
                process.stdout.close()
                process.stderr.close()
            cleanup = {'pid': process.pid, 'groupId': process.pid, 'leaderStartTicks': value['leaderStartTicks'],
                       'observedMemberCount': len(observed), 'descendantsAbsent': absent}
            receipts[resource] = {**value['stdout'].receipt(), 'stderr': value['stderr'].receipt(),
                                  'command': value['command'], 'startedAtUtc': value['startedAtUtc'],
                                  'readyAtUtc': self.ready_at, 'stoppedAtUtc': self.now(), 'aliveAtOwnedStop': alive,
                                  'continuousThroughInterval': continuous, 'exitCodeAfterOwnedStop': process.returncode, 'cleanup': cleanup}
        covered = bool(interval.get('startedAtUtc') and interval.get('endedAtUtc') and self.ready_at
                       and self.ready_at <= interval['startedAtUtc'] <= interval['endedAtUtc'] <= stopped_at
                       and len(receipts) == 3 and all(value['continuousThroughInterval'] for value in receipts.values()))
        return {'scope': 'Continuous console streams established before Playwright starts, through its completion; one earlier entry per resource is included.',
                'browserInterval': interval, 'completeIntervalObserved': covered, 'resources': receipts,
                'protectedMarkerAbsent': all(value['protectedMarkerAbsent'] and value['jwtShapedCredentialAbsent']
                    and value['stderr']['protectedMarkerAbsent'] and value['stderr']['jwtShapedCredentialAbsent'] for value in receipts.values())}


MANUAL_HOLD_DEFAULT_SECONDS = 4 * 60 * 60


def manual_hold(directory, access, *, seconds=MANUAL_HOLD_DEFAULT_SECONDS, poll=5.0, clock=time.monotonic, sleep=time.sleep):
    """Keep the verified owned topology available for a person, then release it for exact owned cleanup.

    Access details (including the private fixture password) exist only in a 0600 file inside an existing
    owner-only directory, and are removed before returning; the returned receipt holds no access values.
    """
    directory = Path(directory)
    if directory.is_symlink() or not directory.is_dir():
        raise RuntimeError('The manual hold directory must be an existing real directory.')
    status = directory.stat()
    if status.st_uid != os.getuid() or status.st_mode & 0o077:
        raise RuntimeError('The manual hold directory must be private to the launcher owner.')
    access_path, release_path = directory / 'access.json', directory / 'release'
    if access_path.exists() or release_path.exists():
        raise RuntimeError('The manual hold directory must not contain an earlier hold.')
    descriptor = os.open(access_path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, 'w') as handle:
        json.dump(access, handle, indent=2)
    started = clock()
    released = False
    try:
        while clock() - started < seconds:
            if release_path.exists():
                released = True
                break
            sleep(poll)
    finally:
        access_path.unlink(missing_ok=True)
        release_path.unlink(missing_ok=True)
    return {'released': released, 'heldSeconds': round(clock() - started, 1), 'limitSeconds': seconds,
            'accessRemoved': not access_path.exists(), 'countedAsAutomatedProof': False}


def run_browser(root, private, evidence, graph, endpoint, fixture, env, run, assembly, report, run_test_process, group_members):
    """Seed actual persisted owner state through real credentials, then inspect it through OIDC and SDK."""
    command = ['dotnet', str(assembly), '-method',
               'Hexalith.Projects.Integration.Tests.SupportedProjectBrowserSeedTests.SeedRealOidcBrowserFixture',
               '-xml', str(evidence / 'browser-seed.xml')]
    report['commands'].append(command)
    result = run_test_process(command, env=env, timeout=600, cleanup=report.setdefault('testProcessCleanup', []))
    (evidence / 'browser-seed.log').write_text(result.stdout)
    diagnostic = private / 'command-diagnostic.json'
    if diagnostic.exists():
        report['seedCommandRefusal'] = safe_command_refusal(diagnostic)
    report['browserSeed'] = require_seed_receipt(evidence / 'browser-seed.xml', assembly)
    if result.returncode: raise RuntimeError('Real OIDC owner seeding failed; inspect browser-seed.xml.')
    run(['aspire', 'wait', 'projects-ui', '--apphost', str(root / 'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'), '--timeout', '120', '--non-interactive'])
    run(['aspire', 'wait', 'browser-read-observer', '--apphost', str(root / 'src/Hexalith.Projects.AppHost/Hexalith.Projects.AppHost.csproj'), '--timeout', '120', '--non-interactive'])
    fixture_url = endpoint(graph, 'browser-read-observer')
    values = {'graphId': fixture['IsolationId'], 'runId': fixture['IsolationId'], 'workerIndex': 0, 'retry': 0, 'repeatEachIndex': 0,
              'scenario': 'supported-oidc', 'tenantId': fixture['Tenant'], 'principalId': fixture['Browser']['PrimaryActor'], 'filePath': 'fixture.txt'}
    for key in ['projectId','secondaryProjectId','proposalProjectId','proposalRetryProjectId','degradedProjectId','conversationId',
                'ambiguousConversationId','existingConversationId','staleConversationId','forbiddenConversationId','unavailableConversationId',
                'folderId','secondaryFolderId','proposalFolderId','workspaceId','fileReferenceId','secondaryFileReferenceId','proposalFileReferenceId',
                'deniedFileReferenceId','memoryReferenceId','correlationId','taskId','idempotencyKey']:
        values[key] = 'owned-' + key.lower()
    values.update(projectId='project-a', secondaryProjectId='project-b', folderId='folder-project-a')
    request = urllib.request.Request(fixture_url + '/_fixtures/graphs', json.dumps(values).encode(), {'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=10) as response:
        if response.status != 200: raise RuntimeError('Test-owned observation graph registration failed.')
    foreign = dict(values, graphId=fixture['IsolationId'] + '-foreign', tenantId=fixture['ForeignTenant'], principalId=fixture['Browser']['ForeignActor'])
    request = urllib.request.Request(fixture_url + '/_fixtures/graphs', json.dumps(foreign).encode(), {'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=10) as response:
        if response.status != 200: raise RuntimeError('Foreign test-owned observation graph registration failed.')
    browser_env = env.copy()
    browser_env.update(E2E_LIVE_APPHOST='1', E2E_SUPPORTED_READ_APPHOST='1', E2E_RUN_ID=fixture['IsolationId'],
                       BASE_URL=endpoint(graph, 'projects-ui'), API_URL=endpoint(graph, 'projects'),
                       EVENTSTORE_API_URL=endpoint(graph, 'eventstore'), FIXTURE_API_URL=fixture_url,
                       KEYCLOAK_URL=fixture['Browser']['Origin'], KEYCLOAK_CLIENT_ID='supported-read-bootstrap',
                       E2E_FOREIGN_USERNAME='foreign-actor', E2E_FOREIGN_PASSWORD=fixture['Browser']['Password'],
                       KEYCLOAK_REALM='hexalith', TEST_USER_USERNAME='actor-1', TEST_USER_PASSWORD=fixture['Browser']['Password'],
                       E2E_BROWSER_SESSION_FILE=str(private / 'browser-session.json'), TEST_TENANT_ID=fixture['Tenant'], TEST_PRINCIPAL_ID=fixture['Browser']['PrimaryActor'], NODE_EXTRA_CA_CERTS=str(private / 'certificate.pem'))
    command = ['npx', 'playwright', 'test', 'specs/projects-supported-read-browser.spec.ts', '--project', 'chromium', '--workers', '1', '--retries', '0']
    report['commands'].append(command)
    protected = [fixture['Browser']['Password'], fixture['Browser']['ClientSecret'], fixture['Browser']['TenantToken'], fixture['Browser']['ForeignToken'],
                 'Synthetic project project-a', 'Synthetic descriptive metadata', 'Restricted legacy description']
    monitor = _BrowserTelemetry(root, protected, group_members)
    interval = {}
    try:
        monitor.start()
        interval['startedAtUtc'] = monitor.now()
        result = run_test_process(command, env=browser_env, timeout=600, cleanup=report.setdefault('testProcessCleanup', []), cwd=root / 'tests/e2e')
    finally:
        interval['endedAtUtc'] = monitor.now()
        telemetry = monitor.finish(interval)
        report['browserTelemetryReaderCleanup'] = [value['cleanup'] for value in telemetry['resources'].values()]
        (evidence / 'browser-telemetry.json').write_text(json.dumps(telemetry, indent=2) + '\n')
        (private / 'browser-session.json').unlink(missing_ok=True)
    (evidence / 'browser.log').write_text(result.stdout)
    import shutil
    junit = root / 'tests/e2e/test-results/junit.xml'
    if junit.exists(): shutil.copyfile(junit, evidence / 'browser.xml')
    results = root / 'tests/e2e/test-results'
    for receipt in [*results.glob('*/supported-read-*.json'), *results.glob('*/attachments/supported-read-*.json')]:
        shutil.copyfile(receipt, evidence / receipt.name)
    if result.returncode:
        raise RuntimeError('Real authenticated supported browser assertions failed; inspect browser.log.')
    if not telemetry['completeIntervalObserved'] or not telemetry['protectedMarkerAbsent']:
        raise RuntimeError('Browser telemetry interval coverage or protected-marker absence failed; raw logs were not retained.')
    report['browserProof'] = {'realmSha256': fixture['Browser']['RealmSha256'], 'keycloakImageId': fixture['Browser']['ImageId'], 'realOidc': True, 'realOwners': True, 'persistedObservation': True, 'workers': 1, 'manualScreenReader': 'pending'}
    hold = os.environ.get('SUPPORTED_READ_MANUAL_HOLD')
    if hold:
        # Only after every automated assertion passed: a person inspects the same seeded runtime with the
        # same fixture sign-in. Nothing during the hold is observed or counted as automated proof.
        seconds = int(os.environ.get('SUPPORTED_READ_MANUAL_HOLD_SECONDS', MANUAL_HOLD_DEFAULT_SECONDS))
        access = {'uiUrl': endpoint(graph, 'projects-ui') + '/inspect/projects', 'identityProviderOrigin': fixture['Browser']['Origin'],
                  'username': 'actor-1', 'password': fixture['Browser']['Password'], 'tenant': fixture['Tenant'],
                  'safeMetadataProject': 'project-a', 'quarantinedProject': 'legacy-a'}
        print('Supported Project reads: manual hold active; create the release file in the hold directory to stop.', flush=True)
        report['manualHold'] = manual_hold(hold, access, seconds=seconds)
