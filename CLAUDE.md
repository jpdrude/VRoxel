# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VRoxel is a Unity 2021.3.45f2 VR application for 3D block-based assembly and geometry editing, primarily targeting HTC Vive via SteamVR. Users place, remove, and manipulate voxel-like blocks in VR space; assemblies are saved in the custom `.vrxl` format.

## Development Environment

- **Unity Version:** 2021.3.45f2 LTS
- **Platform:** Windows, VR (HTC Vive / OpenVR / SteamVR)
- **IDE:** Rider or Visual Studio (`.vsconfig` present)
- Build via Unity Editor (File > Build Settings) to `..\Build\VRoxel`; the installer is built from there (`Installer/README.md`)
- No automated test runner commands; tests use Unity Test Framework via the Test Runner window
- Python 3 with `Tools/mcp/requirements.txt` for the MCP server during development; Unity must be in Play mode for it to reach VRoxel

## Core Architecture

### Central Data Flow

1. `GameManager` (singleton MonoBehaviour) holds the active `Bond` (assembly configuration) and references the GPU Instancer prefab manager
2. `Bond` (ScriptableObject-like MonoBehaviour) defines block type prefabs, grid spacing, scaling, and offset — changing the active Bond changes the entire assembly grid system
3. `BlockContainer` (singleton) stores all placed blocks in a `Dictionary<PositionKey, PositionEntry>` and pushes Matrix4x4 batches (max 100,000 per batch) to GPU Instancer for rendering
4. User actions go through `SteamVR_AssemblySpawner` (VR) or `TargetSpawner` (desktop), which call `BlockContainer.AddBlock()` / `RemoveBlock()`
5. Every state-changing action is wrapped in a `DoAction` subclass and pushed to `UndoStack` (static, capped at 20)
6. `MessageSystem` (static) surfaces feedback via fade-out UI messages

### Key Subsystems

**Undo/Redo (`Scripts/History/`)**
- `UndoStack` is static with `UnityEvent` hooks (`UndoStackAvailable`, `UndoStackUnavailable`, etc.) that `MenuSwitch` listens to for button enable/disable
- All reversible actions subclass `DoAction` and implement `Undo()` / `Redo()`
- Action types: `DoAddStroke`, `DoRemoveStroke`, `DoAddPartialStroke`, `DoChangeObjectPos`, `DoChangeAnimationPos`, `DoToPointCloud`, `DoFromPointCloud`, `DoLoadAsPointCloud`

**IO Systems (`Scripts/IO/`)**
- `AssemblyIO` — save/load `.vrxl` assemblies using Newtonsoft.Json + bond remapping on load; supports incremental loading via `IncrementalLoader`
- `GeometryIO` — import external meshes (glTF, FBX, etc.) via TriLib; manages spawned geometry instances
- `PointCloudIO` — point cloud visualization using PointCloudViewerDX11 (DX11 runtime viewer)
- `AnimationIO` / `KitBashIO` — animation and kitbash component serialization
- `AutoSave` — periodic automatic save

**VR Input (`Scripts/VR_Input/`)**
- `SpawnerActions` is the abstract base with virtual callbacks: `OnTriggerDown()`, `OnTrigger()`, `OnTriggerUp()`, `OnGripDown()`, etc.
- `SteamVR_AssemblySpawner` handles placement modes (`VRPlacementMode` enum): SpawnInSphere, SpawnInBox, SpawnInCylinder, SpawnInCapsule (and Destroy variants)
- SteamVR action bindings use `SteamVR_Action_Boolean` / `SteamVR_Input_Sources`. VRoxel's action set is `/actions/VRoxel` (Trigger, Up/Down/Left/Right, Teleport, QuickSwitch, PushToTalk); `Assets/StreamingAssets/SteamVR/bindings_*.json` hold default bindings for the Vive wand, Oculus/Meta Touch, Index, WMR and Cosmos controllers. Change actions in Window > SteamVR Input (Save and generate), bindings in those files

