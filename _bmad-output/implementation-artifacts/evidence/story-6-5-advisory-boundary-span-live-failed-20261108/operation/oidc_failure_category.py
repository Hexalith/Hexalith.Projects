"""Closed metadata only; command output and owned identities stay in RAM."""
from pathlib import Path
import re


def created_owned_oidc(command, environment, output):
    if not isinstance(command, list) or command[:2] != ['docker', 'create']:
        return None
    namespace = environment.get('NAMESPACE') if isinstance(environment, dict) else None
    if not isinstance(namespace, str) or re.fullmatch(r'reads-[0-9a-f]{24}', namespace) is None:
        return None
    for option in ['--cidfile', '--name', '--label']:
        if command.count(option) != 1 or command.index(option) + 1 >= len(command):
            return None
    if command[command.index('--name') + 1] != namespace + '-oidc':
        return None
    if command[command.index('--label') + 1] != 'hexalith.supported-reads=' + namespace:
        return None
    cidfile = Path(command[command.index('--cidfile') + 1])
    if not cidfile.is_absolute() or cidfile.name != 'oidc.cid' or not cidfile.parent.name.startswith('supported-project-reads-private-'):
        return None
    if 'quay.io/keycloak/keycloak:26.6' not in command or 'start-dev' not in command:
        return None
    identifier = output.strip() if isinstance(output, str) else None
    return (identifier, namespace) if isinstance(identifier, str) and re.fullmatch(r'[0-9a-f]{64}', identifier) else None


def classify_reported_output(output):
    if not isinstance(output, str) or not output:
        return 'output-unavailable'
    # Inspect only a bounded prefix. No message/fragment/hash/length is persisted.
    text = output[:16384].casefold()
    for category, fragments in [
        ('port-bind-reported', ['port is already allocated', 'address already in use']),
        ('storage-space-reported', ['no space left on device', 'disk quota exceeded']),
        ('mount-path-reported', ['invalid mount config', 'mount source path does not exist']),
        ('permission-denied-reported', ['permission denied', 'operation not permitted']),
        ('daemon-unavailable-reported', ['cannot connect to the docker daemon', 'error during connect']),
        ('runtime-task-reported', ['failed to create task', 'failed to create shim task', 'oci runtime create failed']),
    ]:
        if any(fragment in text for fragment in fragments):
            return category
    return 'unclassified'


def owned_start_failure(failure, command, environment, owned):
    if not isinstance(failure, RuntimeError) or owned is None or not isinstance(command, list) or len(command) != 3:
        return None
    identifier, namespace = owned
    if command != ['docker', 'start', identifier] or not isinstance(environment, dict) or environment.get('NAMESPACE') != namespace:
        return None
    match = re.fullmatch(r'Command failed \((-?[0-9]+)\): docker start ([0-9a-f]{64})', str(failure))
    if match is None or match[2] != identifier or int(match[1]) == 0:
        return None
    return {'diagnosticOnly': True, 'featureAcceptance': False, 'ownedRole': 'oidc',
            'category': classify_reported_output(getattr(failure, 'command_output', None)),
            'dockerStartExitCode': int(match[1])}
