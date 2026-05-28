using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Core;

// 玩家背包 — 容量限制的库存管理（纯数据层）
// Player backpack — capacity-limited inventory management (pure data layer)
namespace Player
{
    public class PlayerBackpack : MonoBehaviour
    {
        [SerializeField, Tooltip("背包容量上限 / Maximum backpack capacity")]
        private int _capacity = 20;
        public int capacity => _capacity;

        [Tooltip("资源块在角色背上的堆叠锚点 / Anchor point for stacked resource blocks on player back")]
        public Transform backpackAnchor;

        [Tooltip("运行时背包快照（仅调试查看） / Runtime inventory snapshot (debug read-only)")]
        [SerializeField] private string inventoryDebug = "空 / Empty";

        // 当前背包内资源库存 / Current resource inventory in backpack
        private Dictionary<ResourceType, int> storedResources = new();
        public IReadOnlyDictionary<ResourceType, int> StoredResources => storedResources;

        // 背包内容变化时触发 / Fired when backpack contents change
        public UnityEvent OnBackpackChanged;

        // 检查是否可以添加指定数量的资源 / Check if the specified amount can be added
        public bool CanAddResource(ResourceType type, int amount = 1)
        {
            if (amount <= 0) return false;
            return GetTotalStored() + amount <= _capacity;
        }

        // 检查是否至少有一个该类型资源可取 / Check if at least one unit of this type can be removed
        public bool CanRemoveResource(ResourceType type)
        {
            return storedResources.TryGetValue(type, out int count) && count > 0;
        }

        // 添加指定数量的资源 / Add specified amount of resource
        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            if (!CanAddResource(type, amount))
                return;

            if (!storedResources.ContainsKey(type))
                storedResources[type] = 0;
            storedResources[type] += amount;
            RefreshDebugDisplay();
            OnBackpackChanged?.Invoke();
        }

        // 移除指定数量的资源 / Remove specified amount of resource
        public void RemoveResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            if (!storedResources.TryGetValue(type, out int current)) return;
            int newCount = Mathf.Max(0, current - amount);
            if (newCount <= 0)
                storedResources.Remove(type);
            else
                storedResources[type] = newCount;
            RefreshDebugDisplay();
            OnBackpackChanged?.Invoke();
        }

        // 获取背包中所有资源总数 / Get total count of all resources in backpack
        public int GetTotalStored()
        {
            int total = 0;
            foreach (var kvp in storedResources)
                total += kvp.Value;
            return total;
        }

        // 背包是否已满 / Whether the backpack is full
        public bool IsFull()
        {
            return GetTotalStored() >= _capacity;
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
            parts.Append($"{total}/{_capacity}  ");
            foreach (var kvp in storedResources)
            {
                parts.Append($"[{kvp.Key}:{kvp.Value}] ");
            }
            inventoryDebug = parts.ToString().TrimEnd();
        }
    }
}
