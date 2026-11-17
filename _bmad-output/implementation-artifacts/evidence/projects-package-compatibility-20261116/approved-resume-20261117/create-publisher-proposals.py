"""Prepare workflow proposals from exact owner Git bytes; no remote mutation."""
from pathlib import Path
import subprocess,sys
from urllib.request import urlopen
ROOT=Path(__file__).resolve().parent
owner=sys.argv[1]
assert len(owner)==40 and set(owner)<=set('0123456789abcdef')
for profile,repo in [('eventstore','/home/administrator/projects/hexalith/projects/references/Hexalith.EventStore'),('projects','/home/administrator/projects/hexalith/projects')]:
    base=subprocess.check_output(['git','ls-remote','origin','refs/heads/main'],cwd=repo,text=True).split()[0]
    repository='Hexalith.EventStore' if profile=='eventstore' else 'Hexalith.Projects'
    with urlopen('https://raw.githubusercontent.com/Hexalith/'+repository+'/'+base+'/.github/workflows/release.yml',timeout=30) as response:
        original=response.read(1024*1024).decode()
    assert original.count('  workflow_dispatch:\n')==1
    revised=original.replace('  workflow_dispatch:\n','''  workflow_dispatch:
    inputs:
      release-profile:
        description: Full release or the fixed owner-approved Contracts prerequisite
        type: choice
        default: full
        options:
          - full
          - contracts-prerequisite
          - contracts-prerequisite-verify
''',1)
    assert revised.count('  verify-source:\n')==1
    revised=revised.replace('  verify-source:\n',"  verify-source:\n    if: ${{ inputs.release-profile != 'contracts-prerequisite' && inputs.release-profile != 'contracts-prerequisite-verify' }}\n",1)
    account='NUGET_TRUSTED_PUBLISHING_USER' if profile=='eventstore' else 'NUGET_USER'
    jobs='''
  qualify-contracts-prerequisite:
    if: ${{ github.ref == 'refs/heads/main' && (inputs.release-profile == 'contracts-prerequisite' || inputs.release-profile == 'contracts-prerequisite-verify') }}
    runs-on: ubuntu-latest
    timeout-minutes: 10
    permissions:
      contents: read
    outputs:
      plan: ${{ steps.qualify.outputs.plan }}
    steps:
      - name: Initialize qualification failure receipt
        run: >-
          printf '%s\\n' '{"status":"failed","failed_operation":"workflow initialization","reason":"qualification has not completed","push_attempted":false,"upload_count":0}' > prerequisite-qualification.json
      - name: Checkout immutable CiCd prerequisite verifier and approved archives
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          repository: Hexalith/Hexalith.CiCd
          ref: OWNER
          path: .prerequisite-owner
          submodules: false
          persist-credentials: false
          sparse-checkout: |
            tools/publish_contracts_prerequisite.py
            tools/test_publish_contracts_prerequisite.py
            docs/contracts-prerequisites
          sparse-checkout-cone-mode: false
      - name: Exercise fixed publisher controls without publication credentials
        run: python3 -B .prerequisite-owner/tools/test_publish_contracts_prerequisite.py
      - name: Initialize package signature verifier for read-only recovery
        uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          dotnet-version: '11.0.x'
      - name: Bind exact approved payload and package-only owner gates
        id: qualify
        env:
          GH_TOKEN: ${{ github.token }}
          PREREQUISITE_OWNER_SOURCE: OWNER
          HEXALITH_RELEASE_PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}
          PREREQUISITE_OPERATION: ${{ inputs.release-profile == 'contracts-prerequisite-verify' && 'verify' || 'qualify' }}
        run: python3 -I .prerequisite-owner/tools/publish_contracts_prerequisite.py "$PREREQUISITE_OPERATION" --profile PROFILE --report prerequisite-qualification.json
      - name: Retain exact-source prerequisite qualification
        if: ${{ always() }}
        uses: actions/upload-artifact@cf430e030ddbb5b0abf93d22962f4752f3646cd9 # v7.0.2
        with:
          name: contracts-prerequisite-qualification-${{ github.run_id }}-${{ github.run_attempt }}
          path: prerequisite-qualification.json
          if-no-files-found: error
          retention-days: 90

  publish-contracts-prerequisite:
    needs: qualify-contracts-prerequisite
    if: ${{ !cancelled() && github.ref == 'refs/heads/main' && inputs.release-profile == 'contracts-prerequisite' && needs.qualify-contracts-prerequisite.result == 'success' && needs.qualify-contracts-prerequisite.outputs.plan != '' }}
    runs-on: ubuntu-latest
    timeout-minutes: 15
    environment: production
    permissions:
      contents: read
      id-token: write
    steps:
      - name: Initialize publication failure receipts
        run: |
          printf '%s\\n' '{"status":"failed","failed_operation":"workflow initialization","reason":"pre-OIDC qualification has not completed","push_attempted":false,"upload_count":0}' > prerequisite-pre-oidc.json
          printf '%s\\n' '{"status":"failed","failed_operation":"workflow initialization","reason":"publisher has not completed","push_attempted":false,"upload_count":0}' > prerequisite-publication.json
      - name: Checkout only immutable CiCd prerequisite publisher and approved archives
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
        with:
          repository: Hexalith/Hexalith.CiCd
          ref: OWNER
          path: .prerequisite-owner
          submodules: false
          persist-credentials: false
          sparse-checkout: |
            tools/publish_contracts_prerequisite.py
            docs/contracts-prerequisites
          sparse-checkout-cone-mode: false
      - name: Initialize package signature verifier
        uses: actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6.0.0
        with:
          dotnet-version: '11.0.x'
      - name: Requalify immediately before OIDC exchange
        env:
          GH_TOKEN: ${{ github.token }}
          PREREQUISITE_OWNER_SOURCE: OWNER
          HEXALITH_RELEASE_PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}
        run: python3 -I .prerequisite-owner/tools/publish_contracts_prerequisite.py qualify --profile PROFILE --report prerequisite-pre-oidc.json
      - name: Exchange existing owner release identity for temporary NuGet key
        id: nuget-login
        uses: NuGet/login@8d196754b4036150537f80ac539e15c2f1028841 # v1.2.0
        with:
          user: ${{ vars.ACCOUNT }}
      - name: Publish the single approved archive and verify signed public payload
        env:
          GH_TOKEN: ${{ github.token }}
          PREREQUISITE_OWNER_SOURCE: OWNER
          HEXALITH_RELEASE_PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}
          PREREQUISITE_QUALIFICATION: ${{ needs.qualify-contracts-prerequisite.result }}
          PREREQUISITE_PLAN: ${{ needs.qualify-contracts-prerequisite.outputs.plan }}
          NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}
        run: python3 -I .prerequisite-owner/tools/publish_contracts_prerequisite.py publish --profile PROFILE --report prerequisite-publication.json
      - name: Retain publication outcome and verification
        if: ${{ always() }}
        uses: actions/upload-artifact@cf430e030ddbb5b0abf93d22962f4752f3646cd9 # v7.0.2
        with:
          name: contracts-prerequisite-publication-${{ github.run_id }}-${{ github.run_attempt }}
          path: |
            prerequisite-pre-oidc.json
            prerequisite-publication.json
          if-no-files-found: error
          retention-days: 90
'''.replace(': OWNER', ': '+owner).replace('PROFILE',profile).replace('ACCOUNT',account)
    revised+=jobs
    out=ROOT/'publisher-workflows'/profile;out.mkdir(parents=True,exist_ok=True)
    (out/'before.yml').write_text(original)
    (out/'release.yml').write_text(revised)
    (out/'base.txt').write_text(base+'\n')
    assert revised.replace(jobs,'').replace("    if: ${{ inputs.release-profile != 'contracts-prerequisite' && inputs.release-profile != 'contracts-prerequisite-verify' }}\n",'',1).replace('''    inputs:
      release-profile:
        description: Full release or the fixed owner-approved Contracts prerequisite
        type: choice
        default: full
        options:
          - full
          - contracts-prerequisite
          - contracts-prerequisite-verify
''','',1)==original
    print(profile,base)
