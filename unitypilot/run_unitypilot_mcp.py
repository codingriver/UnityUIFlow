#!/usr/bin/env python3
"""UnityPilot MCP server launcher — used by Unity Editor to start the MCP bridge."""

import os
import sys

_repo_root = os.path.dirname(os.path.abspath(__file__))
_src = os.path.join(_repo_root, "src")
if _src not in sys.path:
    sys.path.insert(0, _src)

from unitypilot_mcp.mcp_main import _cli

if __name__ == "__main__":
    _cli()
