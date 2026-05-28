using System.Collections.Generic;
using UnityEngine;
using Core;
using Data;
using Transfer;
using Util;

// 仓库资源可视化 — 按 X×Z 网格分层堆叠显示仓库中的资源方块
// Warehouse resource visualization — displays stored resources as layered X×Z grid of blocks
namespace Building
{
    public class WarehouseView : MonoBehaviour
    {
        [Tooltip("要显示的仓库 / Warehouse to visualize")]
        [SerializeField] private Warehouse warehouse;

        [Tooltip("资源数据库 / Resource database")]
        [SerializeField] private ResourceDatabase resourceDatabase;

        [Tooltip("每行方块数量（X 方向）/ Blocks per row (X axis)")]
        [SerializeField] private int columns = 3;

        [Tooltip("每层行数（Z 方向）/ Rows per layer (Z axis)")]
        [SerializeField] private int rowsPerLayer = 3;

        [Tooltip("列间距（X 方向）/ Horizontal spacing between columns")]
        [SerializeField] private float horizontalSpacing = 0.5f;

        [Tooltip("行间距（Z 方向）/ Depth spacing between rows")]
        [SerializeField] private float depthSpacing = 0.5f;

        [Tooltip("层高间距（Y 方向，一层堆满后向上）/ Layer height (Y axis, when a layer fills up)")]
        [SerializeField] private float layerHeight = 0.5f;

        [Tooltip("显示锚点（留空则使用仓库的 ContentPoint）/ Display anchor (uses warehouse ContentPoint if empty)")]
        [SerializeField] private Transform displayAnchor;

        // 已生成的方块列表 / List of spawned blocks
        private readonly List<GameObject> spawnedBlocks = new();

        private void Start()
        {
            if (warehouse == null)
                warehouse = GetComponent<Warehouse>();

            if (displayAnchor == null && warehouse != null)
                displayAnchor = warehouse.ContentPoint;

            if (warehouse != null)
            {
                warehouse.OnResourceAdded.AddListener(OnResourceChanged);
                warehouse.OnResourceRemoved.AddListener(OnResourceChanged);
                RefreshView();
            }
        }

        private void OnDestroy()
        {
            if (warehouse != null)
            {
                warehouse.OnResourceAdded.RemoveListener(OnResourceChanged);
                warehouse.OnResourceRemoved.RemoveListener(OnResourceChanged);
            }
        }

        private void OnResourceChanged(ResourceType type, int amount)
        {
            RefreshView();
        }

        // 重建所有资源块的视觉显示 / Rebuild visual display of all resource blocks
        public void RefreshView()
        {
            ClearBlocks();

            if (warehouse == null || resourceDatabase == null || displayAnchor == null)
                return;

            int index = 0;
            foreach (var kvp in warehouse.StoredResources)
            {
                ResourceType type = kvp.Key;
                int count = kvp.Value;

                ResourceEntry entry = resourceDatabase.GetResourceData(type);
                if (entry == null || entry.backpackPrefab == null)
                    continue;

                for (int i = 0; i < count; i++)
                {
                    Vector3 localPos = GridLayoutHelper.GetGridPosition(index, columns, rowsPerLayer, horizontalSpacing, depthSpacing, layerHeight);
                    Vector3 worldPos = displayAnchor.TransformPoint(localPos);
                    Quaternion rot = displayAnchor.rotation;

                    GameObject block = ResourceObjectPool.Instance?.Get(entry.backpackPrefab, worldPos, rot, displayAnchor);
                    if (block != null)
                        spawnedBlocks.Add(block);
                    index++;
                }
            }
        }

        // 清除所有方块 / Clear all spawned blocks
        private void ClearBlocks()
        {
            foreach (var block in spawnedBlocks)
            {
                if (block != null)
                    ResourceObjectPool.Instance?.Return(block);
            }
            spawnedBlocks.Clear();
        }
    }
}
