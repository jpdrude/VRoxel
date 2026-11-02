# VRoxel installer

`VRoxel.iss` (Inno Setup 6.5 or later, which downloads files while installing) installs VRoxel with its optional features as components:

| Component | Needs | What it adds |
|---|---|---|
| MCP server and bridge | – | `MCP\vroxel-mcp.exe` and VRoxel's bridge port (8765, this computer only): LLM clients control VRoxel |
| Assistant terminal | MCP | LLM tools such as Claude Code in a terminal from the Esc menu, mirrored in VR. Needs Windows Terminal and the tool itself; the installer says so if it doesn't find them |
| Push to talk | Assistant | Speech to text with Whisper on this computer: whisper.cpp's server for an NVIDIA card (CUDA, 685 MB) or the processor (9 MB), and the speech model (465 MB), all downloaded while installing and checked against their hashes; updates keep the model |

Each component can be left out on its own (`checkablealone`). Where Whisper runs is a choice on the "Additional tasks" page, shown with push to talk; a first installation without an NVIDIA driver starts with the processor. whisper.cpp needs Microsoft's Visual C++ runtime (x64, 14.40 or newer): where it is missing or older, the installer downloads Microsoft's `vc_redist.x64.exe` and installs it, asking for an administrator on an installation for the current user only.

The installer is a 32-bit setup installing to the 64-bit Program Files, like the earlier VRoxel installers: they registered in the 32-bit part of the registry, where it finds them and updates them in place.

For silent installations: `/COMPONENTS="mcp,mcp\assistant,mcp\assistant\speech"` and `/TASKS="whispercuda"` (or `whispercpu`, and `claudedesktop` to connect Claude Desktop), with `/CURRENTUSER` or `/ALLUSERS`.

The installer writes what it installed to `InstalledFeatures.json` next to `VRoxel.exe`, for all users of the computer; VRoxel reads it (`Assets/Scripts/OptionalFeatures/OptionalFeatures.cs`). Each user's settings stay in `optionalFeatures.json` in their VRoxel data folder (`%USERPROFILE%\AppData\LocalLow\dMA Hannover\VRoxel`).

The animated avatars (the Mixamo animation library) are part of every installation, so animations saved by LLMs (`.vrxlanim`) can always be imported in the character menu. Mixamo's animations may be used in VRoxel but not handed out as files, which is why they aren't in this repository.

## LLM clients

The MCP server is a standard stdio server. Clients start it with this entry in their `mcpServers` configuration (`vroxel-mcp --print-config` prints it with the installed path):

```json
{ "mcpServers": { "vroxel": { "command": "C:\\Program Files\\VRoxel\\MCP\\vroxel-mcp.exe" } } }
```

- **Claude Desktop**: the installer's task "Connect Claude Desktop to VRoxel" adds it (`vroxel-mcp --register claude-desktop`), uninstalling removes it again. Claude Desktop needs a restart to see it.
- **The assistant terminal** writes it for Claude Code (`.mcp.json`) and Gemini CLI (`.gemini/settings.json`) into its working folder, where they are missing.
- **Others** (Cursor, Claude Code elsewhere, ...): add the entry above to their configuration, e.g. `claude mcp add --scope user vroxel -- "C:\Program Files\VRoxel\MCP\vroxel-mcp.exe"`.

VRoxel has to be running for the tools to work; the server reaches it on `127.0.0.1:8765`.

## Building the installer

1. **Build VRoxel** in Unity (File > Build Settings) to `..\Build\VRoxel` next to this repository.
2. **MCP server** (needs Python with `pip install -r Tools/mcp/requirements.txt pyinstaller`):
   `python Installer/build_mcp_server.py` builds `Installer/packages/mcp` and checks it starts.
3. **Compile**: open `Installer/VRoxel.iss` in Inno Setup and compile, or `ISCC Installer\VRoxel.iss`. The setup goes to `Installer/Output/VRoxel_Setup_<version>.exe`. Alternatively `..\Build\Setup\VRoxel_CreateSetup.iss` compiles this same script from there, with the license and readme in that folder, and writes the setup next to it.

Whisper isn't part of the setup, so installing push to talk needs internet. For a setup that works without: `python Installer/prepare_whisper.py` fetches both builds, the model and the Visual C++ runtime into `Installer/packages` (`--cuda-from Whisper` takes the CUDA build and the model from the project's `Whisper` folder instead), and `ISCC /DOffline Installer\VRoxel.iss` puts them in; that setup is about 1.2 GB larger.

Paths can be set on the command line: `/DBuildDir=`, `/DPackagesDir=`, `/DSetupDir=` (license and readme, default `..\Build\Setup`), `/DOutputDir=`, `/DMyAppVersion=`. A setup over about 2 GB needs `/DDiskSpanning=yes`, which adds `.bin` files next to the setup.

`Installer/packages`, `Installer/build` and `Installer/Output` are not committed.
