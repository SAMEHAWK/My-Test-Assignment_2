using UnityEngine;
using Core;

// 建筑生产配置 SO — 单建筑配方与生产间隔（仓库容量在 Warehouse 组件上配置）
// Building production config SO — recipe and interval per building (warehouse capacity is on Warehouse component)
namespace Data
{
    [CreateAssetMenu(fileName = "BuildingProductionConfig", menuName = "Game/Building Production Config")]
    public class BuildingProductionConfig : ScriptableObject
    {
        [Tooltip("建筑名称 / Building display name")]
        public string buildingName;

        [Tooltip("生产间隔（秒）/ Production interval in seconds")]
        public float productionInterval = 3f;

        [Tooltip("每轮消耗的资源配方 / Resources consumed per production cycle")]
        public RecipeEntry[] inputRecipe;

        [Tooltip("每轮产出的资源配方 / Resources produced per production cycle")]
        public RecipeEntry[] outputRecipe;
    }

    [System.Serializable]
    public struct RecipeEntry
    {
        [Tooltip("资源类型 / Resource type")]
        public ResourceType type;

        [Tooltip("数量 / Amount")]
        public int amount;
    }
}
