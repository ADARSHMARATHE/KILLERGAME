using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StrategyGame
{
    public static class EventCameraSetup
    {
        public static void Configure(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.15f, 0.17f, 0.22f);
            camera.orthographic = true;
            camera.orthographicSize = 72f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.depth = -1;
            camera.enabled = true;

            var urpData = camera.GetUniversalAdditionalCameraData();
            urpData.renderType = CameraRenderType.Base;
            urpData.renderPostProcessing = false;
            urpData.antialiasing = AntialiasingMode.None;
        }
    }
}
