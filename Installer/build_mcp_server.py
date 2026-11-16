"""Builds VRoxel's MCP server (Tools/mcp/vroxel_mcp.py) into a program that needs no Python on the user's machine:
Installer/packages/mcp/vroxel-mcp.exe with its libraries, which VRoxel.iss installs as MCP next to VRoxel.exe.

Needs Python with the server's requirements (Tools/mcp/requirements.txt) and PyInstaller:
    pip install -r Tools/mcp/requirements.txt pyinstaller
    python Installer/build_mcp_server.py
"""
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

installer = Path(__file__).resolve().parent
root = installer.parent
server = root / "Tools" / "mcp" / "vroxel_mcp.py"
# PyInstaller's work files outside the repository, which may be synced (OneDrive locks them)
work = Path(tempfile.gettempdir()) / "vroxel-mcp-build" / "work"
dist = Path(tempfile.gettempdir()) / "vroxel-mcp-build" / "dist"
target = installer / "packages" / "mcp"

try:
    import PyInstaller  # noqa: F401
except ImportError:
    sys.exit("PyInstaller is missing: pip install pyinstaller")

# One folder rather than one file: starts faster, as nothing is unpacked on each start
subprocess.run([
    sys.executable, "-m", "PyInstaller", "--noconfirm", "--clean", "--onedir", "--console",
    "--name", "vroxel-mcp",
    "--add-data", f"{server.parent / 'examples'};examples",
    "--distpath", str(dist), "--workpath", str(work), "--specpath", str(work),
    str(server),
], check=True)

# The old build's files go; a synced folder may keep empty directories locked, which the new build is copied into
if target.exists():
    shutil.rmtree(target, ignore_errors=True)
shutil.copytree(dist / "vroxel-mcp", target, dirs_exist_ok=True)

# The built server has to start and find its tools and examples
check = subprocess.run([str(target / "vroxel-mcp.exe"), "--list-tools"], capture_output=True, text=True)
print(check.stdout.strip() or check.stderr.strip())
if check.returncode != 0:
    sys.exit("The built MCP server does not start")
print("MCP server ready in", target)
