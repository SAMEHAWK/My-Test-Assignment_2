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

        [Tooltip("生产状态变化时触发（状态, 原因描述）/ Fired when production state changes (state, reason)")]
        public UnityEvent<BuildingState, string> OnStateChanged;

        [Tooltip("生产完成时触发（资源类型）/ Fired when a production cycle completes")]
        public UnityEvent<ResourceType> OnProductionCompleted;

        // 输入/输出仓库 / Input and output warehouses
        [SerializeField] private Warehouse inputWarehouse;
        [SerializeField] private Warehouse outputWarehouse;

        [Tooltip("消耗资源时的目标锚点（资源从仓库飞向此处）/ Target anchor for consumed resources")]
        [SerializeField] private Transform consumeAnchor;

        // 生产计时器 / Production timer
        private float productionTimer;

        private void Start()
        {
            FindWarehouses();
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
            if (!HasRequiredInputs(out string missingReason))
            {
                ChangeState(BuildingState.InputMissing, missingReason);
                return;
            }

            // 2. 检查输出仓库是否能容纳本轮全部产出 / Check if output warehouse can fit this cycle's output
            if (!CanFitOutputRecipe(out string outputFullReason))
            {
                ChangeState(BuildingState.OutputFull, outputFullReason);
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

            // 4. 产出资源在动画到达后才加入仓库，确保视觉同步
            //    Add output resources on animation arrival to keep visuals in sync
            foreach (var recipe in config.outputRecipe)
            {
                for (int i = 0; i < recipe.amount; i++)
                {
                    // 捕获变量供回调闭包使用 / Capture variables for closure
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
                        // 无动画锚点时直接加入（回退）/ Add directly when no animation anchors (fallback)
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
            ChangeState(BuildingState.Producing, null);
        }

        // 逐帧错开播放传输动画，消耗动画纯视觉，产出动画在到达时回调加入仓库
        // Play transfer animations staggered — consume is visual-only, produce adds to warehouse on arrival
        private IEnumerator PlayAnimationsStaggered(
            List<(ResourceType type, Transform start, Transform end, System.Action onComplete)> animations)
        {
            foreach (var (type, start, end, onComplete) in animations)
            {
                TransferManager.Instance.PlayTransfer(type, start, end, onComplete);
                yield return new WaitForSeconds(0.15f);
            }
        }

        // 检查输出仓库是否能容纳本轮配方产出 / Check if output warehouse can fit all recipe outputs
        private bool CanFitOutputRecipe(out string reason)
        {
            reason = null;

            if (outputWarehouse == null || config.outputRecipe == null || config.outputRecipe.Length == 0)
                return true;

            foreach (var recipe in config.outputRecipe)
            {
                if (!outputWarehouse.CanAddResource(recipe.type, recipe.amount))
                {
                    int current = outputWarehouse.GetTotalStored();
                    reason = $"输出仓库空间不足 / Output warehouse cannot fit {recipe.type} × {recipe.amount} " +
                             $"(当前 {current}/{outputWarehouse.Capacity})";
                    return false;
                }
            }

            return true;
        }

        // 检查所有输入资源是否充足 / Check if all required input resources are available
        private bool HasRequiredInputs(out string reason)
        {
            reason = null;

            if (inputWarehouse == null || config.inputRecipe == null || config.inputRecipe.Length == 0)
                return true;

            foreach (var recipe in config.inputRecipe)
            {
                if (!inputWarehouse.HasResources(recipe.type, recipe.amount))
                {
                    int current = inputWarehouse.GetResourceCount(recipe.type);
                    reason = $"缺少 {recipe.type}: 需要 {recipe.amount} / 当前 {current}";
                    return false;
                }
            }

            return true;
        }

        // 切换状态并触发事件 / Change state and fire event
        private void ChangeState(BuildingState newState, string reason)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnStateChanged?.Invoke(newState, reason);
        }
    }
}
