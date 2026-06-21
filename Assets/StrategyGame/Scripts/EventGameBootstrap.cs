using UnityEngine;
using UnityEngine.UI;

namespace StrategyGame
{
    public class EventGameBootstrap : MonoBehaviour
    {
        [SerializeField] int gridWidth = 12;
        [SerializeField] int gridHeight = 12;
        [SerializeField] float tileSize = 1f;
        [SerializeField] bool useNavMeshAgents = true;

        void Start()
        {
            SetupCamera();
            SetupLighting();

            var gridObject = new GameObject("GridManager");
            var grid = gridObject.AddComponent<GridManager>();
            grid.BuildGrid(gridWidth, gridHeight, tileSize);

            var arenaObject = new GameObject("ArenaLayoutGenerator");
            var arenaLayout = arenaObject.AddComponent<ArenaLayoutGenerator>();
            arenaLayout.Generate();

            var isoCamera = Camera.main != null ? Camera.main.GetComponent<IsometricCamera>() : null;
            isoCamera?.ShowFullArena(arenaLayout.ArenaSize);

            ArenaNavMeshBuilder.BakeArenaNavMesh(new Bounds(Vector3.zero, new Vector3(120f, 20f, 120f)));

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var uiObject = new GameObject("EventLeaderboardUI");
            var leaderboardUI = uiObject.AddComponent<EventLeaderboardUI>();
            leaderboardUI.Build(canvas);

            var eventManagerObject = new GameObject("EventGameManager");
            var eventManager = eventManagerObject.AddComponent<EventGameManager>();
            eventManager.Initialize(grid, leaderboardUI);

            if (useNavMeshAgents)
            {
                var spawnerObject = new GameObject("AIPlayerSpawner");
                var spawner = spawnerObject.AddComponent<AIPlayerSpawner>();
                spawner.SpawnPlayers(arenaLayout, eventManager);

                var worldCaptureObject = new GameObject("WorldCaptureManager");
                var worldCapture = worldCaptureObject.AddComponent<WorldCaptureManager>();
                worldCapture.Initialize(eventManager, leaderboardUI);
            }
            else
            {
                var inputObject = new GameObject("EventPlayerController");
                var playerController = inputObject.AddComponent<EventPlayerController>();
                playerController.Initialize(grid, Camera.main);

                var airdropObject = new GameObject("AirdropSpawner");
                var airdropSpawner = airdropObject.AddComponent<AirdropSpawner>();
                airdropSpawner.Initialize(grid);
            }
        }

        static void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            EventCameraSetup.Configure(camera);

            if (camera.GetComponent<IsometricCamera>() == null)
            {
                camera.gameObject.AddComponent<IsometricCamera>();
            }
        }

        static void SetupLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.37f, 0.42f);

            if (Object.FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = Color.white;
            lightObject.transform.rotation = Quaternion.Euler(52f, -25f, 0f);
        }
    }
}
