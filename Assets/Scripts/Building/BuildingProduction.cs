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

        // 尚未落地到 Output 仓库的产出数量（计入容量预占）/ In-flight output units reserved against capacity
        private int _inFlightOutputCount;

        private TransferManager _transfer;

        private void Awake() {
            _transfer = transferManager != null ? transferManager : TransferManager.Instance;
            if (_transfer == null)
                Debug.LogError("[BuildingProduction] TransferManager not found.", this);

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
            if (!HasRequiredInputs())
            {
                ChangeState(BuildingState.InputMissing);
                return;
            }

            if (!CanFitOutputRecipe())
            {
                ChangeState(BuildingState.OutputFull);
                return;
            }

            var rollbackInputs = DeductInputs();
            var animations = new List<(ResourceType type, Transform start, Transform end, System.Action onComplete)>();

            foreach (var recipe in config.outputRecipe)
            {
                for (int i = 0; i < recipe.amount; i++)
                {
                    var capturedType = recipe.type;
                    var capturedOutput = outputWarehouse;
                    var capturedRollback = rollbackInputs;

                    _inFlightOutputCount++;

                    System.Action onComplete = () => OnOutputTransferComplete(
                        capturedType, capturedOutput, capturedRollback);

                    if (capturedOutput?.SpawnPoint != null && capturedOutput?.ContentPoint != null)
                    {
                        animations.Add((capturedType, capturedOutput.SpawnPoint, capturedOutput.ContentPoint, onComplete));
                    }
                    else
                    {
                        onComplete.Invoke();
                    }
                }
            }

            foreach (var recipe in config.inputRecipe)
            {
                for (int i = 0; i < recipe.amount; i++)
                {
                    if (consumeAnchor != null && inputWarehouse?.ContentPoint != null)
                    {
                        animations.Add((recipe.type, inputWarehouse.ContentPoint, consumeAnchor, null));
                    }
                }
            }

            if (animations.Count > 0)
                StartCoroutine(PlayAnimationsStaggered(animations));

            ChangeState(BuildingState.Producing);
        }

        // 同步扣除输入并返回回滚列表 / Deduct inputs synchronously and return rollback list
        private List<(ResourceType type, int amount)> DeductInputs()
        {
            var rollback = new List<(ResourceType type, int amount)>();
            if (config.inputRecipe == null || inputWarehouse == null)
                return rollback;

            foreach (var recipe in config.inputRecipe)
            {
                inputWarehouse.RemoveResource(recipe.type, recipe.amount);
                rollback.Add((recipe.type, recipe.amount));
            }

            return rollback;
        }

        // 产出动画落地回调：入库或回滚原料 / Output transfer callback: deposit or rollback inputs
        private void OnOutputTransferComplete(
            ResourceType type,
            Warehouse output,
            List<(ResourceType type, int amount)> rollbackInputs)
        {
            _inFlightOutputCount = Mathf.Max(0, _inFlightOutputCount - 1);

            if (output != null && output.CanAddResource(type, 1))
            {
                output.AddResource(type, 1);
                OnProductionCompleted?.Invoke(type);
                return;
            }

            RollbackInputs(rollbackInputs);
            ChangeState(BuildingState.OutputFull);
            Debug.LogError(
                $"[BuildingProduction] 产出 {type} 无法入库，已回滚本轮原料 / Output {type} rejected, inputs rolled back",
                this);
        }

        // 将本轮已扣原料退回 Input 仓库 / Return deducted inputs to input warehouse
        private void RollbackInputs(List<(ResourceType type, int amount)> rollbackInputs)
        {
            if (inputWarehouse == null || rollbackInputs == null) return;

            foreach (var (type, amount) in rollbackInputs)
            {
                if (amount > 0)
                    inputWarehouse.AddResource(type, amount);
            }
        }

        // 逐帧错开播放传输动画 / Play transfer animations staggered
        private IEnumerator PlayAnimationsStaggered(
            List<(ResourceType type, Transform start, Transform end, System.Action onComplete)> animations)
        {
            float stagger = _transfer != null ? _transfer.ProductionStaggerInterval : 0.15f;

            foreach (var (type, start, end, onComplete) in animations)
            {
                _transfer?.PlayTransfer(type, start, end, onComplete);
                yield return new WaitForSeconds(stagger);
            }
        }

        // 检查输出仓库是否能容纳本轮配方产出（含在途预占）/ Check output capacity including in-flight reservation
        private bool CanFitOutputRecipe()
        {
            if (outputWarehouse == null || config.outputRecipe == null || config.outputRecipe.Length == 0)
                return true;

            int outputUnits = 0;
            foreach (var recipe in config.outputRecipe)
                outputUnits += recipe.amount;

            return outputWarehouse.GetTotalStored() + _inFlightOutputCount + outputUnits
                   <= outputWarehouse.Capacity;
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
