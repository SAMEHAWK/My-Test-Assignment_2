using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Core;

// 仓库组件 — 管理单一仓库的库存与容量（Trigger 交互在 PlayerWarehouseInteraction）
// Warehouse component — inventory and capacity (trigger interaction lives on PlayerWarehouseInteraction)
namespace Building
{
    public class Warehouse : MonoBehaviour
    {
        [Tooltip("仓库类型（Input 只进不出 / Output 只出不进）/ Warehouse type (Input: receive only, Output: give only)")]
        [SerializeField] private WarehouseType warehouseType;
        public WarehouseType Type => warehouseType;

        [Tooltip("仓库容量上限 / Maximum storage capacity")]
        [SerializeField] private int capacity = 10;
        public int Capacity => capacity;

        [Tooltip("资源物体 Lerp 移动的起点/终点锚点 / Anchor point for resource transfer Lerp animation")]
        [SerializeField] private Transform contentPoint;
        public Transform ContentPoint => contentPoint;

        [Tooltip("建筑产出点（仅 Output 仓库使用，资源从此处生成）/ Spawn point for produced resources (used by Output warehouse only)")]
        [SerializeField] private Transform spawnPoint;
        public Transform SpawnPoint => spawnPoint;

        [Tooltip("允许存放的资源类型（空=全部允许）/ Accepted resource types for storage (empty = accept all)")]
        [SerializeField] private List<ResourceType> acceptedTypes = new();
        public IReadOnlyList<ResourceType> AcceptedTypes => acceptedTypes;

        private ResourceInventory _inventory;
        public IReadOnlyDictionary<ResourceType, int> StoredResources => _inventory.StoredResources;

        [Tooltip("运行时库存快照（仅调试查看） / Runtime inventory snapshot (debug read-only)")]
        [SerializeField] private string inventoryDebug = "空 / Empty";

        [Tooltip("资源添加时触发 / Fired when a resource is added")]
        public UnityEvent<ResourceType, int> OnResourceAdded;

        [Tooltip("资源移除时触发 / Fired when a resource is removed")]
        public UnityEvent<ResourceType, int> OnResourceRemoved;

        private void Awake()
        {
            _inventory = new ResourceInventory(capacity);
            OnResourceAdded ??= new UnityEvent<ResourceType, int>();
            OnResourceRemoved ??= new UnityEvent<ResourceType, int>();
        }

        public bool AcceptsType(ResourceType type)
        {
            return acceptedTypes.Count == 0 || acceptedTypes.Contains(type);
        }

        public bool CanAddResource(ResourceType type, int amount)
        {
            return _inventory.CanAdd(type, amount, AcceptsType);
        }

        public bool CanRemoveResource(ResourceType type, int amount)
        {
            return _inventory.CanRemove(type, amount);
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (!_inventory.TryAdd(type, amount, AcceptsType))
                return;

            RefreshDebugDisplay();
            OnResourceAdded?.Invoke(type, amount);
        }

        public void RemoveResource(ResourceType type, int amount)
        {
            if (!_inventory.TryRemove(type, amount))
                return;

            RefreshDebugDisplay();
            OnResourceRemoved?.Invoke(type, amount);
        }

        public int GetTotalStored() => _inventory.GetTotalStored();

        public bool IsFull() => _inventory.IsFull();

        public bool HasResources(ResourceType type, int amount) => _inventory.HasResources(type, amount);

        public int GetResourceCount(ResourceType type) => _inventory.GetResourceCount(type);

        private void RefreshDebugDisplay()
        {
            int total = GetTotalStored();
            if (total == 0)
            {
                inventoryDebug = "空 / Empty";
                return;
            }

            var parts = new System.Text.StringBuilder();
            parts.Append($"{total}/{capacity}  ");
            foreach (var kvp in StoredResources)
                parts.Append($"[{kvp.Key}:{kvp.Value}] ");
            inventoryDebug = parts.ToString().TrimEnd();
        }
    }
}
