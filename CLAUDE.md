# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VRoxel is a Unity 2021.3.45f2 VR application for 3D block-based assembly and geometry editing, primarily targeting HTC Vive via SteamVR. Users place, remove, and manipulate voxel-like blocks in VR space; assemblies are saved in the custom `.vrxl` format.

## Development Environment

- **Unity Version:** 2021.3.45f2 LTS
- **Platform:** Windows, VR (HTC Vive / OpenVR / SteamVR)
- **IDE:** Rider or Visual Studio (`.vsconfig` present)
- No CLI build scripts — build via Unity Editor (File > Build Settings)
- No automated test runner commands; tests use Unity Test Framework via the Test Runner window

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
- SteamVR action bindings use `SteamVR_Action_Boolean` / `SteamVR_Input_Sources`

**Menu / UI (`Scripts/Menu/`)**
- `MenuSwitch` is the central menu state manager; it wires undo/redo button states to `UndoStack` events and manages player elevation/teleportation
- Geometry, character, and kitbash editing each have dedicated menu scripts (`EditGeometryMenu`, `CharacterEditor`, `KitBashMenu`)
- `MessageSystem` provides static `ShowMessage()` / `ShowPermanentMessage()` — do not display feedback any other way

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
