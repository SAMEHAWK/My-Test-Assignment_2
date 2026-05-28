using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Core;

// 仓库组件 — 管理单一仓库的库存、容量和触发检测
// Warehouse component — manages inventory, capacity, and trigger detection for a single warehouse
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

        // 当前库存 / Current inventory
        private Dictionary<ResourceType, int> storedResources = new();
        public IReadOnlyDictionary<ResourceType, int> StoredResources => storedResources;

        [Tooltip("运行时库存快照（仅调试查看） / Runtime inventory snapshot (debug read-only)")]
        [SerializeField] private string inventoryDebug = "空 / Empty";

        [Tooltip("资源添加时触发 / Fired when a resource is added")]
        public UnityEvent<ResourceType, int> OnResourceAdded;

        [Tooltip("资源移除时触发 / Fired when a resource is removed")]
        public UnityEvent<ResourceType, int> OnResourceRemoved;

        // 检查是否接受该资源类型 / Check if this resource type is accepted
        public bool AcceptsType(ResourceType type)
        {
            return acceptedTypes.Count == 0 || acceptedTypes.Contains(type);
        }

        // 检查能否添加指定数量的资源 / Check if there is enough space to add the resource
        public bool CanAddResource(ResourceType type, int amount)
        {
            if (!AcceptsType(type))
                return false;
            return GetTotalStored() + amount <= capacity;
        }

        // 检查能否移除指定数量的资源 / Check if there is enough stock to remove the resource
        public bool CanRemoveResource(ResourceType type, int amount)
        {
            return HasResources(type, amount);
        }

        // 添加资源到仓库 / Add resources to warehouse
        public void AddResource(ResourceType type, int amount)
        {
            if (!CanAddResource(type, amount))
                return;

            if (!storedResources.ContainsKey(type))
                storedResources[type] = 0;

            storedResources[type] += amount;
            RefreshDebugDisplay();
            OnResourceAdded?.Invoke(type, amount);
        }

        // 从仓库移除资源 / Remove resources from warehouse
        public void RemoveResource(ResourceType type, int amount)
        {
            if (!CanRemoveResource(type, amount))
                return;

            storedResources[type] -= amount;
            if (storedResources[type] <= 0)
                storedResources.Remove(type);

            RefreshDebugDisplay();
            OnResourceRemoved?.Invoke(type, amount);
        }

        // 获取仓库中所有资源的总数量 / Get total count of all stored resources
        public int GetTotalStored()
        {
            int total = 0;
            foreach (var kvp in storedResources)
                total += kvp.Value;
            return total;
        }

        // 仓库是否已满 / Whether the warehouse is full
        public bool IsFull()
        {
            return GetTotalStored() >= capacity;
        }

        // 是否拥有足够数量的指定资源 / Whether the warehouse has enough of the specified resource
        public bool HasResources(ResourceType type, int amount)
        {
            return storedResources.TryGetValue(type, out int current) && current >= amount;
        }

        // 获取指定资源的库存数量 / Get stored amount of a specific resource type
        public int GetResourceCount(ResourceType type)
        {
            return storedResources.TryGetValue(type, out int count) ? count : 0;
        }

        // 更新 Inspector 调试显示 / Update inspector debug display
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
            foreach (var kvp in storedResources)
            {
                parts.Append($"[{kvp.Key}:{kvp.Value}] ");
            }
            inventoryDebug = parts.ToString().TrimEnd();
        }
    }
}
