using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;

    private RectTransform rectTransform;

    void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        transform.position = mousePos + new Vector2(15, -15);
        //transform.position = mousePos + new Vector2(15, 15);
    }

    public void Show(BuildingData data)
    {
        gameObject.SetActive(true);
        titleText.text = data.buildingType.ToString();

        costText.text = $"<color=#FFD700>{data.goldCost}</color>   <sprite name=\"gold_resource_icon\"> | <color=#A52A2A>{data.woodCost}</color>   <sprite name=\"wood_resource_icon\">";

        descriptionText.text = $"{data.description}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}