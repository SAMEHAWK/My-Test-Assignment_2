# Resource Production Chain — Unity Test Assignment

A Unity 6 (6000.3.13f1) resource production chain game for a technical assessment. Three buildings chain-produce N1 → N2 → N3; the player moves resources via a virtual joystick with parabolic Lerp transfer animations.

> **中文说明见 [README.zh-CN.md](README.zh-CN.md)**

---

## Table of Contents

- [Features](#features)
- [How to Use](#how-to-use)
- [Controls](#controls)
- [Building Status UI Setup](#building-status-ui-setup)
- [Project Architecture](#project-architecture)
- [Project Directory](#project-directory)
- [Configuration & Parameters](#configuration--parameters)
- [Related Documents](#related-documents)
- [Requirements](#requirements)
- [Verification](#verification)
- [FAQ](#faq)
- [Notes](#notes)

---

## Features

- **Production chain**: Building 1 produces N1 → Building 2 consumes N1 to produce N2 → Building 3 consumes N1+N2 to produce N3
- **Warehouse system**: Each building has independent Input / Output warehouses, capacity configured per Prefab
- **Auto interaction**: Player approaches warehouse Trigger zones to automatically pick up or deposit resources at intervals
- **Visual animations**: Four transfer scenarios unified under `TransferManager` (Building→Warehouse, Warehouse→Player, Player→Warehouse, Warehouse→Building)
- **Grid stack display**: `WarehouseView` / `BackpackView` display resource blocks on warehouses and player back as an X×Z layered grid
- **Stop-status text**: `BuildingStatusText` subscribes to `OnStateChanged` and shows English stop reasons; reverts to building name when producing
- **Production state events**: `BuildingProduction.OnStateChanged` fires `Producing / InputMissing / OutputFull`

---

## How to Use

### 1. Open the Project

1. Install **Unity 6000.3.13f1 (LTS)** (version confirmed by `ProjectSettings/ProjectVersion.txt`).
2. In Unity Hub, choose **Open** and point to this project's root directory.
3. Wait for initial import and script compilation; the `Console` should show zero red errors.

### 2. Run the Game

1. Double-click `Assets/Scenes/SampleScene.unity` in the Project window to open the main scene.
2. Click **Play (▶)** at the top of the editor to enter Play Mode.
3. Use the on-screen **virtual joystick** (bottom-left) to move the character.
4. Approach a building's **Output warehouse** (green/production side) → auto-pickup resources into backpack.
5. Approach the next building's **Input warehouse** (input side) → auto-deposit from backpack into warehouse for consumption.

### 3. Recommended Play Flow

```
Building 1 Output → pickup N1
    → Building 2 Input → deposit N1 (needs 2 N1 to produce 1 N2)
    → Building 2 Output → pickup N2
    → Building 3 Input → deposit N1 + N2
    → Building 3 Output → pickup N3
```

### 4. Setup from Scratch (optional)

If the scene or Prefabs are not yet configured, refer to the [Configuration & Parameters](#configuration--parameters) section and [docs/架构设计.md](docs/架构设计.md) for component relationships, then complete in Unity Editor:

- Resource cube Prefab and `ResourceDatabase` binding
- Warehouse / Building / Player Prefab creation and component mounting
- Virtual joystick UI (`OnScreenStick` → `Player/Move`), Layers, Physics matrix
- Scene placement and Inspector reference assignments

### 5. Tuning Entry Points

| What to change | Where to change it |
|----------|----------|
| Building recipes, production interval | `Assets/Configs/Building/Building*_Config.asset` |
| Resource Prefab, display name | `Assets/Configs/ResourceDatabase.asset` |
| Flight animation speed, arc height, queue rate | `Assets/Configs/ResourceTransferConfig.asset` |
| Warehouse capacity, warehouse type | `Warehouse` component on each building Prefab |
| Player move speed, backpack capacity | `PlayerController` / `PlayerBackpack` on `Player` Prefab |
| Player pickup/deposit interval | `PlayerWarehouseInteraction.transferInterval` on `Player` Prefab |

---

## Controls

| Action | Input |
|--------|-------|
| Move | Virtual joystick (on-screen touch, `InputSystem_Actions` → `Player/Move`) |
| Pickup | Automatic — approach **Output** warehouse Trigger zone |
| Deposit | Automatic — approach **Input** warehouse Trigger zone |

> Trigger detection is implemented on the player side in `PlayerWarehouseInteraction`; warehouse Prefabs must have a Collider with `Is Trigger` enabled, and the Player / Warehouse Layers must allow collision in the Physics matrix.

---

## Building Status UI Setup

Production state is broadcast via `BuildingProduction.OnStateChanged`. The built-in **`BuildingStatusText`** component displays stop reasons on world-space TMP text. You can also subscribe to the event for custom UI.

### Message flow

| Layer | Mechanism | Description |
|------|------|------|
| Logic | `OnStateChanged(BuildingState)` | Fires only on state **transitions** |
| Logic | `TryResumeProduction()` | Re-checks immediately after input deposit or output pickup |
| UI | `BuildingStatusText` | Subscribes to `OnStateChanged`; formats English reason from warehouse + recipe |

### Display examples

| State | Display |
|------|------|
| `Producing` | Building name only (TMP default text) |
| `InputMissing` | Name + `Need N1x2(has 1)` |
| `OutputFull` | Name + `Output full (8/8)` |

### Editor steps (BuildingStatusText)

1. Under building root (`BuildingProduction`): `GameObject > UI > Canvas`, set **Render Mode = World Space**, scale ~`0.01`, position above building, assign **Event Camera = Main Camera**.
2. Child: `UI > Text - TextMeshPro`, set **Text** to building name (e.g. `N2 Producer`).
3. Add **Building Status Text** component; drag `BuildingProduction` into **Building** (or leave empty if under building hierarchy).
4. Apply Prefab; Play to verify stop/resume text.

Custom UI: subscribe to `OnStateChanged` — the event carries **state enum only**, not reason text; query warehouses and recipe like `BuildingStatusText` does.

See **[README.zh-CN.md — 建筑状态 UI 接入](README.zh-CN.md#建筑状态-ui-接入)** for the full Chinese walkthrough.

---

## Project Architecture

### Layer Structure

```
┌─────────────────────────────────────────────────────────┐
│  View                                                    │
│  WarehouseView · BackpackView · BuildingStatusText       │
├─────────────────────────────────────────────────────────┤
│  Gameplay                                                │
│  BuildingProduction · PlayerController                  │
│  PlayerWarehouseInteraction                             │
├─────────────────────────────────────────────────────────┤
│  Model                                                   │
│  Warehouse · PlayerBackpack                             │
├─────────────────────────────────────────────────────────┤
│  Infrastructure                                          │
│  TransferManager · ResourceObjectPool                   │
├─────────────────────────────────────────────────────────┤
│  Config (ScriptableObject)                               │
│  ResourceDatabase · BuildingProductionConfig            │
│  ResourceTransferConfig                                 │
├─────────────────────────────────────────────────────────┤
│  Core                                                    │
│  ResourceType · WarehouseType · BuildingState           │
└─────────────────────────────────────────────────────────┘
```

### Module Dependencies

| Module | Responsibility | Main Dependencies |
|------|------|----------|
| **Core** | Enum definitions, no runtime logic | None |
| **Data** | SO config classes | Core |
| **Building** | Warehouse inventory, production state machine, warehouse visualization | Core, Data, Transfer |
| **Player** | Movement, backpack, warehouse interaction, backpack visualization | Core, Building, Transfer, Data |
| **Transfer** | Flight animation scheduling, object pool | Core, Data |
| **UI** | Building stop-status text | Core, Building |
| **Util** | Grid layout computation | None |

### Data Flow Summary

1. **Building Production**: `BuildingProduction` ticks on interval or **`TryResumeProduction()`** (after input deposit / output pickup) → checks I/O → deducts inputs → plays animation → writes output → `OnStateChanged` notifies UI.
2. **Player Interaction**: `PlayerWarehouseInteraction` detects Trigger → initiates transfer at intervals → `TransferManager.EnqueueTransfer()` queues playback → callback updates backpack or warehouse.
3. **View Refresh**: `Warehouse` / `PlayerBackpack` inventory changes fire events → `WarehouseView` / `BackpackView` rebuild grid; `BuildingStatusText` listens to production state and warehouse changes.

### Core Design Principles

- **SO-driven config**: Recipes, resource metadata, transfer parameters separated from code; reskin/rebalance without scripting
- **Model-View separation**: Inventory logic decoupled from stack display
- **WarehouseType behavioral constraints**: Input — in only, Output — out only
- **TransferManager unified entry**: Four transfer scenarios share the same animation and object pool
- **Event-driven extensibility**: Production state and inventory changes via `UnityEvent`, ready for UI hooks

For detailed design, see **[docs/架构设计.md](docs/架构设计.md)** (current v1.3).

### Script Directory

```
Assets/Scripts/
├── Core/
│   ├── ResourceType.cs              # Resource enum N1 / N2 / N3
│   ├── WarehouseType.cs             # Warehouse type Input / Output
│   └── BuildingState.cs             # Production state Producing / InputMissing / OutputFull
├── Data/
│   ├── ResourceDatabase.cs          # Resource metadata SO
│   ├── BuildingProductionConfig.cs  # Single-building recipe SO
│   └── ResourceTransferConfig.cs    # Transfer animation SO
├── Building/
│   ├── Warehouse.cs                 # Warehouse inventory (pure data)
│   ├── WarehouseView.cs             # Warehouse grid visualization
│   └── BuildingProduction.cs        # Production timer & state machine
├── Player/
│   ├── PlayerController.cs          # Virtual joystick + CharacterController
│   ├── PlayerBackpack.cs            # Backpack inventory (pure data)
│   ├── BackpackView.cs              # Backpack grid visualization
│   └── PlayerWarehouseInteraction.cs # Trigger interaction
├── Transfer/
│   ├── TransferManager.cs           # Singleton: animation + queue
│   └── ResourceObjectPool.cs        # Singleton: object pool
├── UI/
│   └── BuildingStatusText.cs        # Building stop-status TMP text
└── Util/
    └── GridLayoutHelper.cs          # X×Z grid layered coordinate calculation
```

---

## Project Directory

```
My-Test-Assignment_2/
├── Assets/
│   ├── Configs/                     # ★ ScriptableObject config assets
│   │   ├── ResourceDatabase.asset
│   │   ├── ResourceTransferConfig.asset
│   │   └── Building/
│   │       ├── Building1_Config.asset
│   │       ├── Building2_Config.asset
│   │       └── Building3_Config.asset
│   ├── Prefabs/
│   │   ├── N1.prefab / N2.prefab / N3.prefab       # Resource cubes
│   │   └── Building/
│   │       ├── Warehouse.prefab                      # Warehouse template
│   │       ├── N1 Producer.prefab                    # Building 1
│   │       ├── N2 Producer.prefab                    # Building 2 (Variant)
│   │       └── N3 Producer.prefab                    # Building 3 (Variant)
│   ├── Scenes/
│   │   └── SampleScene.unity         # ★ Main scene (Play entry)
│   ├── Scripts/                      # See script directory above
│   ├── InputSystem_Actions.inputactions  # Input System action definitions
│   └── InputSystem_Actions.cs        # Auto-generated (under Scripts/Input/)
│   ├── Materials/                    # Resource materials
│   └── Settings/                     # URP render pipeline settings
├── docs/
│   ├── 架构设计.md                   # Authoritative architecture document
│   └── 项目改进计划.md               # Code improvement roadmap
├── ProjectSettings/
├── Packages/
└── README.zh-CN.md                   # Chinese version
```

| Path | Description |
|------|------|
| `Assets/Configs/` | All adjustable game parameter SOs — **tune here first** |
| `Assets/Prefabs/` | Building, warehouse, resource cube, player Prefabs |
| `Assets/Scenes/SampleScene.unity` | Runtime entry scene |
| `docs/` | Architecture design and improvement plan |

---

## Configuration & Parameters

### ScriptableObject Config Assets

#### ResourceDatabase (`Assets/Configs/ResourceDatabase.asset`)

Defines metadata and Prefab references for each resource type.

| Field | Type | Description |
|------|------|------|
| `resources[]` | ResourceEntry[] | Resource entry list |
| `resources[].type` | ResourceType | N1 / N2 / N3 |
| `resources[].displayName` | string | Display name |
| `resources[].transferPrefab` | GameObject | Prefab used during flight transfer |
| `resources[].backpackPrefab` | GameObject | Static stacked display Prefab (used by both BackpackView and WarehouseView) |

#### BuildingProductionConfig (`Assets/Configs/Building/Building*_Config.asset`)

Defines the production recipe for a single building (one asset per building).

| Field | Type | Description |
|------|------|------|
| `buildingName` | string | Building display name |
| `productionInterval` | float | Production interval in seconds |
| `inputRecipe[]` | RecipeEntry[] | Resources consumed per cycle |
| `outputRecipe[]` | RecipeEntry[] | Resources produced per cycle |

`RecipeEntry` structure: `{ ResourceType type; int amount; }`

#### ResourceTransferConfig (`Assets/Configs/ResourceTransferConfig.asset`)

Controls transfer animation and queue behavior.

| Field | Type | Default | Description |
|------|------|------|------|
| `moveSpeed` | float | 5 | Base flight speed (duration = distance / speed) |
| `moveCurve` | AnimationCurve | Linear(0,0,1,1) | Normalized time → progress curve |
| `initialPoolSize` | int | 10 | Total pre-warm count for the object pool |
| `transferInterval` | float | 0.3 | Min interval between queued transfers (seconds) |
| `arcHeight` | float | 2 | Peak Y offset for parabolic arc |

### Component Parameters (key fields)

| Component | Field | Description |
|------|------|------|
| `Warehouse` | `Capacity` | Max total resource units in this warehouse |
| `Warehouse` | `Warehouse Type` | Input / Output |
| `Warehouse` | `Accepted Types` | Allowed resource types (empty = accept all) |
| `PlayerController` | `Move Speed` | Character movement speed |
| `PlayerBackpack` | `Capacity` | Max backpack capacity |
| `PlayerWarehouseInteraction` | `Transfer Interval` | Seconds between each unit transfer attempt |

---

## Related Documents

| Document | Path | Description |
|------|------|------|
| Architecture Design | [docs/架构设计.md](docs/架构设计.md) | Core design doc, component relationships, data flow |
| Improvement Plan | [docs/项目改进计划.md](docs/项目改进计划.md) | Code improvement items, phases, acceptance criteria |

---

## Requirements

- Unity 6000.3.13f1 (LTS)
- Universal Render Pipeline (URP)
- Input System package

---

## Verification

Manually verified in Unity Play Mode on **2026-05-29** (see `docs/架构设计.md` §7):

- Three buildings auto-produce N1 → N2 → N3 at configured intervals
- All transfers use Lerp flight animations; stop states show on world-space TMP **and building mesh color** (green/red/yellow)
- Auto pickup/deposit near warehouses; production resumes via `TryResumeProduction`
See [`docs/架构设计.md`](docs/架构设计.md) §7.2–7.3 for IMP-12～14 and phase G checklist.

### Phase G (IMP-05 / IMP-06)

**Editor required** — Create `Assets/Prefabs/Player.prefab` by dragging Hierarchy `Player` into Project (see README.zh-CN.md「阶段 G 收尾」for full steps). Run `git init` at project root for IMP-05.

---

## FAQ

**Q: Where are the game parameters configured?**

A: All adjustable parameters are in `Assets/Configs/`. Building recipes, resource prefabs, transfer animation speed and arc height are all managed via SO inspector fields — no need to edit code.

**Q: What does "Accepted Types" on a warehouse do?**

A: It's a whitelist. When empty, all resource types are accepted. When populated, only listed types can be deposited. Configure this on Input warehouses to match the building's input recipe.

**Q: What are ContentPoint and SpawnPoint?**

A: `ContentPoint` is the resource storage location — the endpoint for "fly to warehouse" animations and the start point for "take from warehouse". `SpawnPoint` is the building's "delivery port" — resources spawn here after production and fly to `ContentPoint` (Output warehouse only).

**Q: How does the interaction between player and warehouse work?**

A: Fully automatic. Step within a warehouse Trigger zone → `PlayerWarehouseInteraction` detects it → at `TransferInterval` cadence, initiates a `TransferManager.EnqueueTransfer()` call — one resource unit flies in parabolic arc to its destination, with inventory updated via callback on arrival.

**Q: The backpack / warehouse shows too many stacked blocks — can I adjust layout?**

A: Yes. On `BackpackView` / `WarehouseView` components, adjust `Columns`, `Rows Per Layer`, `Horizontal Spacing`, `Depth Spacing`, and `Layer Height` — the grid layout recalculates automatically.
