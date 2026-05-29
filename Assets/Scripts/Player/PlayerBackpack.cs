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

        private ResourceInventory _inventory;
        public System.Collections.Generic.IReadOnlyDictionary<ResourceType, int> StoredResources =>
            _inventory.StoredResources;

        [Tooltip("背包内容变化时触发 / Fired when backpack contents change")]
        public UnityEvent OnBackpackChanged;

        private void Awake()
        {
            _inventory = new ResourceInventory(_capacity);
            OnBackpackChanged ??= new UnityEvent();
        }

        public bool CanAddResource(ResourceType type, int amount = 1)
        {
            return _inventory.CanAdd(type, amount);
        }

        public bool CanRemoveResource(ResourceType type)
        {
            return _inventory.GetResourceCount(type) > 0;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (!_inventory.TryAdd(type, amount))
                return;

            RefreshDebugDisplay();
            OnBackpackChanged?.Invoke();
        }

        public void RemoveResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            if (_inventory.RemoveUpTo(type, amount) <= 0)
                return;

            RefreshDebugDisplay();
            OnBackpackChanged?.Invoke();
        }

        public int GetTotalStored() => _inventory.GetTotalStored();

        public bool IsFull() => _inventory.IsFull();

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
            foreach (var kvp in StoredResources)
                parts.Append($"[{kvp.Key}:{kvp.Value}] ");
            inventoryDebug = parts.ToString().TrimEnd();
        }
    }
}
