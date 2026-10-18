# 3.125.1 finite-enrollment planning evidence

The user chose option 2: target published **3.125.1** for the next Story 6.4 prerequisite. This is planning scope; enrollment implementation, exact execution selection, qualification and owner acceptance are not recorded as complete.

`release-source-identities.json` retains exact source/tag, successful workflow and separate Builds identities. `package-provenance.json` inventories all fourteen official NuGet metadata records and five downloaded P1R archives. `nuget-signature-verification.json` records successful verification of those five archives. Full signed archive SHA-512/catalog hashes differ from NuGet signed-content verification hashes; restore/lock contentHash agreement remains unmeasured. Download URLs and hashes permit reproduction; the five archives remain in the temporary collection directory and are not copied here.

`qualification-tools-comparison.json` confirms all 52 local qualification tooling/test files match the immutable released blobs. The large repository tree response is omitted; exact blob identifiers and immutable API URLs remain available. `project-evaluation-before.json` proves host/domain/probe currently lack successor constants at 3.125.1; this evaluation performs no restore or package build. The three existing baseline tests pass and do not exercise a repaired enrollment.

`release-artifact-members/` retains the release artifact's OCI manifests/configuration, publication preflights and passing smoke observations. These are release observations, not independent live registry/layer or P1R execution. Nine other archives/signatures and restored/loaded consumer graphs remain unverified.

The immutable executor requires its source HEAD, selected tag and selected source revision to be identical. A tooling edit cannot inherit this proof. The enrollment prerequisite must preserve that guard and identify a separate owner-controlled tooling/tag binding prerequisite before any 3.125.1 operational qualification. No executable qualification command is approved by this evidence.

The parent Story 6.4 draft and earlier receipt are preserved except for recording the answered choice. Runtime code, dependency pins, gitlinks, acceptance and sprint state are unchanged. `workspace-before.json` and `owner-file-identities-before.json` retain pre-edit identities. Raw evidence uses `-text`; `manifest.json` hashes only the files actually retained here.


## Retained publication source and checkout supplement

The exact original [publication-source receipt](story-6-4-live-publication.json)
is now retained at its existing SHA-256
`1255f4a228ecf600dca372c2aad217bbf762da651ce6cdf37c5fa5ed3bfdbded`.
[Supplemental source link and package-build observation](release-source-supplement.json)
connects the historical temporary-path reference without rewriting
`release-source-identities.json` or its raw observations. The receipt's
`release_31251_job_log` selected observation records the package-build checkout
of Builds `4629ff2d3bd66376b29a553663c241d3a7b87853` at
2026-10-18T13:41:29.0012813Z. Tag provenance, successful CI reusable workflow
`13a3316ad0b93e25bf273efca00be36154b327c2` and release-execution Builds
`22a578b576a515d2af214fe81859447fffc97981` remain distinct coordinates.
This checkout observation does not measure actual archive construction or
restored/locked/loaded graph agreement and grants no execution authority.

Scoped local `.gitignore` exceptions retain the three original `smoke-*.log`
manifest members. `review-0/` preserves the earlier README and manifest bytes;
the mutable manifest now includes all retained members and supplemental files.
Published qualification and Story 6.4 remain blocked.
