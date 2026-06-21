using UnityEngine;

namespace StrategyGame
{
    public class SelectionController : MonoBehaviour
    {
        GridManager grid;
        Camera mainCamera;
        GridTile hoveredTile;

        public void Initialize(GridManager gridManager, Camera camera)
        {
            grid = gridManager;
            mainCamera = camera;
        }

        void Update()
        {
            if (grid == null || mainCamera == null)
            {
                return;
            }

            var ray = mainCamera.ScreenPointToRay(StrategyInput.PointerScreenPosition);
            GridTile hitTile = null;

            if (Physics.Raycast(ray, out var hit, 200f))
            {
                hitTile = hit.collider.GetComponent<GridTile>();
                if (hitTile == null)
                {
                    hitTile = hit.collider.GetComponentInParent<GridTile>();
                }
            }

            if (hoveredTile != hitTile)
            {
                hoveredTile?.SetHovered(false);
                hoveredTile = hitTile;
                hoveredTile?.SetHovered(true);
            }

            if (StrategyInput.LeftClickThisFrame && hitTile != null)
            {
                GameManager.Instance?.HandleTileClicked(hitTile);
            }
        }
    }
}
