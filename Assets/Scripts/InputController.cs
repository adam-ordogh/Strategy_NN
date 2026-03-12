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


    public event Action OnSelectionChanged;

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleGlobalUIInput();
        HandleHoverLogic();

        if (activeBuildingType != null)
        {
            if (activeBuildingType.buildingType == Building.BuildingType.Road)
            {
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
                CancelBuildMode();
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
    }

    void HandleClick()
    {
        Vector2Int gridPos = GetGridPositionFromMouse();

        Building buildingAtPos = buildingManager.GetBuildingAtTile(gridPos);

        if (buildingAtPos != null && buildingAtPos.data.isSelectable == true)
        {
            if (buildingAtPos.isConstructed)
            {
                SelectBuilding(buildingAtPos);
            }
            else
            {
                Debug.Log("Construction in progress... " + buildingAtPos.turnsRemaining + " turns left.");
            }
            return;
        }

        if (mapData.units.TryGetValue(gridPos, out Unit unit))
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
                unitVisualizer.ShowHighlights(reachable, new Color(0, 0.5f, 1f, 0.4f));

                if (unit.canAttack)
                {
                    var attackCommands = unitManager.GetReachableTargets(unit);
                    var enemyPositions = attackCommands.Select(c => c.TargetPos);
                    unitVisualizer.ShowHighlights(enemyPositions, new Color(1f, 0, 0, 0.6f));
                }
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

        // Létrehozunk egy "szellem" épületet a kurzor pozíciójában
        Building ghost = new Building(activeBuildingType, gameManager.currentPlayerId, mousePos);
        var footprint = ghost.GetOccupiedTiles();

        bool isValid = buildingManager.CanPlaceBuilding(activeBuildingType, mousePos, gameManager.currentPlayerId) && buildingManager.CanAffordBuilding(activeBuildingType, gameManager.GetPlayerProfile(gameManager.currentPlayerId));

        unitVisualizer.ClearHighlights();
        Color ghostColor = isValid ? new Color(0, 1f, 0, 0.5f) : new Color(1f, 0, 0, 0.5f);
        unitVisualizer.ShowHighlights(footprint, ghostColor);
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

    private List<Vector2Int> GetRoadPath(Vector2Int start, Vector2Int end)
    {
        Func<Vector2Int, float> roadCostFunc = (pos) => 1.0f;

        Func<Vector2Int, bool> roadValidFunc = (pos) =>
        {
            if (pos.x < 0 || pos.x >= mapData.mapWidth || pos.y < 0 || pos.y >= mapData.mapHeight) return false;

            if (float.IsInfinity(mapData.moveCostMap[pos.x, pos.y])) return false;

            Building buildingAtTile = buildingManager.GetBuildingAtTile(pos);
            if (buildingAtTile != null)
            {
                // Út az úton keresztül mehet, de más épület nem lehet ott
                if (buildingAtTile.buildingType != Building.BuildingType.Road)
                    return false;
            }

            return true;
        };

        return Pathfinder.FindPath(start, end, roadCostFunc, roadValidFunc);
    }

    private void HandleRoadDragging()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            dragStartPos = null;
            previewRoadPath = null; 
            unitVisualizer.ClearHighlights();
            return;
        }

        Vector2Int currentMousePos = GetGridPositionFromMouse();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            dragStartPos = currentMousePos;
        }

        if (dragStartPos.HasValue)
        {
            if (previewRoadPath == null || previewRoadPath.Count == 0 || currentMousePos != previewRoadPath.Last())
            {
                previewRoadPath = GetRoadPath(dragStartPos.Value, currentMousePos);
            }

            unitVisualizer.ClearHighlights();

            if (previewRoadPath != null && previewRoadPath.Count > 0)
            {
                unitVisualizer.ShowHighlights(previewRoadPath, new Color(0.4f, 0.6f, 1f, 0.5f));
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && dragStartPos.HasValue)
        {
            if (previewRoadPath != null)
            {
                foreach (var pos in previewRoadPath)
                {
                    Building placed = buildingManager.PlaceBuilding(activeBuildingType, pos, gameManager.currentPlayerId);
                    Debug.Log(activeBuildingType);
                }
            }

            dragStartPos = null;
            previewRoadPath = null;
            unitVisualizer.ClearHighlights();
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
