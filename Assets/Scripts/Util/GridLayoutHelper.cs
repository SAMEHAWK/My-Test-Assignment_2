using UnityEngine;

// 网格布局工具 — 按 X×Z 网格分层计算方块位置，供 WarehouseView 和 BackpackView 共用
// Grid layout helper — calculates block positions in layered X×Z grid, shared by WarehouseView and BackpackView
namespace Util
{
    public static class GridLayoutHelper
    {
        // 根据索引计算网格局部坐标（XZ 平面铺开，Y 轴层叠）
        // Calculate local grid position from index (XZ plane grid, Y axis stacking)
        public static Vector3 GetGridPosition(int index, int columns, int rowsPerLayer,
            float horizontalSpacing, float depthSpacing, float layerHeight)
        {
            int blocksPerLayer = columns * rowsPerLayer;
            int layer = index / blocksPerLayer;
            int indexInLayer = index % blocksPerLayer;
            int rowInLayer = indexInLayer / columns;
            int col = indexInLayer % columns;

            float x = (col - (columns - 1) * 0.5f) * horizontalSpacing;
            float y = layer * layerHeight;
            float z = (rowInLayer - (rowsPerLayer - 1) * 0.5f) * depthSpacing;

            return new Vector3(x, y, z);
        }
    }
}
