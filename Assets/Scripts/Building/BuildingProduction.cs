using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Core;
using Data;
using Transfer;

// 建筑生产组件 — 按配方周期生产资源，状态机驱动
// Building production component — produces resources cyclically based on recipe, driven by a state machine
namespace Building
{
    public class BuildingProduction : MonoBehaviour
    {
        [Tooltip("建筑生产配置 SO / Building production config ScriptableObject")]
        [SerializeField] private BuildingProductionConfig config;
        public BuildingProductionConfig Config => config;

        // 当前生产状态 / Current production state
        private BuildingState currentState = BuildingState.Producing;
        public BuildingState CurrentState => currentState;

        [Tooltip("生产状态变化时触发 / Fired when production state changes")]
        public UnityEvent<BuildingState> OnStateChanged;

        [Tooltip("生产完成时触发（资源类型）/ Fired when a production cycle completes")]
        public UnityEvent<ResourceType> OnProductionCompleted;

        // 输入/输出仓库 / Input and output warehouses
        [SerializeField] private Warehouse inputWarehouse;
        [SerializeField] private Warehouse outputWarehouse;
        public Warehouse InputWarehouse => inputWarehouse;
        public Warehouse OutputWarehouse => outputWarehouse;

        [Tooltip("消耗资源时的目标锚点（资源从仓库飞向此处）/ Target anchor for consumed resources")]
        [SerializeField] private Transform consumeAnchor;

        [Tooltip("传输管理器（留空则使用单例）/ Transfer manager (uses singleton if empty)")]
        [SerializeField] private TransferManager transferManager;

        // 生产计时器 / Production timer
        private float productionTimer;

        // 解析传输管理器引用 / Resolve transfer manager reference
        private TransferManager Transfer => transferManager != null ? transferManager : TransferManager.Instance;

        private void Awake()
        {
            FindWarehouses();
        }

        private void OnEnable()
        {
            BindWarehouseListeners(true);
        }

        private void OnDisable()
        {
            BindWarehouseListeners(false);
        }

        // 绑定仓库事件：入库后立即重检停产状态 / Bind warehouse events to re-evaluate stop state on deposit
        private void BindWarehouseListeners(bool bind)
        {
            if (inputWarehouse != null)
            {
                if (bind)
                    inputWarehouse.OnResourceAdded.AddListener(OnInputWarehouseChanged);
                else
                    inputWarehouse.OnResourceAdded.RemoveListener(OnInputWarehouseChanged);
            }

            if (outputWarehouse != null)
            {
                if (bind)
                    outputWarehouse.OnResourceRemoved.AddListener(OnOutputWarehouseChanged);
                else
                    outputWarehouse.OnResourceRemoved.RemoveListener(OnOutputWarehouseChanged);
            }
        }

        // Input 入库（含玩家存放动画落地）后尝试恢复生产 / Try resume after input deposited
        private void OnInputWarehouseChanged(ResourceType type, int amount)
        {
            if (currentState != BuildingState.InputMissing) return;
            TryResumeProduction();
        }

        // Output 被取走后尝试恢复生产 / Try resume after output warehouse has space
        private void OnOutputWarehouseChanged(ResourceType type, int amount)
        {
            if (currentState != BuildingState.OutputFull) return;
            TryResumeProduction();
        }

        // 库存变化后立刻重检并尝试生产，避免等下一个 Tick / Re-check and produce immediately
        private void TryResumeProduction()
        {
            if (config == null) return;

            if (!HasRequiredInputs())
                return;

            if (!CanFitOutputRecipe())
            {
                ChangeState(BuildingState.OutputFull);
                return;
            }

            ChangeState(BuildingState.Producing);
            TryProduce();
        }

        // 从子物体中查找并分类仓库 / Find and classify warehouses from child objects
        private void FindWarehouses()
        {
            Warehouse[] warehouses = GetComponentsInChildren<Warehouse>();
            foreach (var wh in warehouses)
            {
                if (wh.Type == WarehouseType.Input)
                    inputWarehouse = wh;
                else if (wh.Type == WarehouseType.Output)
                    outputWarehouse = wh;
            }

            if (config != null && config.inputRecipe != null && config.inputRecipe.Length == 0)
                inputWarehouse = null;
        }

