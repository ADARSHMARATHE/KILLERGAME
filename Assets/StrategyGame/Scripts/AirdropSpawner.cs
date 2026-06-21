using UnityEngine;

namespace StrategyGame
{
    public class AirdropSpawner : MonoBehaviour
    {
        public const float SpawnIntervalSeconds = 60f;

        [SerializeField] float spawnIntervalSeconds = SpawnIntervalSeconds;
        [SerializeField] SupplyCrate supplyCratePrefab;

        GridManager grid;
        float spawnTimer;

        public void Initialize(GridManager gridManager)
        {
            grid = gridManager;
            TryLoadPrefab();
            spawnTimer = spawnIntervalSeconds;
        }

        void TryLoadPrefab()
        {
            if (supplyCratePrefab != null)
            {
                return;
            }

#if UNITY_EDITOR
            supplyCratePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<SupplyCrate>("Assets/StrategyGame/Prefabs/SupplyCrate.prefab");
#endif
        }

        void Update()
        {
            if (grid == null || EventGameManager.Instance == null || !EventGameManager.Instance.MatchActive)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            SpawnAirdrop();
            spawnTimer = spawnIntervalSeconds;
        }

        void SpawnAirdrop()
        {
            var coordinates = GetRandomMapCoordinate();
            var crate = InstantiateCrate();
            crate.transform.SetParent(transform, false);
            crate.Launch(grid, coordinates);
        }

        GridCoordinates GetRandomMapCoordinate()
        {
            return new GridCoordinates(Random.Range(0, grid.Width), Random.Range(0, grid.Height));
        }

        SupplyCrate InstantiateCrate()
        {
            if (supplyCratePrefab != null)
            {
                return Instantiate(supplyCratePrefab);
            }

            return SupplyCrate.CreateSupplyCratePrefabInstance();
        }
    }
}
