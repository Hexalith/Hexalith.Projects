"""Read-only audit; optionally recheck a signed archive downloaded by the recorded command."""
from pathlib import Path
import argparse, base64, collections, gzip, hashlib, json, re, subprocess
import xml.etree.ElementTree as ET
import zipfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--archive', type=Path, help='Official signed 4.38.1 nupkg; omit for retained-evidence/source audit only')
args = parser.parse_args()
E = Path(__file__).resolve().parent
R = E.parents[4]
assert (R/'.gitmodules').is_file(), 'Keep this receipt under its owning workspace evidence path.'
P = R/'references/Hexalith.Platform'
B = R/'references/Hexalith.Builds'
SHA = '16ff70a2321ad576faaac3fe53088c2f1bdacae3'
OLD_PLATFORM = 'a5e1389fe2c97b14e8c697a2e54f45631d07ef95'
CURRENT_PLATFORM = '8de90958bc41b7fd01b7ed22b8ab364447a800e4'
OLD_OWNER_BUILDS = '51a71cb1cfe860d5fc135eb3b8521fd80c413810'
CURRENT_OWNER_BUILDS = '9f85d8e66e5c16675cee8c224f1fd88430d96007'
STAGED_BUILDS = '1745ea0f09df9e518aeea3eeb1a7df836f67e09d'
CHECKOUT_BUILDS = 'abc0e1be8069d1c7a567515a13bb015adc407536'
commands = json.loads((E/'commands.json').read_text())
def command(name):
    found = [x for x in commands if x['name'] == name]
    assert len(found) == 1, name
    return found[0]
def readlog(name):
    file = E/command(name)['stdout']
    payload = file.read_bytes()
    return gzip.decompress(payload) if file.suffix == '.gz' else payload

def git(root, *argv):
    return subprocess.check_output(['git', *argv], cwd=root)

def sha256(data):
    return hashlib.sha256(data).hexdigest()

baseline = json.loads((E/'reviewed-source-baseline.json').read_text())
identity = json.loads((E/'official-tool-identity.json').read_text())
release = json.loads(readlog('builds-4381-release'))
asset = next(x for x in release['assets'] if x['name'] == 'g4-tool-package-inventory.json')
inventory_bytes = (E/asset['name']).read_bytes()
assert 'sha256:'+sha256(inventory_bytes) == asset['digest']
assert len(inventory_bytes) == asset['size']
assert release['tag_name'] == 'v4.38.1' and not release['draft'] and not release['prerelease']
assert SHA.encode()+b'\trefs/tags/v4.38.1\n' in readlog('remote-Builds-tags')
inv = json.loads(inventory_bytes)
assert inv['version'] == '4.38.1'
for key in ['packageBuild', 'sourceValidation', 'controls']:
    assert inv['qualification'][key]['result'] == 'passed'
qualification = inv['qualification']
assert qualification['sourceTree']['revision'] == SHA
assert qualification['sourceTree']['clean'] is True
assert qualification['releaseEligible'] is True
assert qualification['ineligibilityReasons'] == []
runs = json.loads(readlog('builds-4381-runs'))['workflow_runs']
for name in ['CI', 'Release']:
    assert any(x['head_sha'] == SHA and x['name'] == name and x['conclusion'] == 'success' for x in runs)
assert identity['repository_commit'] == SHA and identity['version'] == '4.38.1'
feed = json.loads(readlog('nuget-cli-4381-feed-catalog'))
assert identity['signed_archive_sha512'] == feed['packageHash']
assert identity['signed_archive_bytes'] == feed['packageSize']
assert command('builds-4381-nuget-verify')['exit_code'] == 0
if args.archive is not None:
    archive_bytes = args.archive.read_bytes()
    assert sha256(archive_bytes) == identity['signed_archive_sha256']
    assert base64.b64encode(hashlib.sha512(archive_bytes).digest()).decode() == feed['packageHash']
    assert len(archive_bytes) == feed['packageSize']
    with zipfile.ZipFile(args.archive) as archive:
        nuspec = archive.read(next(x for x in archive.namelist() if x.endswith('.nuspec')))
        repo = next(x for x in ET.fromstring(nuspec).iter() if x.tag.endswith('repository'))
        assert repo.attrib['commit'] == SHA
for relative, expected in baseline['source_sha256'].items():
    assert sha256((R/relative).read_bytes()) == expected, 'Source changed: '+relative
story_relative = '_bmad-output/implementation-artifacts/spec-6-4-resolve-projects-with-transient-current-explanations.md'
story = (R/story_relative).read_bytes()
assert sha256(story[:baseline['original_story_before_append_bytes']]) == baseline['original_story_before_append_sha256']
frozen = re.search(rb'<frozen-after-approval.*?</frozen-after-approval>', story, re.S).group()
assert sha256(frozen) == baseline['original_story_frozen_sha256'] == '78e8a29557f949b064db4111f8d487ff4c4d15f3f7d57284e72905f797b9a7df'
assert json.loads((P/'.config/dotnet-tools.json').read_text())['tools']['hexalith.builds.module.cli']['version'] == '4.38.1'
assert json.loads((P/'hexalith-platform.json').read_text())['toolVersionRange'] == '[4.38.1, 5.0.0-0)'
assert git(B, 'rev-parse', 'v4.38.1^{commit}').decode().strip() == SHA
assert git(P, 'rev-parse', 'HEAD:references/Hexalith.Builds').decode().strip() == CURRENT_OWNER_BUILDS != SHA
assert command('platform-gitlink-patch-check')['exit_code'] == 0
preservation = {}
for name, path in [('projects',R), ('platform',P), ('builds',B), ('conversations',R/'references/Hexalith.Conversations')]:
    assert git(path,'rev-parse','HEAD') == readlog('before-'+name+'-head')
    assert git(path,'ls-files','--stage') == readlog('before-'+name+'-index')
    if name in ['builds', 'conversations']:
        assert git(path,'diff','--binary','--ignore-submodules=all') == readlog('before-'+name+'-diff')
    preservation[name] = {'head_unchanged':True, 'index_unchanged':True}
