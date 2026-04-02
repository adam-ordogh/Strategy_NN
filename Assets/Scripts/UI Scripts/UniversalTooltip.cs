using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UniversalTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Simple Mode (Use for generic UI)")]
    public string title;
    [TextArea] public string description;

    [Header("Data Mode (Optional)")]
    public BuildingData buildingData;
    public UnitData unitData;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buildingData != null)
        {
            string costLine = "";

            if (buildingData.woodCost > 0)
            {
                costLine += $"<color=#A52A2A>{buildingData.woodCost}</color>  <sprite name=\"wood_resource_icon\"> ";
            }

            if (buildingData.goldCost > 0)
                costLine += $"<color=#FFD700>{buildingData.goldCost}</color>  <sprite name=\"gold_resource_icon\"> ";

            if (costLine != "") costLine += "| ";
            costLine += $"<color=#be9b7b>{buildingData.constructionTurns}</color>  <sprite name=\"turn_icon\">";

            TooltipController.Instance.Show(buildingData.buildingName, costLine, buildingData.description);
        }

        else if (unitData != null)
        {
            string costLine = "";

            if (unitData.foodCost > 0)
            {
                costLine += $"<color=#00FF00>{unitData.foodCost}</color>  <sprite name=\"food_resource_icon_02\"> ";
            }

            if (unitData.woodCost > 0)
            {
                costLine += $"<color=#A52A2A>{unitData.woodCost}</color>  <sprite name=\"wood_resource_icon\"> ";
            }

            if (unitData.goldCost > 0)
                costLine += $"<color=#FFD700>{unitData.goldCost}</color>  <sprite name=\"gold_resource_icon\"> ";

            if (costLine != "") costLine += "| ";
            costLine += $"<color=#40826D>{unitData.populationCost}</color>  <sprite name=\"population_resource_icon\">";

            if (costLine != "") costLine += "| ";
            costLine += $"<color=#be9b7b>{unitData.trainingTime}</color>  <sprite name=\"turn_icon\">";

            TooltipController.Instance.Show(unitData.unitName, costLine, unitData.description);
        }

        else
        {
            TooltipController.Instance.Show(title, "", description);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipController.Instance.Hide();
    }
}