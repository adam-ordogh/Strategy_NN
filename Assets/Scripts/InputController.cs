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

    private Vector2Int? selectedUnitPos;
    public Building.BuildingType? activeBuildingType;
    public Building selectedBuilding;

    private Vector2Int? dragStartPos;
    private List<Vector2Int> previewRoadPath = new List<Vector2Int>();


    public event Action OnSelectionChanged;

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (activeBuildingType.HasValue)
        {
            if (activeBuildingType == Building.BuildingType.Road)
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

            if (Mouse.current.rightButton.wasPressedThisFrame && selectedUnitPos != null)
            {
                DeselectUnit();
            }
        }
    }

    void HandleClick()
    {
        Vector2Int gridPos = GetGridPositionFromMouse();

        Building buildingAtPos = buildingManager.GetBuildingAtTile(gridPos);

        if (buildingAtPos != null && buildingAtPos.data.isSelectable == true && buildingAtPos.ownerId == gameManager.currentPlayerId)
        {
            SelectBuilding(buildingAtPos);
            return;
        }

        if (mapData.units.TryGetValue(gridPos, out Unit unit))
        {
            if (unit.ownerId == gameManager.currentPlayerId)
            {
                SelectUnit(gridPos);
                return;
            }
        }

        if (selectedUnitPos.HasValue)
        {
            HandleUnitAction(gridPos);
            return;
        }

        DeselectAll();
        
    }

    public void HandleUnitAction(Vector2Int clickedPos)
    {
        bool unitAtClickedPos = mapManager.mapData.units.TryGetValue(clickedPos, out Unit clickedUnit);

        if (unitAtClickedPos)
        {
            if (selectedUnitPos.HasValue && clickedUnit.ownerId != gameManager.currentPlayerId)
            {
                Unit attacker = mapManager.mapData.units[selectedUnitPos.Value];
                Vector2Int? attackTile = unitManager.GetBestAttackPosition(attacker, clickedPos);

                if (attackTile.HasValue)
                {
                    if (attackTile.Value != attacker.position)
                    {
                        unitManager.TryMoveUnit(attacker.position, attackTile.Value);
                    }

                    unitManager.TryAttackUnit(attacker.position, clickedPos);

                    DeselectUnit();
                }
            }
            else if (clickedUnit.ownerId == gameManager.currentPlayerId)
            {
                SelectUnit(clickedPos);
            }
        }
        else if (selectedUnitPos.HasValue)
        {
            unitManager.TryMoveUnit(selectedUnitPos.Value, clickedPos);
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
            if (unit.ownerId != gameManager.currentPlayerId) return;
            selectedUnitPos = clickedPos;

            var reachable = unitManager.GetReachableTilesWithCost(unit).Keys;
            unitVisualizer.ShowHighlights(reachable, new Color(0, 0.5f, 1f, 0.4f));

            if (unit.canAttack)
            {
                var attackCommands = unitManager.GetReachableEnemies(unit);
                var enemyPositions = attackCommands.Select(c => c.TargetPos);
                unitVisualizer.ShowHighlights(enemyPositions, new Color(1f, 0, 0, 0.6f));
            }
        }
    }

    public void DeselectUnit()
    {
        selectedUnitPos = null;
        unitVisualizer.ClearHighlights();
    }

    private void SelectBuilding(Building building)
    {   
        DeselectUnit();
        selectedBuilding = building;

        unitVisualizer.ClearHighlights();
        unitVisualizer.ShowHighlights(building.GetOccupiedTiles(), new Color(1, 1, 0, 0.4f));

        // UI Hook: Itt hozzon elő egy UI elemet az épülethez kapcsolódó műveletekkel
        if (building.buildingType == Building.BuildingType.Barracks &&
            building.ownerId == gameManager.currentPlayerId)
        {
            Debug.Log("Selected Barracks: Ready to produce units.");
        }

        OnSelectionChanged?.Invoke();
    }

    private void DeselectBuilding()
    {
        selectedBuilding = null;
        unitVisualizer.ClearHighlights();
        Debug.Log("Deselected Building");

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
        BuildingData template = buildingManager.buildingTemplates.Find(t => t.buildingType == activeBuildingType.Value);
        Building ghost = new Building(template, gameManager.currentPlayerId, mousePos);
        var footprint = ghost.GetOccupiedTiles();

        bool isValid = buildingManager.CanPlaceBuilding(mousePos, ghost.size, gameManager.currentPlayerId, template.buildingType) && buildingManager.CanAffordBuilding(template, gameManager.GetPlayerProfile(gameManager.currentPlayerId));

        unitVisualizer.ClearHighlights();
        Color ghostColor = isValid ? new Color(0, 1f, 0, 0.5f) : new Color(1f, 0, 0, 0.5f);
        unitVisualizer.ShowHighlights(footprint, ghostColor);
    }

    private void TryPlaceBuilding()
    {
        Vector2Int mousePos = GetGridPositionFromMouse();
        Building placed = buildingManager.PlaceBuilding(activeBuildingType.Value, mousePos, gameManager.currentPlayerId);

        if (placed != null)
        {
            buildingVisualizer.ShowBuilding(placed);
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
                    Building placed = buildingManager.PlaceBuilding(Building.BuildingType.Road, pos, gameManager.currentPlayerId);
                    if (placed != null)
                    {
                        buildingVisualizer.ShowBuilding(placed);
                    }
                }
            }

            dragStartPos = null;
            previewRoadPath = null;
            unitVisualizer.ClearHighlights();
        }
    }
}
