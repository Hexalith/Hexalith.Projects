#!/usr/bin/env python3
"""Run existing nonpublishing source/release/catalog controls without Git mutations."""
from pathlib import Path
import subprocess,sys
out=Path(__file__).resolve().parent
repo='/tmp/story64-builds-producer-2186b9a4'
controls=[
 ('release-source-controls',['bash','./Tools/test-g4-release-source.sh']),
 ('release-lifecycle-controls',['pwsh','-NoProfile','-File','./Tools/test-g4-release-contract.ps1']),
 ('central-versions',['pwsh','-NoProfile','-File','./Tools/validate-central-package-versions.ps1']),
 ('authoritative-catalog-controls',['pwsh','-NoProfile','-File','./Tools/test-authoritative-package-catalog.ps1']),
 ('central-version-controls',['pwsh','-NoProfile','-File','./Tools/test-central-package-version-validator.ps1']),
 ('package-audit',['pwsh','-NoProfile','-File','./Tools/validate-package-version-audit.ps1']),
 ('consumer-authority',['pwsh','-NoProfile','-File','./Tools/validate-consumer-package-authority.ps1','-RepositoryRoot','.','-CatalogPath','./Props/Directory.Packages.props']),
 ('consumer-authority-controls',['pwsh','-NoProfile','-File','./Tools/test-consumer-package-authority-validator.ps1']),
 ('package-exceptions',['pwsh','-NoProfile','-File','./Tools/validate-package-version-exceptions.ps1','-InventoryPath','./Tools/package-version-exceptions.json','-CatalogPath','./Props/Directory.Packages.props']),
 ('package-exception-controls',['pwsh','-NoProfile','-File','./Tools/test-package-version-exception-validator.ps1']),
 ('platform-catalog',['pwsh','-NoProfile','-File','./Tools/validate-platform-version-catalog.ps1']),
 ('dapr-versions',['pwsh','-NoProfile','-File','./Tools/validate-dapr-package-versions.ps1']),
 ('dapr-version-controls',['pwsh','-NoProfile','-File','./Tools/test-dapr-package-version-validator.ps1']),
 ('workflow-test-platform-controls',['pwsh','-NoProfile','-File','./Tools/test-domain-workflow-test-platforms.ps1']),
 ('commitlint-workflow-controls',['pwsh','-NoProfile','-File','./Tools/test-commitlint-workflow.ps1']),
 ('legacy-publisher-controls',['pwsh','-NoProfile','-File','./Tools/test-publish-packages.ps1']),
]
for name,argv in controls:
 rc=subprocess.run([sys.executable,str(out/'capture.py'),'--output',str(out/name),'--cwd',repo,'--',*argv],check=False).returncode
 print(name,rc,flush=True)
