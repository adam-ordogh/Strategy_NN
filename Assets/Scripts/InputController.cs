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
    public UnitVisualizer unitVisualizer;

    private Vector2Int? selectedUnitPos;

    void Update()
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

        if(Mouse.current.rightButton.wasPressedThisFrame && selectedUnitPos!=null)
        {
            DeselectUnit();
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
}
