using UnityEngine;
using Core;
using Building;
using Transfer;

// 玩家仓库交互 — Trigger 检测附近仓库，按间隔自动拾取/存放资源
// Player warehouse interaction — detects nearby warehouse via trigger, auto pickup/deposit on interval
namespace Player
{
    [RequireComponent(typeof(PlayerBackpack))]
    public class PlayerWarehouseInteraction : MonoBehaviour
    {
        [SerializeField, Tooltip("每个资源单位的传输间隔（秒）/ Transfer interval per resource unit in seconds")]
        private float _transferInterval = 0.5f;
        public float transferInterval { get => _transferInterval; set => _transferInterval = value; }

        private PlayerBackpack _backpack;
        private Warehouse _currentWarehouse;
        private float _transferTimer;

        private void Awake()
        {
            _backpack = GetComponent<PlayerBackpack>();
        }

        private void Update()
        {
            if (_currentWarehouse == null) return;

            _transferTimer += Time.deltaTime;
            if (_transferTimer < _transferInterval) return;
            _transferTimer = 0f;

            TryTransfer();
        }

        // 根据仓库类型尝试一次传输 / Attempt one transfer based on warehouse type
        private void TryTransfer()
        {
            if (_currentWarehouse.Type == WarehouseType.Output)
                TryTakeFromWarehouse();
            else if (_currentWarehouse.Type == WarehouseType.Input)
                TryDepositToWarehouse();
        }

        // 从 Output 仓库取出资源放入背包 / Take resource from output warehouse to backpack
        private void TryTakeFromWarehouse()
        {
            if (_backpack.IsFull()) return;

            // 捕获当前引用，防止回调时玩家已离开 / Capture current references to avoid stale refs in callback
            Warehouse warehouse = _currentWarehouse;
            if (warehouse == null) return;

            var stored = warehouse.StoredResources;
            foreach (var kvp in stored)
            {
                ResourceType type = kvp.Key;
                if (kvp.Value <= 0) continue;
                if (!_backpack.CanAddResource(type)) continue;

                // 立即从仓库扣除 / Deduct from warehouse immediately
                warehouse.RemoveResource(type, 1);

                Transform startPoint = warehouse.ContentPoint;
                Transform endPoint = _backpack.backpackAnchor;
                PlayerBackpack backpack = _backpack;

                TransferManager.Instance.EnqueueTransfer(type, startPoint, endPoint, () =>
                {
                    // 动画完成后加入背包；失败则退回仓库 / Add to backpack on arrival; return to warehouse on failure
                    if (backpack.CanAddResource(type, 1))
                    {
                        backpack.AddResource(type, 1);
                    }
                    else if (warehouse.CanAddResource(type, 1))
                    {
                        warehouse.AddResource(type, 1);
                        Debug.LogWarning(
                            $"[PlayerWarehouseInteraction] 背包已满，资源 {type} 已退回仓库 / Backpack full, {type} returned to warehouse");
                    }
                });

                break;
            }
        }

        // 从背包取出资源放入 Input 仓库 / Deposit resource from backpack to input warehouse
        private void TryDepositToWarehouse()
        {
            // 捕获当前引用 / Capture current references
            Warehouse warehouse = _currentWarehouse;
            if (warehouse == null) return;
            if (warehouse.IsFull()) return;

            PlayerBackpack backpack = _backpack;
            var stored = backpack.StoredResources;
            foreach (var kvp in stored)
            {
                ResourceType type = kvp.Key;
                if (kvp.Value <= 0) continue;
                if (!warehouse.CanAddResource(type, 1)) continue;

                // 立即从背包扣除 / Deduct from backpack immediately
                backpack.RemoveResource(type, 1);

                Transform startPoint = backpack.backpackAnchor;
                Transform endPoint = warehouse.ContentPoint;

                TransferManager.Instance.EnqueueTransfer(type, startPoint, endPoint, () =>
                {
                    // 动画完成后加入仓库；失败则退回背包 / Add to warehouse on arrival; return to backpack on failure
                    if (warehouse.CanAddResource(type, 1))
                    {
                        warehouse.AddResource(type, 1);
                    }
                    else if (backpack.CanAddResource(type, 1))
                    {
                        backpack.AddResource(type, 1);
                        Debug.LogWarning(
                            $"[PlayerWarehouseInteraction] 仓库已满或不可接受，资源 {type} 已退回背包 / Warehouse unavailable, {type} returned to backpack");
                    }
                });

                break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var warehouse = other.GetComponent<Warehouse>();
            if (warehouse != null)
                _currentWarehouse = warehouse;
        }

        private void OnTriggerExit(Collider other)
        {
            var warehouse = other.GetComponent<Warehouse>();
            if (warehouse != null && _currentWarehouse == warehouse)
            {
                _currentWarehouse = null;
                _transferTimer = 0f;
            }
        }
    }
}
