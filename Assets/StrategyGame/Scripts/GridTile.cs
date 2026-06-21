using UnityEngine;

namespace StrategyGame
{
    public enum TileHighlight
    {
        None,
        Move,
        Attack,
        Selected
    }

    public class GridTile : MonoBehaviour
    {
        public GridCoordinates Coordinates { get; private set; }
        public Unit Occupant { get; private set; }

        static readonly Color BaseColor = new(0.35f, 0.38f, 0.42f);
        static readonly Color MoveColor = new(0.25f, 0.55f, 0.95f);
        static readonly Color AttackColor = new(0.95f, 0.35f, 0.3f);
        static readonly Color SelectedColor = new(0.95f, 0.85f, 0.2f);
        static readonly Color HoverColor = new(0.5f, 0.55f, 0.6f);

        Renderer tileRenderer;
        MaterialPropertyBlock propertyBlock;
        TileHighlight highlight = TileHighlight.None;
        bool isHovered;

        public bool IsWalkable => Occupant == null;

        public void Initialize(GridCoordinates coordinates, float tileSize)
        {
            Coordinates = coordinates;
            tileRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            transform.localScale = new Vector3(tileSize * 0.95f, 0.15f, tileSize * 0.95f);
            ApplyColor();
        }

        public void SetOccupant(Unit unit)
        {
            Occupant = unit;
        }

        public void SetHighlight(TileHighlight value)
        {
            highlight = value;
            ApplyColor();
        }

        public void SetHovered(bool hovered)
        {
            isHovered = hovered;
            ApplyColor();
        }

        void ApplyColor()
        {
            Color color = highlight switch
            {
                TileHighlight.Move => MoveColor,
                TileHighlight.Attack => AttackColor,
                TileHighlight.Selected => SelectedColor,
                _ => isHovered ? HoverColor : BaseColor
            };

            tileRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            tileRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
