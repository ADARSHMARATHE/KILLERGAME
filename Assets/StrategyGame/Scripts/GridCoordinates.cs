using System;

namespace StrategyGame
{
    [Serializable]
    public struct GridCoordinates : IEquatable<GridCoordinates>
    {
        public int X;
        public int Y;

        public GridCoordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int ManhattanDistance(GridCoordinates other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public bool Equals(GridCoordinates other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridCoordinates other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X}, {Y})";
    }
}
