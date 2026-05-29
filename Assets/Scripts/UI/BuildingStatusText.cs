using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Core;

// 建筑状态文字 — 停产时查询仓库+配方自行格式化原因，恢复后切回建筑名
// Building status text — queries warehouse + recipe to format stop reason on its own
namespace UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class BuildingStatusText : MonoBehaviour
    {
        [SerializeField, Tooltip("建筑生产组件（留空则从父级查找）/ Building production (auto-find in parent if empty)")]
        private Building.BuildingProduction building;

        private TMP_Text textComponent;
        private string buildingName;

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            buildingName = textComponent.text.Trim();
        }

        private void OnEnable()
        {
            if (building == null)
                building = GetComponentInParent<Building.BuildingProduction>();

            if (building == null)
            {
                Debug.LogWarning($"[BuildingStatusText] No BuildingProduction found on {name}", this);
                return;
            }

            building.OnStateChanged.AddListener(OnStateChanged);
            BindWarehouseListeners(true);
            OnStateChanged(building.CurrentState);
        }

        private void OnDisable()
        {
            if (building == null) return;

            building.OnStateChanged.RemoveListener(OnStateChanged);
            BindWarehouseListeners(false);
        }

        // 绑定或解绑仓库库存变化监听 / Bind or unbind warehouse inventory change listeners
        private void BindWarehouseListeners(bool bind)
        {
            var input = building.InputWarehouse;
            var output = building.OutputWarehouse;

            if (bind)
            {
                if (input != null)
                    input.OnResourceAdded.AddListener(OnInputWarehouseChanged);
                if (output != null)
                {
                    output.OnResourceAdded.AddListener(OnOutputWarehouseChanged);
                    output.OnResourceRemoved.AddListener(OnOutputWarehouseChanged);
                }
            }
            else
            {
                if (input != null)
                    input.OnResourceAdded.RemoveListener(OnInputWarehouseChanged);
                if (output != null)
                {
                    output.OnResourceAdded.RemoveListener(OnOutputWarehouseChanged);
                    output.OnResourceRemoved.RemoveListener(OnOutputWarehouseChanged);
                }
            }
        }

        private void OnInputWarehouseChanged(ResourceType type, int amount)
        {
            if (building != null && building.CurrentState == BuildingState.InputMissing)
                OnStateChanged(BuildingState.InputMissing);
        }

        private void OnOutputWarehouseChanged(ResourceType type, int amount)
        {
            if (building != null && building.CurrentState == BuildingState.OutputFull)
                OnStateChanged(BuildingState.OutputFull);
        }

        private void OnStateChanged(BuildingState state)
        {
            if (textComponent == null) return;

            if (state == BuildingState.Producing)
            {
                textComponent.text = buildingName;
            }
            else if (state == BuildingState.InputMissing)
            {
                textComponent.text = $"{buildingName}\n{FormatInputMissing()}";
            }
            else if (state == BuildingState.OutputFull)
            {
                textComponent.text = $"{buildingName}\n{FormatOutputFull()}";
            }
        }

        private string FormatInputMissing()
        {
            var config = building?.Config;
            var warehouse = building?.InputWarehouse;
            if (config == null || warehouse == null) return "Input missing";

            var missing = new List<string>();
            foreach (var recipe in config.inputRecipe)
            {
                if (!warehouse.HasResources(recipe.type, recipe.amount))
                {
                    int current = warehouse.GetResourceCount(recipe.type);
                    missing.Add($"{recipe.type}x{recipe.amount}(has {current})");
                }
            }

            return missing.Count > 0 ? "Need " + string.Join(", ", missing) : "Ready";
        }

        private string FormatOutputFull()
        {
            var warehouse = building?.OutputWarehouse;
            if (warehouse == null) return "Output full";

            return $"Output full ({warehouse.GetTotalStored()}/{warehouse.Capacity})";
        }
    }
}
