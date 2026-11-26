# Replay the accepted 7.1-P2 qualification

The [capability pin](../7.1-P2-capability-pin.json) records the original commands and results. Use [run-qualification.sh](run-qualification.sh) for a fresh run. It verifies the supplied inputs, checks out the recorded source revisions in disposable directories and gives each replay a new output directory.

Use the retained `7-1-p2-replay-inputs.tar.gz` with SHA-256 `fbcfba8b3f2abb2ad5e8c99edf668cd57439ad6003716b3e9d84dd961b906d95`. The archive includes the G-1 and activation launchers, verifier and source overlays, the released Folders packages, the provider JAR, and the original activation source tuple, harness manifest and pre-restart wire receipt. The accepted qualification digest covers the original qualification object.

Run from a Projects checkout with the recorded revisions available locally in its root-declared owner repositories. Supply a Folders checkout containing `bf60ba51e7e665cb99db353652c5a5b6112b9b5c`. The retained launchers use Docker and their pinned local images, the .NET SDK and restore inputs, Git, Bash and Python 3. Choose an existing results parent outside the disposable workspace.

```bash
run-with-temp bash -euc '
printf "%s  %s\n" fbcfba8b3f2abb2ad5e8c99edf668cd57439ad6003716b3e9d84dd961b906d95 "$1" | sha256sum --check
tar -C "$TMPDIR" -xzf "$1"
bash _bmad-output/implementation-artifacts/evidence/epic7/7.1-P2/run-qualification.sh \
    "$TMPDIR/replay-inputs" "$2" "$3"
' _ /path/to/7-1-p2-replay-inputs.tar.gz /path/to/folders /path/to/results-parent
```
