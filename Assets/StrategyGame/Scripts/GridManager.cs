using System.Collections.Generic;
using UnityEngine;

namespace StrategyGame
{
    public class GridManager : MonoBehaviour
    {
        public int Width { get; private set; } = 8;
        public int Height { get; private set; } = 8;
        public float TileSize { get; private set; } = 1f;

        readonly Dictionary<GridCoordinates, GridTile> tiles = new();

        public void BuildGrid(int width, int height, float tileSize)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;

            var gridRoot = new GameObject("Grid");
            gridRoot.transform.SetParent(transform, false);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var coordinates = new GridCoordinates(x, y);
                    var tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tileObject.name = $"Tile_{x}_{y}";
                    tileObject.transform.SetParent(gridRoot.transform, false);
                    tileObject.transform.position = GridToWorld(coordinates);

                    var tile = tileObject.AddComponent<GridTile>();
                    tile.Initialize(coordinates, tileSize);
                    tiles[coordinates] = tile;
                }
            }
        }

        public bool IsInsideGrid(GridCoordinates coordinates)
        {
            return coordinates.X >= 0 && coordinates.X < Width &&
                   coordinates.Y >= 0 && coordinates.Y < Height;
        }

        public bool TryGetTile(GridCoordinates coordinates, out GridTile tile)
        {
            return tiles.TryGetValue(coordinates, out tile);
        }

        public GridTile GetTile(GridCoordinates coordinates)
        {
            tiles.TryGetValue(coordinates, out var tile);
            return tile;
        }

        public Vector3 GridToWorld(GridCoordinates coordinates)
        {
            var offsetX = (Width - 1) * TileSize * 0.5f;
            var offsetZ = (Height - 1) * TileSize * 0.5f;
            return new Vector3(coordinates.X * TileSize - offsetX, 0f, coordinates.Y * TileSize - offsetZ);
        }

        public IEnumerable<GridTile> GetTilesInMoveRange(GridCoordinates origin, int range, Unit movingUnit)
        {
            foreach (var pair in tiles)
            {
                if (pair.Key.ManhattanDistance(origin) > range)
                {
                    continue;
                }

                if (pair.Key.Equals(origin))
                {
                    yield return pair.Value;
                    continue;
                }

                if (pair.Value.IsWalkable || pair.Value.Occupant == movingUnit)
                {
                    yield return pair.Value;
                }
            }
        }

        public IEnumerable<GridTile> GetTilesInAttackRange(GridCoordinates origin, int range)
        {
            foreach (var pair in tiles)
            {
                if (pair.Key.ManhattanDistance(origin) <= range && pair.Value.Occupant != null)
                {
                    yield return pair.Value;
                }
            }
        }

        public void ClearHighlights()
        {
            foreach (var tile in tiles.Values)
            {
                tile.SetHighlight(TileHighlight.None);
                tile.SetHovered(false);
            }
        }

        public void PlaceUnit(Unit unit, GridCoordinates coordinates)
        {
            var tile = GetTile(coordinates);
            if (tile == null)
            {
                return;
            }

            if (TryGetTile(unit.Position, out var oldTile) && oldTile.Occupant == unit)
            {
                oldTile.SetOccupant(null);
            }

            tile.SetOccupant(unit);
            unit.SetPosition(coordinates);
            unit.transform.position = GridToWorld(coordinates) + Vector3.up * 0.35f;
        }
    }
}
