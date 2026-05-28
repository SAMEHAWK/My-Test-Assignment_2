// 建筑生产状态枚举 — 供 BuildingProduction 状态机和 UI 使用
// Building production state enum — used by BuildingProduction state machine and UI
namespace Core
{
    public enum BuildingState
    {
        Producing,      // 正常生产中 / Producing normally
        InputMissing,   // 缺少输入资源 / Missing input resources
        OutputFull      // 输出仓库已满 / Output warehouse is full
    }
}
