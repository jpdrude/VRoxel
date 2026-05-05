# VRoxel

A VR application for block-based 3D assembly and geometry editing, built with Unity 2021.3 LTS for HTC Vive (SteamVR/OpenVR).

Users compose spatial assemblies from discrete block types in VR, save and load them in the `.vrxl` format, import external meshes and point clouds, and animate characters within the scene. The project is research software developed at [your institution].

## Requirements

- Unity **2021.3.45f2** (other 2021.3.x patch releases likely work)
- HTC Vive or compatible OpenVR headset
- Windows 10/11, DirectX 11

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

## Setup

1. Clone this repository
2. Purchase and import the paid plugins listed above via the Unity Package Manager or by dragging the `.unitypackage` files into the project
3. Open the project in Unity 2021.3.45f2
4. Open `Assets/Scenes/VRoxelScene.unity`
5. Ensure SteamVR is running and your headset is connected
6. Press Play

## Project structure

```
Assets/
  Scripts/       — all project C# source code
  Scenes/        — Unity scenes (VRoxelScene.unity is the main scene)
  Geometry/      — block prefabs and geometry assets
  Animation/     — animation data
  Environment/   — terrain and environment assets
  GamePrefabs/   — runtime prefabs
```

See `CLAUDE.md` for a detailed architecture overview.

## License

The project source code (`Assets/Scripts/`) is released under the [MIT License](LICENSE).  
Third-party assets retain their original licenses as described above.
