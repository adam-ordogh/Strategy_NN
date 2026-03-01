using UnityEngine;
using TMPro;

public class WorkerBarController : MonoBehaviour
{
    public TextMeshProUGUI iconsText;
    private Building targetBuilding;

    private bool isHovered = false;
    private bool forceShowAll = false;

    public static System.Action<bool> OnToggleGlobalShow;

    private void Awake()
    {
        OnToggleGlobalShow += SetForceShowAll;
    }

    private void OnDestroy()
    {
        OnToggleGlobalShow -= SetForceShowAll;

        if (targetBuilding != null)
        {
            targetBuilding.OnWorkersChanged -= HandleWorkersChanged;
        }
    }

    public void Setup(Building building)
    {
        targetBuilding = building;

        if (targetBuilding.data.jobSlotsProvided > 0)
        {
            targetBuilding.OnWorkersChanged += HandleWorkersChanged;

            UpdateIcons();
           gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void HandleWorkersChanged(int currentWorkers)
    {
        UpdateIcons();
    }


    public void UpdateIcons()
    {
        if (targetBuilding == null || targetBuilding.data.jobSlotsProvided == 0) return;

        string iconString = "";
        for (int i = 0; i < targetBuilding.data.jobSlotsProvided; i++)
        {
            if (i < targetBuilding.assignedWorkers)
            {
                iconString += "<sprite name=\"assignedWorker_icon\">  ";
            }
            else
            {
                iconString += "<sprite name=\"unassignedWorker_icon\">  ";
            }
        }

        iconsText.text = iconString;
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        RefreshVisibility();
    }

    public void SetForceShowAll(bool force)
    {
        forceShowAll = force;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        gameObject.SetActive(isHovered || forceShowAll);
    }
}