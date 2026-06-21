using UnityEngine;
using UnityEngine.UI;

namespace StrategyGame
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] int gridWidth = 8;
        [SerializeField] int gridHeight = 8;
        [SerializeField] float tileSize = 1f;

        void Start()
        {
            SetupCamera();
            SetupLighting();

            var gridObject = new GameObject("GridManager");
            var grid = gridObject.AddComponent<GridManager>();
            grid.BuildGrid(gridWidth, gridHeight, tileSize);

            var turnObject = new GameObject("TurnManager");
            var turnManager = turnObject.AddComponent<TurnManager>();

            var uiObject = new GameObject("GameUI");
            var gameUI = uiObject.AddComponent<GameUI>();

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var gameManagerObject = new GameObject("GameManager");
            var gameManager = gameManagerObject.AddComponent<GameManager>();
            gameUI.Build(canvas, gameManager);
            gameManager.Initialize(grid, turnManager, gameUI);

            var inputObject = new GameObject("SelectionController");
            var selection = inputObject.AddComponent<SelectionController>();
            selection.Initialize(grid, Camera.main);
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

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            camera.transform.position = new Vector3(0f, 10f, 0f);

            if (camera.GetComponent<IsometricCamera>() == null)
            {
                camera.gameObject.AddComponent<IsometricCamera>();
            }
        }

        static void SetupLighting()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
