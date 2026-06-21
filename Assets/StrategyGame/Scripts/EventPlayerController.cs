using UnityEngine;

namespace StrategyGame
{
    public class EventPlayerController : MonoBehaviour
    {
        public const int LocalPlayerId = 0;

        [SerializeField] float moveCooldownSeconds = 0.75f;

        GridManager grid;
        Camera mainCamera;
        Unit selectedUnit;
        GridTile hoveredTile;
        float moveCooldown;

        public void Initialize(GridManager gridManager, Camera camera)
        {
            grid = gridManager;
            mainCamera = camera;
        }

        void Update()
        {
            if (grid == null || mainCamera == null || EventGameManager.Instance == null || !EventGameManager.Instance.MatchActive)
            {
                return;
            }

            moveCooldown -= Time.deltaTime;
            HandleHover();
            HandleClick();
        }

        void HandleHover()
        {
            var hitTile = RaycastTile(out var hitUnit);
            if (hoveredTile != hitTile)
            {
                if (hoveredTile != null && hoveredTile.Occupant != selectedUnit)
                {
                    hoveredTile.SetHovered(false);
                }

                hoveredTile = hitTile;
                if (hoveredTile != null && hoveredTile.Occupant != selectedUnit)
                {
                    hoveredTile.SetHovered(true);
                }
            }
        }

        void HandleClick()
        {
            if (!StrategyInput.LeftClickThisFrame)
            {
                return;
            }

            RaycastTile(out var hitUnit);

            if (hitUnit != null && hitUnit.PlayerId == LocalPlayerId && hitUnit.IsAlive && hitUnit.IsActiveInArena)
            {
                SelectUnit(hitUnit);
                return;
            }

            if (selectedUnit != null && hitUnit != null && hitUnit.PlayerId != LocalPlayerId && TryAttackSelectedUnit(hitUnit))
            {
                return;
            }

            if (selectedUnit != null && hoveredTile != null && TryMoveSelectedUnit(hoveredTile))
            {
                return;
            }

            ClearSelection();
        }

        void SelectUnit(Unit unit)
        {
            selectedUnit = unit;
            RefreshHighlights();
            EventGameManager.Instance?.LeaderboardUI?.UpdateStatus(
                $"Controlling {PlayerRegistry.GetName(LocalPlayerId)} — blue tiles move, click enemies in range to attack.");
        }

        void ClearSelection()
        {
            selectedUnit = null;
            grid.ClearHighlights();
        }

        void RefreshHighlights()
        {
            grid.ClearHighlights();
            if (selectedUnit == null)
            {
                return;
            }

            grid.GetTile(selectedUnit.Position)?.SetHighlight(TileHighlight.Selected);
            foreach (var tile in grid.GetTilesInMoveRange(selectedUnit.Position, selectedUnit.MoveRange, selectedUnit))
            {
                tile.SetHighlight(TileHighlight.Move);
            }

            foreach (var tile in grid.GetTilesInAttackRange(selectedUnit.Position, selectedUnit.AttackRange))
            {
                if (tile.Occupant != null && tile.Occupant.PlayerId != LocalPlayerId && tile.Occupant.IsAlive)
                {
                    tile.SetHighlight(TileHighlight.Attack);
                }
            }
        }

        bool TryAttackSelectedUnit(Unit target)
        {
            if (selectedUnit == null || target == null)
            {
                return false;
            }

            var combat = selectedUnit.GetComponent<UnitHealthAndCombat>();
            if (combat == null || !combat.TryAttackUnit(target))
            {
                return false;
            }

            RefreshHighlights();
            return true;
        }

        bool TryMoveSelectedUnit(GridTile targetTile)
        {
            if (moveCooldown > 0f || selectedUnit == null || targetTile == null || !selectedUnit.IsActiveInArena)
            {
                return false;
            }

            if (targetTile.Occupant != null && targetTile.Occupant != selectedUnit)
            {
                return false;
            }

            if (targetTile.Coordinates.ManhattanDistance(selectedUnit.Position) > selectedUnit.MoveRange)
            {
                return false;
            }

            grid.PlaceUnit(selectedUnit, targetTile.Coordinates);
            moveCooldown = GetMoveCooldownForUnit(selectedUnit);
            RefreshHighlights();
            return true;
        }

        float GetMoveCooldownForUnit(Unit unit)
        {
            var buffs = unit.GetComponent<UnitBuffs>();
            var multiplier = buffs != null ? buffs.GetMoveSpeedMultiplier() : 1f;
            return moveCooldownSeconds / multiplier;
        }

        GridTile RaycastTile(out Unit hitUnit)
        {
            hitUnit = null;
            var ray = mainCamera.ScreenPointToRay(StrategyInput.PointerScreenPosition);
            if (!Physics.Raycast(ray, out var hit, 200f))
            {
                return null;
            }

            hitUnit = hit.collider.GetComponent<Unit>();
            var tile = hit.collider.GetComponent<GridTile>();
            if (tile == null)
            {
                tile = hit.collider.GetComponentInParent<GridTile>();
            }

            if (hitUnit == null && tile?.Occupant != null)
            {
                hitUnit = tile.Occupant;
            }

            return tile;
        }
    }
}
