using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public BuildingData buildingData;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipController.Instance.Show(buildingData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipController.Instance.Hide();
    }
}