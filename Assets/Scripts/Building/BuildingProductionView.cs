using UnityEngine;
using Core;

// 建筑生产状态视觉反馈 — 订阅 OnStateChanged，切换 Renderer 颜色（绿/红/黄）
// Building production visual feedback — subscribes to OnStateChanged, toggles Renderer color (green/red/yellow)
namespace Building
{
    public class BuildingProductionView : MonoBehaviour
    {
        [SerializeField, Tooltip("建筑生产组件（留空则从本物体查找）/ Building production (auto-find on this object if empty)")]
        private BuildingProduction building;

        [SerializeField, Tooltip("状态颜色目标 Renderer（留空则从子级查找）/ Target renderer (auto-find in children if empty)")]
        private Renderer targetRenderer;

        [SerializeField, Tooltip("正常生产颜色 / Color when producing")]
        private Color producingColor = new Color(0.3f, 0.85f, 0.35f);

        [SerializeField, Tooltip("缺料颜色 / Color when input missing")]
        private Color inputMissingColor = new Color(0.9f, 0.25f, 0.25f);

        [SerializeField, Tooltip("满库颜色 / Color when output full")]
        private Color outputFullColor = new Color(0.95f, 0.85f, 0.2f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            if (building == null)
                building = GetComponent<BuildingProduction>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (building == null) return;

            building.OnStateChanged.AddListener(OnStateChanged);
            OnStateChanged(building.CurrentState);
        }

        private void OnDisable()
        {
            if (building != null)
                building.OnStateChanged.RemoveListener(OnStateChanged);
        }

        private void OnStateChanged(BuildingState state)
        {
            if (targetRenderer == null) return;

            Color color = state switch
            {
                BuildingState.InputMissing => inputMissingColor,
                BuildingState.OutputFull => outputFullColor,
                _ => producingColor
            };

            ApplyColor(color);
        }

        private void ApplyColor(Color color)
        {
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
