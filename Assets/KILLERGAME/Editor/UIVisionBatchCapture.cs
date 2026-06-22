#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KillerGame.Editor
{
    /// <summary>
    /// Headless Game-view style capture for ui_vision_loop.py.
    /// Invoked via: Unity -batchmode -executeMethod KillerGame.Editor.UIVisionBatchCapture.Run
    /// </summary>
    public static class UIVisionBatchCapture
    {
        const string ScenePath = "Assets/KILLERGAME/Scenes/KillerGameScene.unity";
        const int Width = 1080;
        const int Height = 1920;
        const int WarmupFrames = 45;

        static int _framesWaited;
        static bool _captureScheduled;

        public static void Run()
        {
            var output = ResolveOutputPath();
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            _framesWaited = 0;
            _captureScheduled = true;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogError("[UIVisionBatchCapture] Scene save cancelled.");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                _framesWaited = 0;
        }

        static void OnEditorUpdate()
        {
            if (!_captureScheduled)
                return;

            if (!EditorApplication.isPlaying)
                return;

            _framesWaited++;
            if (_framesWaited < WarmupFrames)
                return;

            _captureScheduled = false;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;

            try
            {
                CaptureMainCamera(ResolveOutputPath());
                Debug.Log($"[UIVisionBatchCapture] Saved screenshot to {ResolveOutputPath()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIVisionBatchCapture] Capture failed: {ex}");
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        static string ResolveOutputPath()
        {
            var env = Environment.GetEnvironmentVariable("UI_VISION_OUTPUT");
            if (!string.IsNullOrWhiteSpace(env))
                return env;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            return Path.Combine(projectRoot ?? Application.dataPath, "current_state.png");
        }

        static void CaptureMainCamera(string outputPath)
        {
            var camera = Camera.main;
            if (camera == null)
                camera = UnityEngine.Object.FindAnyObjectByType<Camera>();

            if (camera == null)
                throw new InvalidOperationException("No camera found in KillerGameScene play mode.");

            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;

            var rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();

                File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
#endif
