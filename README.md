# VRoxel

A VR application for block-based 3D assembly and geometry editing, built with Unity 2021.3 LTS for HTC Vive (SteamVR/OpenVR).
Further info at https://drude.io/en-vroxel

Users compose spatial assemblies from discrete block types in VR, save and load them in the `.vrxl` format, import external meshes and point clouds, and animate characters within the scene. The project is research software developed at [your institution].

Current version: **0.510**

## Features beyond block editing

- **Animated avatars**: Mixamo-animated people that walk, sit, climb stairs and ladders, with adjustable bodies; their body can carve or fill blocks as it moves. Scenes of avatars are saved as `.vrxlanim` and imported in the character menu.
- **MCP server** (optional): LLM clients such as Claude Desktop or Claude Code control VRoxel through the [Model Context Protocol](https://modelcontextprotocol.io): spawn and direct avatars, add or remove block volumes, read the scene. `Tools/mcp/` holds the server; VRoxel's side is the `McpBridge` component.
- **Assistant terminal** (optional): opens an LLM command line tool (Claude Code, Codex, Gemini CLI, ...) from the Esc menu, pinned on top of VRoxel and mirrored in VR above the controller menu.
- **Push to talk** (optional): speech to text with [whisper.cpp](https://github.com/ggml-org/whisper.cpp) on this computer, typed into the assistant terminal. F8 or a controller button: hold to talk, click to send, double click to clear.

## Requirements

- Unity **2021.3.45f2** (other 2021.3.x patch releases likely work)
- HTC Vive or compatible OpenVR headset; default controller bindings are included for the Vive wand, Oculus/Meta Touch, Valve Index, Windows Mixed Reality and Vive Cosmos controllers
- Windows 10/11, DirectX 11
- For the optional features: Python 3 with `Tools/mcp/requirements.txt` (MCP server in the editor), Windows Terminal and an LLM command line tool such as Claude Code (assistant terminal), and whisper.cpp's server with a model in `Whisper/` (push to talk; `Installer/prepare_whisper.py --cuda-from` describes the layout)

## Third-party plugins (not included)

Several plugins cannot be redistributed publicly due to their licenses. Purchase/obtain them and import into the project before opening in Unity:

| Plugin | Source | Folder after import |
|---|---|---|
| **GPU Instancer** | [Unity Asset Store](https://assetstore.unity.com/packages/tools/utilities/gpu-instancer-117566) | `Assets/GPUInstancer/` |
| **TriLib 2** | [Unity Asset Store](https://assetstore.unity.com/packages/tools/modeling/trilib-2-model-loading-package-157548) | `Assets/TriLib/` |
| **Point Cloud Viewer & Tools** | [Unity Asset Store](https://assetstore.unity.com/packages/tools/utilities/point-cloud-viewer-and-tools-16019) | `Assets/PointCloudTools/` |
| **GameDevWare Serialization** | [Unity Asset Store](https://assetstore.unity.com/packages/tools/network/messagepack-for-c-unity-72932) | `Assets/Plugins/GameDevWare.Serialization/` |

## Included open-source plugins

These are included in the repository under their respective licenses:

- **SteamVR Plugin** — Valve Software, BSD 3-Clause (`Assets/SteamVR/`)
- **SimpleFileBrowser** — Yasirkula, MIT (`Assets/Plugins/SimpleFileBrowser/`)
- **UnityFBXExporter** — MIT (`Assets/UnityFBXExporter/`)
- **OutlineEffect** — cakeslice, MIT (`Assets/OutlineEffect/`)

## Mixamo animations (not included)

The avatars' animations come from [Mixamo](https://www.mixamo.com), whose terms allow using them in VRoxel but not handing them out as files, so they are not in this repository. `Assets/Animation/Mixamo/ClipChecklist.md` and `MixamoManifest.csv` list the clips to download; put them in `Assets/Animation/Mixamo/Inbox~`, sort them in with VRoxel > Mixamo Inbox, and run VRoxel > Rebuild Mixamo Animation Library. Builds and the installer contain them in compiled form.

## Setup

1. Clone this repository
2. Purchase and import the paid plugins listed above via the Unity Package Manager or by dragging the `.unitypackage` files into the project
3. For avatars, add the Mixamo animations as described above
4. Open the project in Unity 2021.3.45f2
5. Open `Assets/Scenes/VRoxelScene.unity`
6. Ensure SteamVR is running and your headset is connected
7. Press Play

## Installer

`Installer/` builds VRoxel's Windows installer with Inno Setup: VRoxel itself, and the MCP server, the assistant terminal and push to talk as options. Whisper is downloaded while installing. See `Installer/README.md`.

## Project structure

```
Assets/
  Scripts/       — all project C# source code
  Scenes/        — Unity scenes (VRoxelScene.unity is the main scene)
  Geometry/      — block prefabs and geometry assets
  Animation/     — animation data (Mixamo clips are not included)
  Environment/   — terrain and environment assets
  GamePrefabs/   — runtime prefabs
  StreamingAssets/SteamVR/ — SteamVR actions and default controller bindings
Tools/mcp/       — the MCP server (Python) and its example recipes
Installer/       — Inno Setup script and the scripts preparing its packages
```

See `CLAUDE.md` for a detailed architecture overview.

## License

The project source code (`Assets/Scripts/`) is released under the [MIT License](LICENSE).  
Third-party assets retain their original licenses as described above.
