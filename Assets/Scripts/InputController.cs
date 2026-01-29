using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System.Linq;

public class InputController : MonoBehaviour
{
    public Camera mainCamera;
    public Tilemap groundTilemap;

    public MapManager mapManager;
    public GameManager gameManager;
    public UnitManager unitManager;
    public BuildingManager buildingManager;
    public UnitVisualizer unitVisualizer;
    public BuildingVisualizer buildingVisualizer;

    private Vector2Int? selectedUnitPos;
    public Building.BuildingType? activeBuildingType; // Null if not building
    void Update()
    {

        if (activeBuildingType.HasValue)
        {
            HandleBuildingPreview();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPlaceBuilding();
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
        Vector2Int clickedPos = GetGridPositionFromMouse();

        // Debug.Log($"Clicked at {clickedPos}");
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
        else if(selectedUnitPos.HasValue)
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

    private void HandleBuildingPreview()
    {
        Vector2Int mousePos = GetGridPositionFromMouse();

        // Létrehozunk egy "szellem" épületet a kurzor pozíciójában
        Building ghost = new Building(activeBuildingType.Value, gameManager.currentPlayerId, mousePos);
        var footprint = ghost.GetOccupiedTiles();

        bool isValid = buildingManager.CanPlaceBuilding(mousePos, ghost.size, gameManager.currentPlayerId);

        // Use UnitVisualizer to show the ghost footprint
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
}