**Brushing (`Bond.cs`, `BlockBrush.cs`)**
- `Bond.GetBlocksInShapes` finds the blocks in several brush shapes (sphere, capsule, cylinder, box) in one Burst job, skipping the volume the stroke's previous frame covered; the VR and desktop spawners brush through `BlockBrush`, avatars through `CharacterSpawner`
- `BlockContainer` uploads to the GPU only when blocks changed

**Menu / UI (`Scripts/Menu/`)**
- `MenuSwitch` is the central menu state manager; it wires undo/redo button states to `UndoStack` events and manages player elevation/teleportation
- Geometry, character, and kitbash editing each have dedicated menu scripts (`EditGeometryMenu`, `CharacterEditor`, `KitBashMenu`)
- `MessageSystem` provides static `DisplayMessage()` / `DisplayWarning()` / `DisplayPermanentMessage()` — do not display feedback any other way. It works only once the scene has started, not from `Awake`/`OnEnable` or outside Play mode

**Avatars and MCP (`Scripts/Animation/`, `Scripts/MCP/`, `Tools/mcp/`)**
- Mixamo avatars play clips from `AnimationLibrary` (`Assets/Animation/Mixamo/MixamoLibrary.asset`, built with VRoxel > Rebuild Mixamo Animation Library) through `MixamoAnimationPlayer`; `AvatarSequencer` / `SequencePlanner` script multi-step behaviour, `AvatarBodies` shapes bodies
- `McpBridge` (scene component) serves a localhost TCP port (8765, one JSON object per line); commands are registered by `AvatarCommands`, `BlockCommands`, `SceneCommands`, `AnimationFileCommands` and `SpeechCommands` and run on the main thread in `Update`. It holds the avatar prefab and library, which the character menu's `.vrxlanim` import also uses, so it stays in the scene; it opens its port only when the MCP server is installed and closes it before script reloads
- `Tools/mcp/vroxel_mcp.py` is the MCP server (stdio) that forwards tools to the bridge (`vroxel_bridge.py`); `examples/*.md` are its resources; `.mcp.json` registers it for Claude Code in this project. Installed, it is `MCP\vroxel-mcp.exe` (built by `Installer/build_mcp_server.py`), with `--register claude-desktop`, `--print-config` and `--list-tools`
- MCP undo/redo (`McpHistory`) covers only changes made through the tools; blocks they add or remove go on the user's `UndoStack`

**Optional features (`Scripts/OptionalFeatures/`)**
- `OptionalFeatures` knows which optional features are installed: the MCP server, the assistant terminal (needs MCP) and push to talk (needs the assistant). The installer writes `InstalledFeatures.json` next to `VRoxel.exe`; without it (editor, unpacked build) the flags in the user's `optionalFeatures.json` (in `Application.persistentDataPath`) decide, and in the editor MCP counts as installed. That file also holds each feature's settings
- `OptionalFeatureButton` greys out a button whose feature isn't installed. The `Feature` enum values are stored in the scene by number — never renumber them

