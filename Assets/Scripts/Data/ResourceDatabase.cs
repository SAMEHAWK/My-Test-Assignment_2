using UnityEngine;
using Core;

// 资源数据库 SO — 定义每种资源的元数据和视觉表现
// Resource database SO — defines metadata and visual representation for each resource type
namespace Data
{
    [CreateAssetMenu(fileName = "ResourceDatabase", menuName = "Game/Resource Database")]
    public class ResourceDatabase : ScriptableObject
    {
        [Tooltip("所有资源类型的元数据列表 / List of resource metadata entries")]
        public ResourceEntry[] resources;

        // 根据类型获取资源数据 / Get resource data by type
        public ResourceEntry GetResourceData(ResourceType type)
        {
            foreach (var entry in resources)
            {
                if (entry.type == type)
                    return entry;
            }
            return null;
        }
    }

    [System.Serializable]
    public class ResourceEntry
    {
        [Tooltip("资源类型 / Resource type")]
        public ResourceType type;

        [Tooltip("显示名称 / Display name")]
        public string displayName;

        [Tooltip("飞行中显示的小方块 Prefab / Cube prefab shown during transfer animation")]
        public GameObject transferPrefab;

        [Tooltip("角色背上的堆叠块 Prefab / Stacked block prefab displayed on player back")]
        public GameObject backpackPrefab;
    }
}
