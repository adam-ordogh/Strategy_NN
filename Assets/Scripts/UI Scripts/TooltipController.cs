using TMPro;
using UnityEngine.InputSystem;
using UnityEngine;

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

        float pivotX = mousePos.x / Screen.width > 0.5f ? 1.05f : -0.05f;
        float pivotY = mousePos.y / Screen.height > 0.5f ? 1.05f : -0.05f;

        rectTransform.pivot = new Vector2(pivotX, pivotY);
        transform.position = mousePos;
    }

    public void Show(string title, string cost = "", string description = "")
    {
        gameObject.SetActive(true);
        titleText.text = title;

        costText.gameObject.SetActive(!string.IsNullOrEmpty(cost));
        costText.text = cost;

        descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
        descriptionText.text = description;
    }

    public void Hide() => gameObject.SetActive(false);
}