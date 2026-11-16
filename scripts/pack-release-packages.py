#!/usr/bin/env python3
"""Stable command line for the statically imported package checks."""
import subprocess
import sys
from _pack_release_packages import main

if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, subprocess.CalledProcessError, ValueError) as error:
        print(f"Package check failed: {error}", file=sys.stderr)
        sys.exit(1)
