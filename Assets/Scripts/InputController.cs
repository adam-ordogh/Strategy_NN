using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEditor.U2D.Aseprite;
using static UnityEditor.PlayerSettings;
using System.Collections.Generic;

public class InputController : MonoBehaviour
{
    public GameUIController uiController;

    public MapData mapData;
    public Camera mainCamera;
    public Tilemap groundTilemap;

    public MapManager mapManager;
    public GameManager gameManager;
    public UnitManager unitManager;
    public BuildingManager buildingManager;
    public UnitVisualizer unitVisualizer;
    public BuildingVisualizer buildingVisualizer;

    public Unit selectedUnit;
    public BuildingData activeBuildingType;
    public Building selectedBuilding;
    private Building currentlyHoveredBuilding;

    private Vector2Int? dragStartPos;
    private List<Vector2Int> previewRoadPath = new List<Vector2Int>();

    private GameObject ghostPreview;
    private SpriteRenderer ghostRenderer;

    public event Action OnSelectionChanged;

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (uiController.saveLoadMenuPanel.activeSelf)
            {
                uiController.CloseAllMenus();
            }
            else
            {
                uiController.TogglePauseMenu();
            }
        }

        if (GameUIController.IsAnyMenuOpen)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleGlobalUIInput();
        HandleHoverLogic();

        if (activeBuildingType != null)
        {
            unitVisualizer.ClearHighlights();

            if (activeBuildingType.buildingType == Building.BuildingType.Road)
            {
                if (ghostPreview != null) ghostPreview.SetActive(false);
                HandleRoadDragging();
            }
            else
            {
                HandleBuildingPreview();

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    TryPlaceBuilding();
                }
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelBuildMode();
                TooltipController.Instance.gameObject.SetActive(false);
            }
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (unitVisualizer.IsBusy())
                {
                    unitVisualizer.FastForward();
                    return;
                }
                HandleClick();
            }

            if (Mouse.current.rightButton.wasPressedThisFrame && selectedUnit != null)
            {
                DeselectUnit();
            }
        }

        if (activeBuildingType == null || activeBuildingType.buildingType == Building.BuildingType.Road)
        {
            if (ghostPreview != null && ghostPreview.activeSelf)
                ghostPreview.SetActive(false);
        }
    }

    void HandleClick()
    {
        Vector2Int gridPos = GetGridPositionFromMouse();

        Building buildingAtPos = buildingManager.GetBuildingAtTile(gridPos);
        mapData.units.TryGetValue(gridPos, out Unit unitAtPos);

        if (selectedUnit != null && selectedUnit.ownerId == gameManager.currentPlayerId)
        {
            bool targetIsEnemyUnit = unitAtPos != null && unitAtPos.ownerId != gameManager.currentPlayerId;
            bool targetIsEnemyBuilding = buildingAtPos != null && buildingAtPos.ownerId != gameManager.currentPlayerId;

            if (targetIsEnemyUnit || targetIsEnemyBuilding)
            {
                HandleUnitAction(gridPos);
                return;
            }
        }

        if (buildingAtPos != null && buildingAtPos.data.isSelectable)
        {
            if (buildingAtPos.isConstructed)
            {
                SelectBuilding(buildingAtPos);
            }
            else
            {
                Debug.Log($"Construction in progress... {buildingAtPos.turnsRemaining} turns left.");
            }
            return;
        }

        if (unitAtPos != null)
        {
            SelectUnit(gridPos);
            return;
        }

        if (selectedUnit != null && selectedUnit.ownerId == gameManager.currentPlayerId)
        {
            HandleUnitAction(gridPos);
            return;
        }

        DeselectAll();
    }

    public void HandleUnitAction(Vector2Int clickedPos)
    {
        // Egység interakciók kezelése
        if (mapManager.mapData.units.TryGetValue(clickedPos, out Unit clickedUnit))
        {
            if (selectedUnit != null && clickedUnit.ownerId != gameManager.currentPlayerId)
            {
                PerformMoveAndAttack(selectedUnit, clickedPos);
            }
            else if (clickedUnit.ownerId == gameManager.currentPlayerId)
            {
                SelectUnit(clickedPos);
            }
            return;
        }

        // Épület interakciók kezelése
        Building clickedBuilding = buildingManager.GetBuildingAtTile(clickedPos);
        if (clickedBuilding != null && clickedBuilding.data.isSelectable && selectedUnit != null)
        {
            if (clickedBuilding.ownerId != gameManager.currentPlayerId)
            {
                PerformMoveAndAttack(selectedUnit, clickedPos);
                DeselectUnit();
                return;
            }
        }

        // Mozgás kezelése
        if (selectedUnit != null)
        {
            unitManager.TryMoveUnit(selectedUnit.position, clickedPos);
            DeselectUnit();
        }
    }

    private void PerformMoveAndAttack(Unit attacker, Vector2Int targetPos)
    {
        // GetBestAttackPosition most már kezeli az épületeket is
        Vector2Int? attackTile = unitManager.GetBestAttackPosition(attacker, targetPos);

        if (attackTile.HasValue)
        {
            if (attackTile.Value != attacker.position)
            {
                unitManager.TryMoveUnit(attacker.position, attackTile.Value);
            }

            unitManager.TryAttack(attacker.position, targetPos);

            DeselectUnit();
        }
    }

    private Vector2Int GetGridPositionFromMouse()
    {
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3Int cell = groundTilemap.WorldToCell(mouseWorld);
        return new Vector2Int(cell.x, cell.y);
    }

    public void SelectUnit(Vector2Int clickedPos)
    {
        unitVisualizer.ClearHighlights();

        if (mapManager.mapData.units.TryGetValue(clickedPos, out Unit unit))
        {
            selectedUnit = unit;
            DeselectBuilding(); 

            if (unit.ownerId == gameManager.currentPlayerId)
            {
                var reachable = unitManager.GetReachableTilesWithCost(unit).Keys;
                //unitVisualizer.ShowHighlights(reachable, new Color(0, 0.5f, 1f, 0.4f));
                unitVisualizer.ShowHighlights(reachable, new Color(0, 0.5f, 1f));

                if (unit.canAttack)
                {
                    var attackCommands = unitManager.GetReachableTargets(unit);
                    var enemyPositions = attackCommands.Select(c => c.TargetPos);
                    //unitVisualizer.ShowHighlights(enemyPositions, new Color(1f, 0, 0, 0.6f));
                    unitVisualizer.ShowHighlights(enemyPositions, new Color(1f, 0, 0));
                }
            }
            else
            {
                unitVisualizer.ShowHighlight(unit.position, new Color(1, 1, 0, 0.4f));
            }

            OnSelectionChanged?.Invoke(); 
        }
    }

    public void DeselectUnit()
    {
        selectedUnit = null;
        unitVisualizer.ClearHighlights();
        OnSelectionChanged?.Invoke(); 
    }

    private void SelectBuilding(Building building)
    {
        selectedUnit = null; 
        selectedBuilding = building;

        unitVisualizer.ClearHighlights();
        unitVisualizer.ShowHighlights(building.GetOccupiedTiles(), new Color(1, 1, 0, 0.4f));

        OnSelectionChanged?.Invoke(); 
    }

    public void DeselectBuilding()
    {
        selectedBuilding = null;
        unitVisualizer.ClearHighlights();
        OnSelectionChanged?.Invoke();
    }

    private void DeselectAll()
    {
        DeselectUnit();
        DeselectBuilding();
    }

    private void HandleBuildingPreview()
    {
        Vector2Int mousePos = GetGridPositionFromMouse();

        bool canPlace = buildingManager.CanPlaceBuilding(activeBuildingType, mousePos, gameManager.currentPlayerId);
        bool canAfford = buildingManager.CanAffordBuilding(activeBuildingType, gameManager.GetPlayerProfile(gameManager.currentPlayerId));
        bool isValid = canPlace && canAfford;

        unitVisualizer.ClearHighlights();
        Building tempGhost = new Building(activeBuildingType, gameManager.currentPlayerId, mousePos);
        unitVisualizer.ShowHighlights(tempGhost.GetOccupiedTiles(), isValid ? new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f));

        if (ghostPreview == null)
        {
            ghostPreview = new GameObject("BuildingGhost");
            ghostRenderer = ghostPreview.AddComponent<SpriteRenderer>();
            ghostRenderer.sortingLayerName = "WorldObjects";
            ghostRenderer.sortingOrder = 50; 
        }

        ghostPreview.SetActive(true);
        ghostRenderer.sprite = activeBuildingType.buildingSprite;

        ghostPreview.transform.position = new Vector3(mousePos.x, mousePos.y, 0);

        ghostRenderer.color = isValid ? new Color(0.5f, 1f, 0.5f, 0.6f) : new Color(1f, 0.5f, 0.5f, 0.6f);
    }

    private void TryPlaceBuilding()
    {
        Vector2Int mousePos = GetGridPositionFromMouse();
        Building placed = buildingManager.PlaceBuilding(activeBuildingType, mousePos, gameManager.currentPlayerId);

        if (placed != null)
        {
            CancelBuildMode();
        }
    }

    private void CancelBuildMode()
    {
        activeBuildingType = null;
        unitVisualizer.ClearHighlights();
    }

    private List<Vector2Int> GetPathBetween(Vector2Int start, Vector2Int end)
    {
        var path = Pathfinder.FindPath(
            start,
            end,
            pos => 1f, 
            pos => pos.x >= 0 && pos.x < mapData.mapWidth && pos.y >= 0 && pos.y < mapData.mapHeight
        );

        return path ?? new List<Vector2Int>();
    }

    private void HandleRoadDragging()
    {
        //dragStartPos = null;

        Vector2Int mousePos = GetGridPositionFromMouse();
        var player = gameManager.GetPlayerProfile(gameManager.currentPlayerId);

        // Hover
        if (!dragStartPos.HasValue)
        {
            unitVisualizer.ShowHighlight(mousePos, new Color(1f, 0.9f, 0f, 0.4f));
        }

        if (Mouse.current.leftButton.wasPressedThisFrame) dragStartPos = mousePos;

        // Előnézet
        if (dragStartPos.HasValue)
        {
            previewRoadPath = GetPathBetween(dragStartPos.Value, mousePos);

            int totalGold = previewRoadPath.Count * activeBuildingType.goldCost;
            int totalWood = previewRoadPath.Count * activeBuildingType.woodCost;

            bool canAffordAll = player.CanAfford(totalGold, totalWood, 0);

            Color pathColor = canAffordAll ? new Color(0.2f, 0.5f, 1f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
            unitVisualizer.ShowHighlights(previewRoadPath, pathColor);

            // Tooltip
            TooltipController.Instance.Show(
                "Út építés",
                $"Költség: {totalGold}  <sprite name=\"gold_resource_icon\"> | {totalWood}  <sprite name=\"wood_resource_icon\">",
                canAffordAll ? "Engedje el az építéshez" : "<color=red>Kevés alapanyag!</color>"
            );
        }

        // Helyezés
        if (Mouse.current.leftButton.wasReleasedThisFrame && dragStartPos.HasValue)
        {
            int totalGold = previewRoadPath.Count * activeBuildingType.goldCost;
            int totalWood = previewRoadPath.Count * activeBuildingType.woodCost;

            if (player.CanAfford(totalGold, totalWood, 0))
            {
                foreach (var pos in previewRoadPath)
                {
                    if (buildingManager.CanPlaceBuilding(activeBuildingType, pos, player.playerId))
                    {
                        buildingManager.PlaceBuilding(activeBuildingType, pos, player.playerId);
                    }
                }
            }
            else
            {
                Debug.Log("Cannot afford the full road path.");
            }

            activeBuildingType = null;
            dragStartPos = null;
            previewRoadPath = null;
            unitVisualizer.ClearHighlights();
            TooltipController.Instance.gameObject.SetActive(false); 
        }
    }

    private void HandleGlobalUIInput()
    {
        bool isAltPressed = Keyboard.current.leftAltKey.isPressed;

        WorkerBarController.OnToggleGlobalShow?.Invoke(isAltPressed);
    }

    private void HandleHoverLogic()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        Vector2Int tilePos = new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));

        Building hovered = buildingManager.GetBuildingAtTile(tilePos);

        if (hovered != currentlyHoveredBuilding)
        {
            if (currentlyHoveredBuilding != null)
                ToggleWorkerBar(currentlyHoveredBuilding, false);

            if (hovered != null)
                ToggleWorkerBar(hovered, true);

            currentlyHoveredBuilding = hovered;
        }
    }
    private void ToggleWorkerBar(Building building, bool show)
    {
        GameObject visual = buildingVisualizer.GetVisualInstance(building);
        if (visual != null)
        {
            var wb = visual.GetComponentInChildren<WorkerBarController>(true);
            if (wb != null) wb.SetHovered(show);
        }
    }
}
