using System.Collections.Generic;
using UnityEngine;
using Core;
using Building;
using Transfer;
using Data;

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

        [SerializeField, Tooltip("传输管理器（留空则使用单例）/ Transfer manager (uses singleton if empty)")]
        private TransferManager transferManager;

        private PlayerBackpack _backpack;
        private Warehouse _currentWarehouse;
        private float _transferTimer;

        private TransferManager _transfer;

        private void Awake() {
            _backpack = GetComponent<PlayerBackpack>();
            _transfer = transferManager != null ? transferManager : TransferManager.Instance;
            if (_transfer == null)
                Debug.LogError("[PlayerWarehouseInteraction] TransferManager not found.", this);
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

            Warehouse warehouse = _currentWarehouse;
            if (warehouse == null) return;

            var stored = warehouse.StoredResources;
            foreach (var kvp in stored)
            {
                ResourceType type = kvp.Key;
                if (kvp.Value <= 0) continue;
                if (!_backpack.CanAddResource(type)) continue;

                warehouse.RemoveResource(type, 1);

                Transform startPoint = warehouse.ContentPoint;
                Transform endPoint = _backpack.backpackAnchor;
                PlayerBackpack backpack = _backpack;

                _transfer.EnqueueTransfer(type, startPoint, endPoint, () =>
                {
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
            Warehouse warehouse = _currentWarehouse;
            if (warehouse == null) return;
            if (warehouse.IsFull()) return;

            if (!TrySelectDepositType(warehouse, out ResourceType type))
                return;

            PlayerBackpack backpack = _backpack;
            backpack.RemoveResource(type, 1);

            Transform startPoint = backpack.backpackAnchor;
            Transform endPoint = warehouse.ContentPoint;

            _transfer.EnqueueTransfer(type, startPoint, endPoint, () =>
            {
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
        }

        // 按建筑 inputRecipe 优先选择仍缺料的资源类型 / Prefer recipe types still missing in input warehouse
        private bool TrySelectDepositType(Warehouse warehouse, out ResourceType selectedType)
        {
            selectedType = default;

            var building = warehouse.GetComponentInParent<BuildingProduction>();
            RecipeEntry[] recipe = building?.Config?.inputRecipe;

            if (recipe != null && recipe.Length > 0)
            {
                foreach (var entry in recipe)
                {
                    if (!_backpack.CanRemoveResource(entry.type)) continue;
                    if (!warehouse.CanAddResource(entry.type, 1)) continue;

                    int current = warehouse.GetResourceCount(entry.type);
                    if (current < entry.amount)
                    {
                        selectedType = entry.type;
                        return true;
                    }
                }

                foreach (var entry in recipe)
                {
                    if (!_backpack.CanRemoveResource(entry.type)) continue;
                    if (!warehouse.CanAddResource(entry.type, 1)) continue;

                    selectedType = entry.type;
                    return true;
                }
            }

            foreach (var kvp in _backpack.StoredResources)
            {
                if (kvp.Value <= 0) continue;
                if (!warehouse.CanAddResource(kvp.Key, 1)) continue;

                selectedType = kvp.Key;
                return true;
            }

            return false;
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
