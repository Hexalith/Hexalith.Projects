import os, subprocess
keys = ('PATH', 'HOME', 'USER', 'LOGNAME', 'LANG', 'DOTNET_ROOT',
        'DOTNET_ROOT_X64', 'SSL_CERT_FILE', 'SSL_CERT_DIR')
environment = {key: os.environ[key] for key in keys if key in os.environ}
environment.update(GITHUB_ACTIONS='true', LANG='C.UTF-8')
result = subprocess.run(['/bin/bash', '--noprofile', '--norc',
                         '/tmp/g6-ci-readiness-precheck.sh'], env=environment)
print('CLEAN-SHELL-PRECHECK-EXIT:', result.returncode)
raise SystemExit(result.returncode)