        private void Update()
        {
            if (config == null) return;

            productionTimer += Time.deltaTime;
            if (productionTimer >= config.productionInterval)
            {
                productionTimer = 0f;
                TryProduce();
            }
        }

        // 尝试执行一轮生产 / Attempt one production cycle
        private void TryProduce()
        {
            // 1. 检查输入资源是否充足 / Check if input resources are sufficient
            if (!HasRequiredInputs())
            {
                ChangeState(BuildingState.InputMissing);
                return;
            }

            // 2. 检查输出仓库是否能容纳本轮全部产出 / Check if output warehouse can fit this cycle's output
            if (!CanFitOutputRecipe())
            {
                ChangeState(BuildingState.OutputFull);
                return;
            }

            // 收集待播放的动画（含回调）/ Collect animation requests (with callbacks)
            var animations = new List<(ResourceType type, Transform start, Transform end, System.Action onComplete)>();

            // 3. 同步扣除输入资源，消耗动画纯视觉 / Deduct inputs sync, animation visual-only
            foreach (var recipe in config.inputRecipe)
            {
                for (int i = 0; i < recipe.amount; i++)
                {
                    inputWarehouse?.RemoveResource(recipe.type, 1);

                    if (consumeAnchor != null && inputWarehouse?.ContentPoint != null)
                        animations.Add((recipe.type, inputWarehouse.ContentPoint, consumeAnchor, null));
                }
            }

            // 4. 产出资源在动画到达后才加入仓库 / Add output on animation arrival
            foreach (var recipe in config.outputRecipe)
            {
                for (int i = 0; i < recipe.amount; i++)
                {
                    var capturedType = recipe.type;
                    var capturedOutput = outputWarehouse;

                    if (capturedOutput?.SpawnPoint != null && capturedOutput?.ContentPoint != null)
                    {
                        animations.Add((capturedType, capturedOutput.SpawnPoint, capturedOutput.ContentPoint, () =>
                        {
                            if (capturedOutput != null && capturedOutput.CanAddResource(capturedType, 1))
                            {
                                capturedOutput.AddResource(capturedType, 1);
                                OnProductionCompleted?.Invoke(capturedType);
                            }
                        }));
                    }
                    else
                    {
                        if (capturedOutput != null && capturedOutput.CanAddResource(capturedType, 1))
                        {
                            capturedOutput.AddResource(capturedType, 1);
                            OnProductionCompleted?.Invoke(capturedType);
                        }
                    }
                }
            }

            // 5. 错开播放动画 / Play animations staggered
            if (animations.Count > 0)
                StartCoroutine(PlayAnimationsStaggered(animations));

            // 6. 恢复生产状态 / Resume producing state
            ChangeState(BuildingState.Producing);
        }

        // 逐帧错开播放传输动画 / Play transfer animations staggered
        private IEnumerator PlayAnimationsStaggered(
            List<(ResourceType type, Transform start, Transform end, System.Action onComplete)> animations)
        {
            foreach (var (type, start, end, onComplete) in animations)
            {
                Transfer.PlayTransfer(type, start, end, onComplete);
                yield return new WaitForSeconds(0.15f);
            }
        }

        // 检查输出仓库是否能容纳本轮配方产出 / Check if output warehouse can fit all recipe outputs
        private bool CanFitOutputRecipe()
        {
            if (outputWarehouse == null || config.outputRecipe == null || config.outputRecipe.Length == 0)
                return true;

            foreach (var recipe in config.outputRecipe)
            {
                if (!outputWarehouse.CanAddResource(recipe.type, recipe.amount))
                    return false;
            }

            return true;
        }

        // 检查所有输入资源是否充足 / Check if all required input resources are available
        private bool HasRequiredInputs()
        {
            if (inputWarehouse == null || config.inputRecipe == null || config.inputRecipe.Length == 0)
                return true;

            foreach (var recipe in config.inputRecipe)
            {
                if (!inputWarehouse.HasResources(recipe.type, recipe.amount))
                    return false;
            }

            return true;
        }

        // 切换状态并触发事件 / Change state and fire event
        private void ChangeState(BuildingState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
