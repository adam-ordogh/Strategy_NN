using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    public UnityEngine.UI.Slider slider;
    public UnityEngine.UI.Image fillImage;
    private Building targetBuilding;
    private Unit targetUnit;

    void Update()
    {
        // Force the health bar to always have a positive global scale 
        // so it doesn't mirror when the parent unit flips.
        Vector3 localScale = transform.localScale;

        // Check if the parent is flipped
        if (transform.parent != null && transform.parent.localScale.x < 0)
        {
            // If parent is flipped, we flip the local scale back to stay "normal"
            transform.localScale = new Vector3(-Mathf.Abs(localScale.x), localScale.y, localScale.z);
        }
        else
        {
            transform.localScale = new Vector3(Mathf.Abs(localScale.x), localScale.y, localScale.z);
        }
    }

    public void SetupForBuildings(Building building)
    {
        targetBuilding = building;
        targetBuilding.OnBuildingHealthChanged += UpdateBar;

        UpdateBar(building.currentHp, building.maxHealth);
    }

    public void SetupForUnits(Unit unit)
    {
        targetUnit = unit;
        targetUnit.OnUnitHealthChanged += UpdateBar;

        UpdateBar(unit.currentHealth, unit.data.maxHealth);
    }

    private void UpdateBar(int current, int max)
    {
        float percentage = (float)current / max;
        slider.value = percentage;

        // Optional: Hide if full health, show if damaged
        gameObject.SetActive(current < max && current > 0);

        // Color juice: Green to Red
        fillImage.color = Color.Lerp(Color.red, Color.green, percentage);
    }

    private void OnDestroy()
    {
        // Clean up to prevent memory leaks
        if (targetBuilding != null)
            targetBuilding.OnBuildingHealthChanged -= UpdateBar;

        if (targetUnit != null)
            targetUnit.OnUnitHealthChanged -= UpdateBar;
    }
}