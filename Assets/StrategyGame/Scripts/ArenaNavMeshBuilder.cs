using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace StrategyGame
{
    public static class ArenaNavMeshBuilder
    {
        public static void BakeArenaNavMesh(Bounds bounds)
        {
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(
                bounds,
                ~0,
                NavMeshCollectGeometry.RenderMeshes,
                0,
                markups,
                sources);

            if (sources.Count == 0)
            {
                Debug.LogWarning("ArenaNavMeshBuilder: No NavMesh sources found.");
                return;
            }

            var settings = NavMesh.GetSettingsByID(0);
            var navMeshData = NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                bounds,
                Vector3.zero,
                Quaternion.identity);

            if (navMeshData != null)
            {
                NavMesh.AddNavMeshData(navMeshData);
            }
        }
    }
}
