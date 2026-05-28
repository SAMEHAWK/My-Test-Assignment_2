using System.Collections.Generic;
using UnityEngine;
using Data;
using Core;
using Transfer;
using Util;

// 背包视图 — 订阅背包变化，在角色背上按 X×Z 网格分层堆叠显示资源块
// Backpack view — subscribes to backpack changes, displays blocks as layered X×Z grid on player back
namespace Player
{
    public class BackpackView : MonoBehaviour
    {
        [Tooltip("资源数据库引用 / Reference to resource database")]
        [SerializeField] private ResourceDatabase resourceDatabase;

        [Tooltip("每行方块数量（X 方向）/ Blocks per row (X axis)")]
        [SerializeField] private int columns = 3;

        [Tooltip("每层行数（Z 方向）/ Rows per layer (Z axis)")]
        [SerializeField] private int rowsPerLayer = 3;

        [Tooltip("列间距（X 方向）/ Horizontal spacing between columns")]
        [SerializeField] private float horizontalSpacing = 0.35f;

        [Tooltip("行间距（Z 方向）/ Depth spacing between rows")]
        [SerializeField] private float depthSpacing = 0.35f;

        [Tooltip("层高间距（Y 方向）/ Layer height (Y axis)")]
        [SerializeField] private float layerHeight = 0.35f;

        private PlayerBackpack _backpack;
        private readonly List<GameObject> _spawnedBlocks = new();

        private void Awake()
        {
            _backpack = GetComponent<PlayerBackpack>();
        }

        private void OnEnable()
        {
            if (_backpack != null)
                _backpack.OnBackpackChanged.AddListener(RefreshView);
        }

        private void OnDisable()
        {
            if (_backpack != null)
                _backpack.OnBackpackChanged.RemoveListener(RefreshView);
        }

        private void Start()
        {
            RefreshView();
        }

        // 重建所有资源块的视觉显示 / Rebuild visual display of all resource blocks
        private void RefreshView()
        {
            // 清除旧块（归还对象池） / Clear old blocks (return to pool)
            foreach (var block in _spawnedBlocks)
            {
                if (block != null) ResourceObjectPool.Instance?.Return(block);
            }
            _spawnedBlocks.Clear();

            if (_backpack == null || resourceDatabase == null) return;
            if (_backpack.backpackAnchor == null) return;

            int index = 0;
            foreach (var kvp in _backpack.StoredResources)
            {
                ResourceType type = kvp.Key;
                int count = kvp.Value;

                var entry = resourceDatabase.GetResourceData(type);
                if (entry == null || entry.backpackPrefab == null) continue;

                for (int i = 0; i < count; i++)
                {
                    Vector3 localPos = GridLayoutHelper.GetGridPosition(index, columns, rowsPerLayer, horizontalSpacing, depthSpacing, layerHeight);
                    Vector3 worldPos = _backpack.backpackAnchor.TransformPoint(localPos);
                    Quaternion rot = _backpack.backpackAnchor.rotation;

                    var block = ResourceObjectPool.Instance?.Get(entry.backpackPrefab, worldPos, rot, _backpack.backpackAnchor);
                    if (block != null)
                        _spawnedBlocks.Add(block);
                    index++;
                }
            }
        }

    }
}
