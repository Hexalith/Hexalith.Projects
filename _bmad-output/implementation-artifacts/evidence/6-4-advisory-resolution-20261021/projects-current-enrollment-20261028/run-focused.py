"""Run existing narrow contracts, advisory, authorization, isolation and HTTP cases."""
from run import *

workspace = WORK / 'projects'
lanes = [('Contracts', ['ProjectAdvisoryContractTests']), ('Server', ['ProjectAdvisoryQueryTests', 'DelegatedProjectAdvisoryConversationDirectoryTests', 'DelegatedProjectDirectoryRegistrationTests', 'ProjectQueryEnvelopePrincipalBindingTests', 'ProjectQueryTenantFilterTests', 'SupportedProjectReadHttpTests'])]
executables = {}
for layer, classes in lanes:
    output = workspace / f'tests/Hexalith.Projects.{layer}.Tests/bin/Debug/net10.0'
    executables[layer] = {str(p.relative_to(output)): digest(p) for p in sorted(output.rglob('*')) if p.is_file()}
(EVIDENCE / 'executables-before.json').write_text(json.dumps(executables, indent=2) + '\n')
for layer, classes in lanes:
    command = ['dotnet', f'tests/Hexalith.Projects.{layer}.Tests/bin/Debug/net10.0/Hexalith.Projects.{layer}.Tests.dll']
    for name in classes:
        command += ['-class', '*' + name]
    command += ['-result-xml', str(EVIDENCE / ('current-' + layer.lower() + '-tests.xml')), '-failSkips']
    run('current-' + layer.lower() + '-tests', command, workspace)
