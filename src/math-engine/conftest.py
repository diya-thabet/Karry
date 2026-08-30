"""Ensures the project root is importable by pytest regardless of invocation directory."""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