# Initial parent verification proved the root had only the pre-existing story diff.
# The source/prefix checks above bind it, while this check rejects other tracked root edits.
assert git(R,'diff','--binary','--ignore-submodules=all','--','.',':(exclude)'+story_relative) == b''
for name, path in [('platform',P), ('builds',B)]:
    assert git(path,'show-ref') == readlog('before-'+name+'-refs')
assert git(P,'show-ref','--tags','-d') == readlog('remote-Platform-tags').replace(b'\t',b' ')
remote = readlog('remote-Platform-tags').decode()
assert OLD_PLATFORM not in remote and CURRENT_PLATFORM not in remote

# Check the exact public-command observations, including both distinct HXW011 causes.
assert command('package-reviewed')['exit_code'] == 2
package = json.loads(readlog('package-reviewed'))
assert package['status'] == 'failed' and package['outcome']['ruleId'] == 'HXW008'
original_root = str(Path(command('package-reviewed')['cwd']).parents[1])
platform_path = original_root+'/references/Hexalith.Platform'
builds_path = original_root+'/references/Hexalith.Builds'
expected_diagnostics = collections.Counter([
 ('HXW008',platform_path,f"Platform checkout '{platform_path}' is {CURRENT_PLATFORM}; staged composition gitlink is {OLD_PLATFORM}."),
 ('HXW011',platform_path,f"Platform identity {OLD_PLATFORM} has no local v<SemVer> release tag."),
 ('HXW011',platform_path,f"Platform checkout '{platform_path}' has changes."),
 ('HXW009',builds_path,f"Builds catalog '{builds_path}' is staged {STAGED_BUILDS}, checked out {CHECKOUT_BUILDS}, dirty; Platform {OLD_PLATFORM} pins {OLD_OWNER_BUILDS}."),
])
observed_diagnostics = collections.Counter((d['ruleId'], d['field'], d['message']) for d in package['diagnostics'])
assert observed_diagnostics == expected_diagnostics
assert command('source-reviewed')['exit_code'] == 2
source = json.loads(readlog('source-reviewed'))
assert source['status'] == 'failed' and source['outcome']['ruleId'] == 'HXW004'
first = source['diagnostics'][0]
assert first['field'] == original_root+'/references/Hexalith.Commons'
assert "Checkout HEAD '3edfbdafc7d4fa915286dce8a37dc4cc7c06085a'" in first['message']
assert "staged gitlink '1663028fb6d9f46a0bf17919edeb21bb99be1587'" in first['message']
xml = {}
for name in ['builds-focused', 'platform-inventory', 'platform-pin']:
    document = ET.parse(E/(name+'.xml')).getroot()
    assemblies = document.findall('assembly')
    xml[name] = {k:sum(int(x.attrib.get(k,0)) for x in assemblies) for k in ['total','passed','failed','skipped','errors','not-run']}
    if name == 'platform-pin':
        failures = document.findall('.//test[@result="Fail"]')
        assert len(failures) == 1
        assert failures[0].attrib['method'] == 'AcceptedToolPinEqualsSupportedRangeFloorAsync'
        assert failures[0].attrib['type'] == 'Hexalith.Platform.Composition.Tests.DomainEnrollmentKitTests'
        message = failures[0].find('failure/message').text
        assert "fatal: ambiguous argument 'v4.38.1^{commit}'" in message
        assert 'unknown revision or path not in the working tree' in message
assert xml['builds-focused'] == {'total':88,'passed':88,'failed':0,'skipped':0,'errors':0,'not-run':0}
assert command('builds-identity-preparation-catalog-tests')['exit_code'] == 0
assert xml['platform-inventory'] == {'total':1,'passed':1,'failed':0,'skipped':0,'errors':0,'not-run':0}
assert command('platform-existing-pin-test')['exit_code'] == 1
assert xml['platform-pin'] == {'total':1,'passed':0,'failed':1,'skipped':0,'errors':0,'not-run':0}
print(json.dumps({'read_only_audit':'passed','archive_bytes_reverified':args.archive is not None,'retained_inventory_asset_digest':asset['digest'],'retained_release_qualification_source':SHA,'source_hashes_match_reviewed_baseline':True,'original_story_prefix_and_frozen_block_unchanged':True,'preservation':preservation,'tests':xml,'package_recorded_exit':command('package-reviewed')['exit_code'],'package_diagnostic_instances':len(package['diagnostics']),'source_recorded_exit':command('source-reviewed')['exit_code'],'source_first_blocker':'HXW004','owner_pin_acceptance':False,'platform_integration_qualified':False},indent=2))
