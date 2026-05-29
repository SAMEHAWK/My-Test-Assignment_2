using UnityEngine;

// 资源传输配置 SO — Lerp 动画参数与对象池设置
// Resource transfer config SO — Lerp animation parameters and object pool settings
namespace Data
{
    [CreateAssetMenu(fileName = "ResourceTransferConfig", menuName = "Game/Resource Transfer Config")]
    public class ResourceTransferConfig : ScriptableObject
    {
        [Tooltip("资源飞行移动速度 / Movement speed of resource during transfer")]
        public float moveSpeed = 5f;

        [Tooltip("移动动画曲线（X=归一化时间 0~1, Y=进度 0~1）/ Movement curve over normalized time")]
        public AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("对象池初始容量 / Initial object pool size")]
        public int initialPoolSize = 10;

        [Tooltip("队列出队间隔（TransferManager 用）/ Queue dequeue interval for TransferManager")]
        public float transferInterval = 0.3f;

        [Tooltip("建筑生产多段动画错开间隔（秒）/ Stagger interval between building production transfer clips")]
        public float productionStaggerInterval = 0.15f;

        [Tooltip("抛物线弧高（Y 轴偏移峰值）/ Parabolic arc height (peak Y offset)")]
        public float arcHeight = 2f;
    }
}
