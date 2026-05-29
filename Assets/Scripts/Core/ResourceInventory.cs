using System;
using System.Collections.Generic;

namespace Core
{
    // 共享库存 Domain — 容量限制、按类型计数（Warehouse / PlayerBackpack 组合使用）
    // Shared inventory domain — capacity-limited per-type counts (composed by Warehouse / PlayerBackpack)
    public class ResourceInventory
    {
        private readonly Dictionary<ResourceType, int> _stored = new();
        private readonly int _capacity;

        public ResourceInventory(int capacity)
        {
            _capacity = Math.Max(0, capacity);
        }

        public int Capacity => _capacity;
        public IReadOnlyDictionary<ResourceType, int> StoredResources => _stored;

        public int GetTotalStored()
        {
            int total = 0;
            foreach (var kvp in _stored)
                total += kvp.Value;
            return total;
        }

        public bool IsFull() => GetTotalStored() >= _capacity;

        public int GetResourceCount(ResourceType type)
        {
            return _stored.TryGetValue(type, out int count) ? count : 0;
        }

        public bool HasResources(ResourceType type, int amount)
        {
            if (amount <= 0) return true;
            return GetResourceCount(type) >= amount;
        }

        public bool CanAdd(ResourceType type, int amount, Func<ResourceType, bool> acceptFilter = null)
        {
            if (amount <= 0) return false;
            if (acceptFilter != null && !acceptFilter(type)) return false;
            return GetTotalStored() + amount <= _capacity;
        }

        public bool CanRemove(ResourceType type, int amount)
        {
            if (amount <= 0) return false;
            return HasResources(type, amount);
        }

        public bool TryAdd(ResourceType type, int amount, Func<ResourceType, bool> acceptFilter = null)
        {
            if (!CanAdd(type, amount, acceptFilter))
                return false;

            if (!_stored.ContainsKey(type))
                _stored[type] = 0;
            _stored[type] += amount;
            return true;
        }

        public bool TryRemove(ResourceType type, int amount)
        {
            if (!CanRemove(type, amount))
                return false;

            _stored[type] -= amount;
            if (_stored[type] <= 0)
                _stored.Remove(type);
            return true;
        }

        // 移除至多 amount 个（背包语义：允许超额请求时清空该类型）
        // Remove up to amount units (backpack semantics: clamp when amount exceeds stock)
        public int RemoveUpTo(ResourceType type, int amount)
        {
            if (amount <= 0) return 0;
            int current = GetResourceCount(type);
            if (current <= 0) return 0;

            int toRemove = Math.Min(amount, current);
            _stored[type] = current - toRemove;
            if (_stored[type] <= 0)
                _stored.Remove(type);
            return toRemove;
        }
    }
}
