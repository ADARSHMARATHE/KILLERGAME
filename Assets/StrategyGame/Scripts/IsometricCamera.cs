using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace StrategyGame
{
    [RequireComponent(typeof(Camera))]
    public class IsometricCamera : MonoBehaviour
    {
        [SerializeField] Vector3 orbitOffset = new(-28f, 38f, -28f);
        [SerializeField] float panSpeed = 18f;
        [SerializeField] float middleMousePanSensitivity = 0.04f;
        [SerializeField] float zoomSpeed = 0.12f;
        [SerializeField] float minOrthographicSize = 8f;
        [SerializeField] float maxOrthographicSize = 80f;
        [SerializeField] float playerFocusOrthographicSize = 14f;
        [SerializeField] float arenaOverviewSizeMultiplier = 0.72f;

        Camera cam;
        Vector3 focusPoint = Vector3.zero;

        void Awake()
        {
            cam = GetComponent<Camera>();
            EventCameraSetup.Configure(cam);
            ShowFullArena();
        }

        void Update()
        {
            HandlePan();
            HandleZoom();
            HandleRefocus();
        }

        public void ShowFullArena(float arenaSize = 100f)
        {
            focusPoint = Vector3.zero;
            cam.orthographicSize = Mathf.Clamp(
                arenaSize * arenaOverviewSizeMultiplier,
                minOrthographicSize,
                maxOrthographicSize);
            ApplyFocus();
        }

        public void FocusOnPlayer()
        {
            var local = PlayerCharacterController.LocalPlayer;
            if (local == null)
            {
                return;
            }

            focusPoint = local.transform.position;
            cam.orthographicSize = Mathf.Clamp(playerFocusOrthographicSize, minOrthographicSize, maxOrthographicSize);
            ApplyFocus();
        }

        public void FocusOn(Vector3 worldPoint, bool usePlayerZoom = false)
        {
            focusPoint = worldPoint;
            if (usePlayerZoom)
            {
                cam.orthographicSize = Mathf.Clamp(playerFocusOrthographicSize, minOrthographicSize, maxOrthographicSize);
            }

            ApplyFocus();
        }

        void ApplyFocus()
        {
            var lookTarget = focusPoint + Vector3.up * 1f;
            transform.position = lookTarget + orbitOffset;
            transform.LookAt(lookTarget);
        }

        void HandleRefocus()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                FocusOnPlayer();
            }
            else if (keyboard.gKey.wasPressedThisFrame)
            {
                ShowFullArena();
            }
        }

        void HandlePan()
        {
            var delta = Vector3.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed)
                {
                    delta += Vector3.forward;
                }

                if (keyboard.sKey.isPressed)
                {
                    delta += Vector3.back;
                }

                if (keyboard.aKey.isPressed)
                {
                    delta += Vector3.left;
                }

                if (keyboard.dKey.isPressed)
                {
                    delta += Vector3.right;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.middleButton.isPressed)
            {
                var drag = mouse.delta.ReadValue();
                delta += new Vector3(-drag.x, 0f, -drag.y) * middleMousePanSensitivity;
            }

            if (delta.sqrMagnitude <= 0f)
            {
                return;
            }

            focusPoint += delta.normalized * (panSpeed * Time.deltaTime);
            ApplyFocus();
        }

        void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var scroll = mouse.scroll.y.ReadValue();
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - scroll * zoomSpeed,
                minOrthographicSize,
                maxOrthographicSize);
        }
    }
}
