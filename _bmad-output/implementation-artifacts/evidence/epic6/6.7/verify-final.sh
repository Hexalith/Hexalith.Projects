#!/usr/bin/env bash
set -eu
STORY67_ATTEMPT=-verified2 python3 _bmad-output/implementation-artifacts/evidence/epic6/6.7/verify-server.py
STORY67_ATTEMPT=-verified2 bash _bmad-output/implementation-artifacts/evidence/epic6/6.7/verify.sh .Contracts .Client .UI
