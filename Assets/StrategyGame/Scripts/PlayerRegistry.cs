using UnityEngine;

namespace StrategyGame
{
    public static class PlayerRegistry
    {
        public const int PlayerCount = 10;

        static readonly string[] Names =
        {
            "Azure", "Crimson", "Verdant", "Golden", "Violet",
            "Coral", "Teal", "Amber", "Indigo", "Rose"
        };

        static readonly Color[] Colors =
        {
            new(0.20f, 0.45f, 0.95f),
            new(0.90f, 0.25f, 0.25f),
            new(0.25f, 0.80f, 0.35f),
            new(0.95f, 0.80f, 0.15f),
            new(0.65f, 0.30f, 0.90f),
            new(0.95f, 0.45f, 0.30f),
            new(0.20f, 0.75f, 0.75f),
            new(0.95f, 0.60f, 0.10f),
            new(0.35f, 0.35f, 0.85f),
            new(0.95f, 0.40f, 0.65f)
        };

        public static string GetName(int playerId) => Names[Mathf.Clamp(playerId, 0, PlayerCount - 1)];

        public static Color GetColor(int playerId) => Colors[Mathf.Clamp(playerId, 0, PlayerCount - 1)];
    }
}
