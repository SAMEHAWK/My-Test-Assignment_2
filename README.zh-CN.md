# 资源生产链 — Unity 测试任务

基于 Unity 6 (6000.3.13f1) 的资源生产链游戏，用于技术考核。三座建筑串联生产 N1 → N2 → N3，玩家通过虚拟摇杆搬运资源，所有传输均带抛物线 Lerp 飞行动画。

---

## 目录

- [功能特性](#功能特性)
- [如何使用](#如何使用)
- [操作方式](#操作方式)
- [建筑状态 UI 接入](#建筑状态-ui-接入)
- [项目架构](#项目架构)
- [项目目录说明](#项目目录说明)
- [配置与参数说明](#配置与参数说明)
- [相关文档](#相关文档)
- [环境要求](#环境要求)
- [自测验证（Verification）](#自测验证verification)
- [常见问题](#常见问题)
- [备注](#备注)

---

## 功能特性

- **生产链**：建筑 1 生产 N1 → 建筑 2 消耗 N1 生产 N2 → 建筑 3 消耗 N1+N2 生产 N3
- **仓库系统**：每座建筑拥有独立的 Input / Output 仓库，容量在 Prefab 上单独配置
- **自动交互**：玩家靠近仓库 Trigger 区域时，按间隔自动拾取或存放资源
- **可视化动画**：四种传输场景统一由 `TransferManager` 处理（建筑→仓库、仓库→角色、角色→仓库、仓库→建筑）
- **网格堆叠显示**：`WarehouseView` / `BackpackView` 在仓库与角色背上按 X×Z 网格分层显示资源块
- **停产状态文字**：`BuildingStatusText` 订阅 `OnStateChanged`，缺料/满库时显示英文原因，恢复生产后切回建筑名
- **生产状态事件**：`BuildingProduction.OnStateChanged` 抛出 `Producing / InputMissing / OutputFull`

---

## 如何使用

### 1. 打开项目

1. 安装 **Unity 6000.3.13f1 (LTS)**（版本以 `ProjectSettings/ProjectVersion.txt` 为准）。
2. 在 Unity Hub 中选择 **Open**，指向本项目根目录。
3. 等待首次导入与脚本编译完成，`Console` 中应无红色错误。

### 2. 运行游戏

1. 在 Project 窗口双击 `Assets/Scenes/SampleScene.unity` 打开主场景。
2. 点击编辑器顶部的 **Play (▶)** 进入运行模式。
3. 使用屏幕左下角**虚拟摇杆**移动角色。
4. 将角色移近建筑的 **Output 仓库**（绿色/产出侧）→ 自动拾取资源到背包。
5. 将角色移近下一座建筑的 **Input 仓库**（输入侧）→ 自动从背包存入仓库，供建筑消耗生产。

### 3. 推荐游玩流程

```
建筑 1 Output 拾取 N1
    → 建筑 2 Input 存入 N1（需 2 个 N1 才产 1 个 N2）
    → 建筑 2 Output 拾取 N2
    → 建筑 3 Input 存入 N1 + N2
    → 建筑 3 Output 拾取 N3
```

### 4. 从零搭建场景（可选）

若场景或 Prefab 尚未配置，可参考本文 [配置与参数说明](#配置与参数说明) 及 [docs/架构设计.md](docs/架构设计.md) 中的组件关系，在 Unity 编辑器中完成：

- 资源方块 Prefab 与 `ResourceDatabase` 绑定
- 仓库 / 建筑 / 玩家 Prefab 创建与组件挂载
- 虚拟摇杆 UI（`OnScreenStick` → `Player/Move`）、Layer、Physics 矩阵
- 场景摆放与 Inspector 引用拖拽

### 5. 调参入口

| 想改什么 | 去哪里改 |
|----------|----------|
| 建筑配方、生产间隔 | `Assets/Configs/Building/Building*_Config.asset` |
| 资源 Prefab、显示名 | `Assets/Configs/ResourceDatabase.asset` |
| 飞行动画速度、弧高、队列节奏 | `Assets/Configs/ResourceTransferConfig.asset` |
| 仓库容量、仓库类型 | 各建筑 Prefab 内 `Warehouse` 组件 |
| 玩家移动速度、背包容量 | `Player` Prefab 内 `PlayerController` / `PlayerBackpack` |
| 玩家拾取/存放发起频率 | `Player` Prefab 内 `PlayerWarehouseInteraction.transferInterval` |

---

## 操作方式

| 操作 | 方式 |
|------|------|
| 移动 | 虚拟摇杆（屏幕触控，`InputSystem_Actions` → `Player/Move`） |
| 拾取资源 | 自动 — 靠近 **Output** 仓库 Trigger 区域 |
| 存放资源 | 自动 — 靠近 **Input** 仓库 Trigger 区域 |

> Trigger 检测在玩家侧的 `PlayerWarehouseInteraction` 上实现；仓库 Prefab 需有 `Is Trigger` 的 Collider，且 Player / Warehouse Layer 在 Physics 矩阵中允许碰撞。

---

## 建筑状态 UI 接入

建筑生产状态通过 `BuildingProduction.OnStateChanged` 广播，内置组件 **`BuildingStatusText`** 负责在世界空间 TMP 文字上显示停产原因。也可自行订阅该事件实现自定义 UI。

### 状态消息来源

| 层级 | 机制 | 说明 |
|------|------|------|
| **逻辑层** | `BuildingProduction.OnStateChanged(BuildingState)` | 仅在状态**切换**时触发（`Producing` / `InputMissing` / `OutputFull`） |
| **逻辑层** | `TryResumeProduction()` | Input 入库或 Output 被取走后**立即**重检；条件满足则恢复生产，不必等下一个生产周期 |
| **UI 层** | `BuildingStatusText` | 订阅 `OnStateChanged`；自行查 `Config` + 仓库库存格式化英文原因行 |

### 显示文案示例（英文）

| 状态 | 显示 |
|------|------|
| `Producing` | 仅建筑名（TMP 初始文本），如 `N2 Producer` |
| `InputMissing` | 建筑名 + `Need N1x2(has 1)` |
| `InputMissing`（库存已够、等待恢复） | 建筑名 + `Ready`（极短过渡，通常随即切回生产） |
| `OutputFull` | 建筑名 + `Output full (8/8)` |

### 需你在编辑器中操作：挂载 BuildingStatusText

以某建筑 Prefab（根节点含 `BuildingProduction`）为例：

1. **创建 World Space Canvas**
   - 选中建筑根节点，菜单 `GameObject > UI > Canvas`（游戏对象 > UI > Canvas），命名为 `StatusCanvas`
   - **目的**：在建筑上方显示世界空间文字
   - Inspector 中 `Canvas` 组件：`Render Mode` = **World Space**
   - `Rect Transform`：`Width/Height` 约 `8 × 1`，`Scale` 约 `(0.01, 0.01, 0.01)`（按场景比例微调）
   - `Pos Y` 调高至建筑上方（如 `2`～`3`）
   - **Event Camera**：拖入场景 `Main Camera`
   - **预期**：Scene 视图中 Canvas 缩为建筑顶部的薄板

2. **创建 TMP 文字**
   - 右键 `StatusCanvas` → `UI > Text - TextMeshPro`（若首次使用按提示 Import TMP Essentials）
   - 命名为 `StatusText`
   - **Text** 字段填入建筑名（与 `BuildingProductionConfig.buildingName` 一致，如 `N2 Producer`）——此文本即正常生产时的显示内容
   - `Font Size` 约 `0.8`（World Space 下按 Scale 调整）
   - `Alignment` 水平/垂直均居中
   - **预期**：文字居中显示于 Canvas 内

3. **添加 BuildingStatusText 组件**
   - 选中 `StatusText`，`Add Component` → 搜索 `Building Status Text`
   - **Building** 槽：
     - **推荐**：拖入建筑根节点上的 `BuildingProduction` 组件
     - **或留空**：若 `StatusCanvas` 是 `BuildingProduction` 所在物体的子层级，运行时会 `GetComponentInParent` 自动查找
   - **注意**：若留空且层级不在建筑子树内，Console 会出现 `[BuildingStatusText] No BuildingProduction found` Warning
   - **预期**：Inspector 出现 `Building Status Text (Script)`，含 `Building` 可选引用

4. **保存 Prefab**
   - 若编辑 Prefab：点击 **Apply** 保存
   - Play 后缺料/满库应出现第二行英文原因，恢复生产后回到仅建筑名

### 自定义 UI（可选）

不使用时内置组件，可编写脚本订阅：

```csharp
// 挂载到任意 UI 对象，在 Inspector 拖入对应 BuildingProduction
buildingProduction.OnStateChanged.AddListener(state => {
    // state: Producing / InputMissing / OutputFull
    // 自行读 buildingProduction.InputWarehouse / Config 拼文案
});
```

`OnStateChanged` **不携带**原因字符串；缺料详情需像 `BuildingStatusText` 一样查询仓库与配方。

---

## 项目架构

### 模块分层

```
┌─────────────────────────────────────────────────────────┐
│  表现层 View                                             │
│  WarehouseView · BackpackView · BuildingStatusText      │
├─────────────────────────────────────────────────────────┤
│  玩法层 Gameplay                                         │
│  BuildingProduction · PlayerController                  │
│  PlayerWarehouseInteraction                             │
├─────────────────────────────────────────────────────────┤
│  数据层 Model                                            │
│  Warehouse · PlayerBackpack                             │
├─────────────────────────────────────────────────────────┤
│  基础设施 Infrastructure                                 │
│  TransferManager · ResourceObjectPool                   │
├─────────────────────────────────────────────────────────┤
│  配置层 Config (ScriptableObject)                        │
│  ResourceDatabase · BuildingProductionConfig            │
│  ResourceTransferConfig                                 │
├─────────────────────────────────────────────────────────┤
│  核心定义 Core                                           │
│  ResourceType · WarehouseType · BuildingState           │
└─────────────────────────────────────────────────────────┘
```

### 模块依赖关系

| 模块 | 职责 | 主要依赖 |
|------|------|----------|
| **Core** | 枚举定义，无运行时逻辑 | 无 |
| **Data** | SO 配置类 | Core |
| **Building** | 仓库库存、生产状态机、仓库可视化 | Core, Data, Transfer |
| **Player** | 移动、背包、仓库交互、背包可视化 | Core, Building, Transfer, Data |
| **Transfer** | 飞行动画调度、对象池 | Core, Data |
| **UI** | 建筑停产状态文字 | Core, Building |
| **Util** | 网格布局计算 | 无 |

### 数据流概要

1. **建筑生产**：`BuildingProduction` 定时 Tick 或 **`TryResumeProduction()`**（Input 入库 / Output 取货后立即）→ 检查输入/输出 → 扣输入 → 播放动画 → 产出写入 Output 仓库 → `OnStateChanged` 通知 UI。
2. **玩家交互**：`PlayerWarehouseInteraction` 检测 Trigger → 按间隔发起传输 → `TransferManager.EnqueueTransfer()` 排队播放 → 回调更新背包或仓库。
3. **视图刷新**：`Warehouse` / `PlayerBackpack` 库存变化触发事件 → `WarehouseView` / `BackpackView` 重建网格显示；`BuildingStatusText` 监听生产状态与仓库变化更新 TMP 文字。

### 核心设计原则

- **SO 驱动配置**：配方、资源元数据、传输参数与代码分离，换皮/调参不改脚本
- **Model-View 分离**：库存逻辑与堆叠显示解耦
- **WarehouseType 收敛行为**：Input 只进不出，Output 只出不进
- **TransferManager 统一入口**：四种传输场景共用同一套动画与对象池
- **事件驱动扩展**：生产状态、库存变化通过 `UnityEvent` 抛出，便于挂 UI

详细设计见 **[docs/架构设计.md](docs/架构设计.md)**（当前 v1.3）。

### 脚本目录

```
Assets/Scripts/
├── Core/
│   ├── ResourceType.cs         # 资源枚举 N1 / N2 / N3
│   ├── WarehouseType.cs        # 仓库类型 Input / Output
│   └── BuildingState.cs        # 生产状态 Producing / InputMissing / OutputFull
├── Data/
│   ├── ResourceDatabase.cs     # 资源元数据 SO
│   ├── BuildingProductionConfig.cs  # 单建筑配方 SO
│   └── ResourceTransferConfig.cs    # 传输动画 SO
├── Building/
│   ├── Warehouse.cs            # 仓库库存（纯数据）
│   ├── WarehouseView.cs        # 仓库网格可视化
│   └── BuildingProduction.cs   # 生产计时与状态机
├── Player/
│   ├── PlayerController.cs     # 虚拟摇杆 + CharacterController
│   ├── PlayerBackpack.cs       # 背包库存（纯数据）
│   ├── BackpackView.cs         # 背包网格可视化
│   └── PlayerWarehouseInteraction.cs  # Trigger 交互
├── Transfer/
│   ├── TransferManager.cs      # 单例：动画 + 队列
│   └── ResourceObjectPool.cs   # 单例：对象池
├── UI/
│   └── BuildingStatusText.cs   # 建筑停产状态 TMP 文字
└── Util/
    └── GridLayoutHelper.cs     # X×Z 网格分层坐标计算
```

---

## 项目目录说明

```
My-Test-Assignment_2/
├── Assets/
│   ├── Configs/                    # ★ ScriptableObject 配置资产
│   │   ├── ResourceDatabase.asset
│   │   ├── ResourceTransferConfig.asset
│   │   └── Building/
│   │       ├── Building1_Config.asset
│   │       ├── Building2_Config.asset
│   │       └── Building3_Config.asset
│   ├── Prefabs/
│   │   ├── N1.prefab / N2.prefab / N3.prefab   # 资源方块
│   │   └── Building/
│   │       ├── Warehouse.prefab                 # 仓库模板
│   │       ├── N1 Producer.prefab                 # 建筑 1
│   │       ├── N2 Producer.prefab                 # 建筑 2（Variant）
│   │       └── N3 Producer.prefab                 # 建筑 3（Variant）
│   ├── Scenes/
│   │   └── SampleScene.unity       # ★ 主场景（Play 入口）
│   ├── Scripts/                    # 见上文脚本目录
│   ├── InputSystem_Actions.inputactions  # Input System 输入定义
│   └── InputSystem_Actions.cs      # 自动生成（位于 Scripts/Input/）
│   ├── Materials/                  # 资源材质
│   └── Settings/                   # URP 渲染管线设置
├── docs/
│   ├── 架构设计.md                 # 权威架构文档
│   └── 项目改进计划.md             # 代码改进路线
├── ProjectSettings/
├── Packages/
└── README.zh-CN.md                 # 本文件
```

| 路径 | 说明 |
|------|------|
| `Assets/Configs/` | 所有可调游戏参数 SO，**优先在此调参** |
| `Assets/Prefabs/` | 建筑、仓库、资源方块、玩家 Prefab |
| `Assets/Scenes/SampleScene.unity` | 运行入口场景 |
| `docs/` | 架构设计与改进计划 |

---

## 配置与参数说明

### ScriptableObject 配置资产

#### ResourceDatabase（`Assets/Configs/ResourceDatabase.asset`）

定义每种资源的元数据与 Prefab 引用。

| 字段 | 类型 | 说明 |
|------|------|------|
| `resources[]` | ResourceEntry[] | 资源条目列表 |
| `resources[].type` | ResourceType | N1 / N2 / N3 |
| `resources[].displayName` | string | 显示名称 |
| `resources[].transferPrefab` | GameObject | 飞行传输时使用的 Prefab |
| `resources[].backpackPrefab` | GameObject | 静态堆叠显示 Prefab（背包 View 与仓库 View 共用） |

#### BuildingProductionConfig（`Assets/Configs/Building/Building*_Config.asset`）

每种建筑一份，定义配方与生产间隔。**仓库容量不在此配置**，在 Prefab 的 `Warehouse.capacity` 上设置。

| 字段 | 类型 | 说明 |
|------|------|------|
| `buildingName` | string | 建筑显示名 |
| `productionInterval` | float | 生产周期（秒） |
| `inputRecipe[]` | RecipeEntry[] | 每轮消耗；空数组表示无输入（如建筑 1） |
| `outputRecipe[]` | RecipeEntry[] | 每轮产出 |
| `RecipeEntry.type` | ResourceType | 资源类型 |
| `RecipeEntry.amount` | int | 数量 |

**当前项目配置值：**

| 资产 | 消耗 | 产出 | 间隔 |
|------|------|------|------|
| Building1_Config | — | N1 × 1 | 1s |
| Building2_Config | N1 × 2 | N2 × 1 | 2s |
| Building3_Config | N1 × 1 + N2 × 1 | N3 × 1 | 3s |

> 架构文档中的 3s / 5s / 8s 为设计参考值；实际以 `.asset` 文件为准。

**典型仓库容量（在 Prefab 的 Warehouse 组件上）：** 建筑 1 输入/输出 = 0/10，建筑 2 = 8/8，建筑 3 = 6/6。

#### ResourceTransferConfig（`Assets/Configs/ResourceTransferConfig.asset`）

全局传输动画与队列参数。

| 字段 | 类型 | 默认值（参考） | 说明 |
|------|------|----------------|------|
| `moveSpeed` | float | 10 | 飞行移动速度（单位/秒），越大动画越快 |
| `moveCurve` | AnimationCurve | 线性 0→1 | 归一化时间到进度的曲线 |
| `initialPoolSize` | int | 10 | 启动预热对象池总量（按资源类型分摊） |
| `transferInterval` | float | 0.1 | **队列出队间隔**：`EnqueueTransfer` 请求之间的播放间隔 |
| `arcHeight` | float | 2（代码默认） | 抛物线弧高（Y 轴峰值偏移） |

> **注意两种「间隔」职责不同：**
> - `ResourceTransferConfig.transferInterval` — TransferManager **队列**出队节奏
> - `PlayerWarehouseInteraction.transferInterval` — 玩家靠近仓库时**发起**传输的节奏（Inspector 默认 0.5s，可按需调为 0.3s 等）

---

### MonoBehaviour 组件参数

#### Warehouse（仓库 Prefab / 建筑子物体）

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `warehouseType` | WarehouseType | — | Input（只进）或 Output（只出） |
| `capacity` | int | 10 | 仓库总容量（所有资源计数之和上限） |
| `contentPoint` | Transform | — | 传输动画起点/终点锚点 |
| `spawnPoint` | Transform | — | 产出生成点（仅 Output 仓库使用） |
| `acceptedTypes` | List\<ResourceType\> | 空 | 允许存放的类型；**空列表 = 接受全部** |
| `inventoryDebug` | string | — | 运行时库存快照（只读调试） |

事件：`OnResourceAdded`、`OnResourceRemoved` — 供 `WarehouseView` 订阅。

#### BuildingProduction（建筑根节点）

| 字段 | 类型 | 说明 |
|------|------|------|
| `config` | BuildingProductionConfig | 拖入对应建筑 Config SO |
| `inputWarehouse` | Warehouse | 可手动指定；留空则 Awake 时从子物体按类型查找 |
| `outputWarehouse` | Warehouse | 同上 |
| `consumeAnchor` | Transform | 消耗动画飞向的目标点（纯视觉） |
| `transferManager` | TransferManager | 可选；留空则使用 `TransferManager.Instance` |

**恢复生产**：监听 `InputWarehouse.OnResourceAdded` / `OutputWarehouse.OnResourceRemoved`，在 `InputMissing` / `OutputFull` 时调用 `TryResumeProduction()`，原料与空位满足则立即 `TryProduce()`。

事件：

| 事件 | 参数 | 说明 |
|------|------|------|
| `OnStateChanged` | BuildingState | 状态变化；原因文字由 `BuildingStatusText` 自行查询仓库格式化 |
| `OnProductionCompleted` | ResourceType | 单件产出到达仓库时 |

#### BuildingStatusText（建筑子物体 TMP 文字）

| 字段 | 类型 | 说明 |
|------|------|------|
| `building` | BuildingProduction | 可选；留空则从父级 `GetComponentInParent` 查找 |

行为：`Producing` 显示 TMP 初始建筑名；`InputMissing` / `OutputFull` 追加英文原因行；监听 Input/Output 仓库库存变化实时刷新计数。

接入步骤见上文 **[建筑状态 UI 接入](#建筑状态-ui-接入)**。

#### PlayerController（玩家）

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `moveSpeed` | float | 5 | 移动速度 |
| `cameraTransform` | Transform | 空 | 相机引用；用于相机相对移动。留空则使用世界 Z 轴方向 |

#### PlayerBackpack（玩家）

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `capacity` | int | 20 | 背包总容量 |
| `backpackAnchor` | Transform | — | 资源堆叠锚点（传输终点 + BackpackView 父节点） |

#### PlayerWarehouseInteraction（玩家）

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `transferInterval` | float | 0.5 | 玩家在 Trigger 内每隔多久尝试传输 1 个资源 |
| `transferManager` | TransferManager | 空 | 可选；留空则使用 `TransferManager.Instance` |

需要：同物体上有 `CharacterController` + **Is Trigger** 的 Collider（或与仓库 Trigger 重叠检测）。

#### BackpackView / WarehouseView（可视化）

| 字段 | 类型 | Backpack 默认 | Warehouse 默认 | 说明 |
|------|------|---------------|----------------|------|
| `resourceDatabase` | ResourceDatabase | — | — | 资源 Prefab 来源 |
| `columns` | int | 3 | 3 | 每层 X 方向方块数 |
| `rowsPerLayer` | int | 3 | 3 | 每层 Z 方向行数 |
| `horizontalSpacing` | float | 0.35 | 0.5 | 列间距 |
| `depthSpacing` | float | 0.35 | 0.5 | 行间距 |
| `layerHeight` | float | 0.35 | 0.5 | 层间高度 |
| `displayAnchor` | Transform | — | 空=ContentPoint | 仅 WarehouseView；堆叠显示父节点 |

#### TransferManager（场景或玩家上的单例）

| 字段 | 类型 | 说明 |
|------|------|------|
| `resourceDatabase` | ResourceDatabase | 按类型取 transferPrefab |
| `config` | ResourceTransferConfig | 速度、曲线、队列间隔、弧高 |

公开 API：

| 方法 | 调用方 | 说明 |
|------|--------|------|
| `PlayTransfer(type, start, end, onComplete)` | BuildingProduction | 立即播放，用于建筑生产 |
| `EnqueueTransfer(...)` | PlayerWarehouseInteraction | 加入队列，按 `transferInterval` 出队 |

#### ResourceObjectPool（单例）

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `defaultPoolSize` | int | 10 | 池耗尽时动态扩容的参考值 |

---

### 库存与动画时序

| 操作 | 库存变更时机 | 失败回退 |
|------|-------------|----------|
| 建筑消耗输入 | 生产开始时立即扣除 | — |
| 建筑产出 | 动画到达后回调添加 | 预检配方空位，不足则不生产 |
| 玩家拾取 | 发起时立即从仓库扣除 | 背包满则 Animation 回调退回仓库 |
| 玩家存放 | 发起时立即从背包扣除 | 仓库满则 Animation 回调退回背包 |

---

## 相关文档

| 文档 | 路径 | 用途 |
|------|------|------|
| 架构设计 | [docs/架构设计.md](docs/架构设计.md) | 模块关系、数据流、设计决策（**技术权威**） |
| 项目改进计划 | [docs/项目改进计划.md](docs/项目改进计划.md) | 代码改进项、评审反馈、实施阶段 |

---

## 环境要求

| 项 | 要求 |
|----|------|
| Unity | **6000.3.13f1 (LTS)** |
| 渲染管线 | Universal Render Pipeline (URP) |
| 输入 | Input System（`com.unity.inputsystem`） |
| 平台 | 编辑器 Play 模式即可体验；虚拟摇杆面向触控/移动端 |

---

## 自测验证（Verification）

以下项已于 **2026-05-29** 在 Unity Play 模式下手动验证（详见 `docs/架构设计.md` 第七章）：

- 三座建筑按配置间隔自动生产 N1 → N2 → N3
- 建筑产出 / 拾取 / 存放均带 Lerp 飞行动画
- 缺料显示 `InputMissing` 状态文字；满库显示 `OutputFull`；**建筑 Visual 网格颜色**随状态切换（绿/红/黄，`BuildingProductionView`）
- 玩家靠近 Output 自动拾取、靠近 Input 自动存放
- 入库或取货后 `TryResumeProduction` 立即恢复生产
- EditMode 单元测试：`Window > General > Test Runner` → EditMode → Run All（`ResourceInventoryTests` + 适配器薄测）

完整勾选清单见 [`docs/架构设计.md`](docs/架构设计.md) **§7.2～7.3**（含 IMP-12～14 与阶段 G）。

---

## 阶段 G 收尾（IMP-05 / IMP-06）

> **需你在编辑器中操作** — 以下无法仅靠改代码完成。详见 L2 `unity-editor-handoff.md`。

### IMP-06 — 创建 Player Prefab

**目的**：新场景可拖 Prefab 复现玩家，而非仅依赖场景内嵌对象。

1. **打开场景**  
   - Project → `Assets/Scenes/SampleScene.unity`  
   - 预期：Hierarchy 中有 `Player` 根节点（含 `CharacterController`、`PlayerController`、`PlayerBackpack` 等）

2. **创建 Prefab**  
   - 若尚无 `Assets/Prefabs/` 文件夹：Project 右键 → **Create → Folder** → 命名 `Prefabs`  
   - 将 Hierarchy 中的 **Player** 拖到 Project 的 `Assets/Prefabs/`  
   - 命名为 **`Player.prefab`**  
   - 预期：Project 出现 `Player.prefab`；Hierarchy 中 Player 变为蓝色 Prefab 实例

3. **验证引用**  
   - 选中 Player 实例，Inspector 检查 `PlayerBackpack.backpackAnchor`、`BackpackView.resourceDatabase` 等非空  
   - **File → Save** 保存场景

4. **预期结果**  
   - 新建空场景拖入 `Player.prefab` + 三座 `N1/N2/N3 Producer.prefab` 即可复现主流程

### IMP-05 — Git 初始化

**目的**：展示模块演进历史（改进计划验收项）。

1. 安装 [Git for Windows](https://git-scm.com/) 并确保 `git` 在 PATH 中  
2. 项目根目录执行：

```powershell
cd "d:\Unity Project\My-Test-Assignment_2"
git init
git add .
git status   # 确认无 Library/ Temp/ Logs/
```

3. 按 [`docs/项目改进计划.md`](docs/项目改进计划.md) IMP-05 建议分批 commit  
4. 远程已配置时：`git remote add origin https://github.com/SAMEHAWK/My-Test-Assignment_2.git`

> AI 代提交流程见 [`docs/skill-本地覆盖.md`](docs/skill-本地覆盖.md)「Git 同步」（须先 `pre-commit-review.ps1` exit 0）。

### EditMode batchmode（可选 CLI 验证）

**须先关闭 Unity 编辑器**（同一项目不能双开），再执行：

```powershell
& "D:\Unity Engine\6000.3.13f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "d:\Unity Project\My-Test-Assignment_2" `
  -runTests -testPlatform EditMode `
  -testResults "d:\Unity Project\My-Test-Assignment_2\TestResults.xml" `
  -logFile "d:\Unity Project\My-Test-Assignment_2\Logs\editmode-test.log"
```

---

## 常见问题

**Q：靠近仓库没有反应？**

- 确认玩家有 `PlayerWarehouseInteraction` 和 Trigger Collider
- 确认仓库 Collider 勾选了 **Is Trigger**
- 检查 `Edit > Project Settings > Physics` 中 Player / Warehouse Layer 碰撞矩阵已勾选

**Q：资源在飞但背包/仓库数量不对？**

- 传输采用「先扣来源、动画结束后加目标」；回调失败时会 Console 输出 Warning 并退回来源
- 检查背包 `capacity` 或仓库 `capacity` / `acceptedTypes`

**Q：建筑不生产？**

- 看建筑上方状态文字：`Need N1x2(has 0)` = 原料不足；`Output full (10/10)` = 产出仓库满
- 确认 `BuildingProduction.config` 已绑定对应 SO

**Q：存放资源后仍显示 Input missing / Need？**

- 若显示 `Need N1x2(has 1)`：数量仍不足（如建筑 2 需要 2 个 N1），继续搬运
- 若资源刚落地：等待存放动画结束（入库后 `TryResumeProduction` 会自动恢复）
- 若显示 `Ready` 后未恢复：检查 Output 是否已满（会切到 `Output full`）

**Q：修改 Input Actions 后编译报错？**

- 在 `Assets/Scripts/Input/InputSystem_Actions.inputactions` 上勾选 **Generate C# Class** 并 Apply，等待重新生成 `InputSystem_Actions.cs`

**Q：如何新增第四座建筑？**

1. 在 `ResourceType` 枚举中增加 N4（若需要新资源类型）
2. 更新 `ResourceDatabase.asset` 条目
3. 复制现有建筑 Prefab Variant，替换 `BuildingProductionConfig` SO
4. 调整子物体 Warehouse 的 capacity / type
5. 摆入场景即可，**无需改生产逻辑代码**

---

## 备注

- 英文说明见 [README.md](README.md)（内容与中文版应对齐，若发现不一致以本文件及 `docs/` 为准）。