**Assistant terminal (`Scripts/Assistant/`)**
- `AssistantTerminal` opens an LLM command line tool (Claude Code, Codex, Gemini CLI, Copilot CLI, OpenCode or a custom command) in Windows Terminal, pinned on top, and remembers its window; `AssistantMenu` is the Esc menu panel. In a build it works in `persistentDataPath/Assistant` and writes the MCP configuration there
- `AssistantTerminalMirror` (on the VR menu) shows the terminal in VR as a SteamVR overlay set into a `StretchableFrame` (9-slice mesh frame read from its pieces' bounding boxes); without SteamVR it shows a preview quad
- `TerminalKeyboard` types into the terminal (paste, keys) and switches the focus on a thread of its own

**Speech to text (`Scripts/SpeechToText/`)**
- `SpeechToText` is push to talk (F8 or the `PushToTalk` action): hold to record, release to paste the transcription unsent, click to send, double click to clear. Whisper runs locally as whisper.cpp's server (`WhisperServer`), from the gitignored `Whisper/` folder in the editor and `Whisper\` (or `Whisper\Release\`) next to `VRoxel.exe` when installed

### Static Access Pattern

Most systems are accessed via static properties:
- `GameManager.Manager` — active bond, GPU instancer reference
- `BlockContainer.Container` — block dictionary, add/remove API
- `UndoStack` — push/undo/redo (all static methods)
- `MessageSystem` — all static

### Rendering

GPU Instancer (v1.8.21) renders all blocks. Transforms are stored as `Matrix4x4[]` arrays batched at 100,000 entries. Point clouds use a separate DX11 viewer (`PointCloudViewerDX11`). Do not add per-block `MonoBehaviour` components — all block state lives in `BlockContainer`'s dictionaries.

## Key Third-Party Assets

| Asset | Location | Purpose |
|---|---|---|
| GPU Instancer | `Assets/GPUInstancer/` | GPU-instanced block rendering |
| TriLib | `Assets/TriLib/` | Runtime 3D mesh import (glTF, FBX, OBJ…) |
| PointCloudTools | `Assets/PointCloudTools/` | DX11 point cloud visualization |
| SteamVR / OpenVR | `Assets/SteamVR/` | VR input and tracking |
| SimpleFileBrowser | `Assets/Plugins/SimpleFileBrowser/` | Runtime file open/save dialogs |
| Xsens MVN Live | `Assets/Xsens/` | Motion capture integration |
| UnityFBXExporter | `Assets/UnityFBXExporter/` | FBX export |
| GameDevWare.Serialization | (DLL) | Binary serialization for point clouds |

## Scenes

- `Assets/Scenes/VRoxelScene.unity` — main application scene (use this for development)
- `Assets/Scenes/xx_stuff/` — test and scratch scenes
- `Assets/Scenes/xx_versions/` — archived version snapshots

## Conventions

- **Grid position** is represented by `PositionKey` (integer X, Y, Z) — never use `Vector3` as a dictionary key for block lookups
- **Bond changes** require re-initializing `BlockContainer` — check `GameManager` logic before modifying Bond loading
- **Serialized assemblies** embed bond metadata; `AssemblyIO` handles remapping when a different bond is active on load
- Test/scratch code lives under `Scripts/xx_Testing/` — keep production scripts out of that folder
- The `Scripts/Extension/` folder holds only pure utility extension methods (no MonoBehaviours)
- **Mixamo data is never committed** (the repository is public and Mixamo's animations may not be redistributed as files): `Assets/Animation/Mixamo/Clips`, `Reference`, `Generated`, `MixamoLibrary.asset`, `Inbox~` and `Reports~` are gitignored. Builds and the installer contain them compiled
- Gitignored local folders: `Whisper/`, `claude_exchange/`, `Installer/packages`, `Installer/build`, `Installer/Output`
- Commit `VRoxelScene.unity` when it carries a feature's setup (components and wiring done in the Inspector); never commit `UserSettings/` layouts
- Hidden helper objects created at runtime use `HideFlags.HideInHierarchy` with `DontDestroyOnLoad`, not `HideAndDontSave`, which would outlive Play mode in the editor; threads, sockets and processes they start must stop before script reloads (`AssemblyReloadEvents.beforeAssemblyReload`)

## Installer

`Installer/VRoxel.iss` (Inno Setup 6.5+) installs VRoxel with the optional features as components (each `checkablealone`, so it can be installed without its sub-components); Whisper (builds and model) is downloaded while installing and hash-checked, CUDA or processor chosen as a task, and Microsoft's Visual C++ runtime is installed where whisper.cpp would miss it. It is a 32-bit setup (like the earlier ones, so it finds and updates them in the 32-bit registry) installing to the 64-bit Program Files. `..\Build\Setup\VRoxel_CreateSetup.iss` compiles it from the build folder. The version is set there (`MyAppVersion`) and in Player Settings (`bundleVersion`), currently 0.500. See `Installer/README.md` for the build steps.
