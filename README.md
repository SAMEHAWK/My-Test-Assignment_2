# Resource Chain - Unity Test Assignment

A Unity 6 (6000.3.13f1) resource production chain game for a technical assessment. Three buildings produce N1 → N2 → N3; the player moves resources via a virtual joystick with parabolic Lerp transfer animations.

> **中文说明见 [README.zh-CN.md](README.zh-CN.md)** — the Chinese README is the primary user guide (architecture, directory, configuration, usage).

---

## Overview

Three buildings chain-produce three resource types. The player approaches warehouse trigger zones to automatically pick up or deposit resources. All transfers are handled by a unified `TransferManager`.

## Features

- **Production chain**: Building 1 → N1, Building 2 → N2 (2×N1), Building 3 → N3 (N1+N2)
- **Warehouse system**: Independent Input/Output warehouses per building
- **Auto interaction**: Proximity-based pickup/deposit
- **Transfer animations**: Four scenarios via `TransferManager` + object pool
- **Grid visualization**: `WarehouseView` / `BackpackView` stack display
- **Production events**: `BuildingProduction.OnStateChanged` for UI or logging

## Quick Start

1. Open the project in **Unity 6000.3.13f1 (LTS)**
2. Open `Assets/Scenes/SampleScene.unity`
3. Press **Play** — use the on-screen virtual joystick to move
4. Approach **Output** warehouses to pick up; **Input** warehouses to deposit

For full setup from scratch, see [README.zh-CN.md](README.zh-CN.md) (configuration and architecture sections).

## Controls

| Action | Input |
|--------|-------|
| Move | Virtual joystick (Input System `Player/Move`) |
| Pickup / Deposit | Automatic (approach warehouse triggers) |

## Architecture

See [docs/架构设计.md](docs/架构设计.md) (v1.1) for full design documentation.

```
Assets/Scripts/
├── Core/       # Enums (ResourceType, WarehouseType, BuildingState)
├── Data/       # ScriptableObject configs
├── Building/   # Warehouse, BuildingProduction, WarehouseView
├── Player/     # Controller, backpack, interaction, BackpackView
├── Transfer/   # TransferManager, ResourceObjectPool
└── Util/       # GridLayoutHelper
```

**Config assets**: `Assets/Configs/` (ResourceDatabase, Building configs, ResourceTransferConfig)

## Requirements

- Unity 6000.3.13f1 (LTS)
- Universal Render Pipeline (URP)
- Input System package

## Notes

AI-assisted development; bilingual code comments per [AGENTS.md](AGENTS.md).
