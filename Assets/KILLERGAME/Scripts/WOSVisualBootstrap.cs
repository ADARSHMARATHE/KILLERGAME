using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KillerGame
{
    /// <summary>
    /// WOS-style 3D isometric city view with transparent UI overlays on top.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WOSVisualBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (Object.FindAnyObjectByType<WOSVisualBootstrap>() != null) return;
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                canvas.gameObject.AddComponent<WOSVisualBootstrap>();
            else
                new GameObject("WOSVisualBootstrap").AddComponent<WOSVisualBootstrap>();
        }

        static readonly Color GoldText     = new Color(1f, 0.86f, 0.32f);
        static readonly Color AmberCost    = new Color(0.96f, 0.65f, 0.14f);
        static readonly Color BlueLevel    = new Color(0.55f, 0.82f, 1f);
        static readonly Color TabBarBg     = new Color(0.02f, 0.03f, 0.07f, 0.88f);
        static readonly Color HudBg        = new Color(0.02f, 0.03f, 0.07f, 0.72f);
        static readonly Color SheetBg      = new Color(0.04f, 0.07f, 0.13f, 0.38f);
        static readonly Color FurnaceSheet  = new Color(0.03f, 0.05f, 0.09f, 0.28f);

        const string Bg2DName     = "WOS_CityBackground";
        const string TopVigName   = "WOS_TopVignette";
        const string BotVigName   = "WOS_BottomVignette";

        void Awake()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var cam = Setup3DCamera();
            SetupCanvasOverlay(canvas, cam);
            Hide2DBackground(canvas);
            EnsureSkyBackdrop(canvas.transform);
            Setup3DCity();
            RefreshCamera();
            ApplyAtmosphere();
            EnsureVignettes(canvas.transform);
            WOSUIStyler.Apply(canvas);
            ApplySnow();
        }

        const float CameraPitch = 38f;
        const float CameraYaw   = 30f;
        static readonly Vector3 CityLookAt = new Vector3(0f, 2f, 0f);

        static Camera Setup3DCamera()
        {
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return null;

            cam.enabled = true;
            cam.cullingMask = ~0;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.08f, 0.14f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 250f;
            cam.depth = -1;

            var bounds = GetCityBounds();
            var lookAt = CityLookAt;
            if (bounds.size.sqrMagnitude > 0.01f)
                lookAt = new Vector3(bounds.center.x, CityLookAt.y, bounds.center.z);

            cam.orthographic = true;
            cam.orthographicSize = ComputeOrthoSize(bounds);

            var rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            cam.transform.rotation = rotation;
            float back = Mathf.Max(bounds.extents.magnitude, 32f) * 0.92f + 18f;
            cam.transform.position = lookAt - rotation * Vector3.forward * back;
            return cam;
        }

        static void RefreshCamera()
        {
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null) return;

            var bounds = GetCityBounds();
            var lookAt = CityLookAt;
            if (bounds.size.sqrMagnitude > 0.01f)
                lookAt = new Vector3(bounds.center.x, CityLookAt.y, bounds.center.z);

            cam.orthographicSize = ComputeOrthoSize(bounds);
            var rotation = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            cam.transform.rotation = rotation;
            float back = Mathf.Max(bounds.extents.magnitude, 32f) * 0.92f + 18f;
            cam.transform.position = lookAt - rotation * Vector3.forward * back;
        }

        static Bounds GetCityBounds()
        {
            var city = GameObject.Find("City3D");
            if (city == null)
                return new Bounds(Vector3.zero, new Vector3(40f, 10f, 40f));

            var renderers = city.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, new Vector3(40f, 10f, 40f));

            bool hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in renderers)
            {
                if (r == null || !r.enabled) continue;
                var n = r.gameObject.name;
                if (n == "SnowGround" || n == "SnowLayer") continue;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds)
                return new Bounds(Vector3.zero, new Vector3(40f, 10f, 40f));
            return bounds;
        }

        static float ComputeOrthoSize(Bounds bounds)
        {
            const float margin = 0.92f;
            float ex = bounds.extents.x * margin;
            float ez = bounds.extents.z * margin;

            float yawRad = CameraYaw * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(yawRad));
            float sin = Mathf.Abs(Mathf.Sin(yawRad));
            float spanX = ex * cos + ez * sin;
            float spanZ = ex * sin + ez * cos;

            float w = Screen.width > 0 ? Screen.width : 1080f;
            float h = Screen.height > 0 ? Screen.height : 1920f;
            float aspect = w / h;
            float sizeForWidth = spanX / Mathf.Max(aspect, 0.01f);
            return Mathf.Max(sizeForWidth, spanZ);
        }

        static void SetupCanvasOverlay(Canvas canvas, Camera cam)
        {
            if (cam == null) return;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 100f;
        }

        static void EnsureSkyBackdrop(Transform canvas)
        {
            var existing = canvas.Find("WOS_SkyGradient");
            if (existing != null) return;

            var go = new GameObject("WOS_SkyGradient");
            go.transform.SetParent(canvas, false);
            go.transform.SetAsFirstSibling();
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(0.55f, 0.65f, 0.82f, 0.35f);

            var sprite = Resources.Load<Sprite>("Sprites/city_background");
#if UNITY_EDITOR
            if (sprite == null)
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/KILLERGAME/Sprites/city_background.png");
#endif
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = new Color(0.75f, 0.82f, 0.95f, 0.42f);
            }
        }

        static void Hide2DBackground(Canvas canvas)
        {
            var bg = canvas.transform.Find(Bg2DName);
            if (bg != null) bg.gameObject.SetActive(false);
        }

        static void Setup3DCity()
        {
            var city = GameObject.Find("City3D");
            if (city == null) return;
            city.SetActive(true);

            var matStone  = LoadMat("Assets/KILLERGAME/Materials/Mat_Stone.mat");
            var matDark   = LoadMat("Assets/KILLERGAME/Materials/Mat_Stone_Dark.mat");
            var matSnow   = LoadMat("Assets/KILLERGAME/Materials/Mat_SnowHeavy.mat");
            var matGround = LoadMat("Assets/KILLERGAME/Materials/Mat_SnowGround.mat");
            var matWin    = LoadMat("Assets/KILLERGAME/Materials/Mat_WindowGlow.mat");

            int i = 0;
            foreach (var r in city.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("ground") || n.Contains("snowground") || n.Contains("plane"))
                    r.sharedMaterial = matGround ?? r.sharedMaterial;
                else if (n.Contains("furnace") || n.Contains("tower") || n.Contains("chimney"))
                    r.sharedMaterial = matWin ?? matStone ?? r.sharedMaterial;
                else if (n.Contains("snow") || n.Contains("roof"))
                    r.sharedMaterial = matSnow ?? r.sharedMaterial;
                else
                    r.sharedMaterial = (i++ % 3 == 0 ? matDark : matStone) ?? r.sharedMaterial;
            }

            EnsureFireLight(city.transform, "FurnaceCoreGlow", new Vector3(0f, 9f, 0f), 32f, 55f);
            EnsureFireLight(city.transform, "FurnaceMouthGlow", new Vector3(0f, 4.8f, 6f), 38f, 24f);
            EnsureFireLight(city.transform, "FireGlowL", new Vector3(-8f, 2.5f, 3f), 8f, 24f);
            EnsureFireLight(city.transform, "FireGlowR", new Vector3(8f, 2.5f, 3f), 8f, 24f);
            EnsureFireLight(city.transform, "BonfireGlow", new Vector3(0f, 1.5f, 2f), 16f, 22f);

            CityModelReplacer.Replace(city, matSnow, matStone, matWin);
        }

        static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();

        static Material LoadMat(string path)
        {
            if (MatCache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            Material mat = null;
#if UNITY_EDITOR
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
#endif
            if (mat == null)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                mat = Resources.Load<Material>($"Materials/{name}");
            }

            if (mat != null)
                MatCache[path] = mat;
            return mat;
        }

        static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.08f, 0.12f, 0.20f);
            RenderSettings.fogDensity = 0.014f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.24f);

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(1f, 0.82f, 0.60f);
                    light.intensity = 1.5f;
                    light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
                }
                else if (light.type == LightType.Point && !light.name.Contains("Furnace") && !light.name.Contains("FireGlow"))
                {
                    light.color = new Color(1f, 0.52f, 0.10f);
                    light.intensity = 5f;
                    light.range = 20f;
                }
            }
        }

        static void EnsureFireLight(Transform parent, string name, Vector3 localPos, float intensity, float range)
        {
            var existing = parent.Find(name);
            if (existing != null)
                Object.Destroy(existing.gameObject);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.52f, 0.10f);
            l.intensity = intensity;
            l.range = range;
        }

        static void ApplySnow()
        {
            var snow = GameObject.Find("SnowParticles3D");
            if (snow == null) return;
            snow.SetActive(true);
            var ps = snow.GetComponent<ParticleSystem>();
            if (ps == null) return;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            var main = ps.main;
            main.startLifetime = 9f;
            main.startSpeed = 1.2f;
            main.startSize = 0.07f;
            main.maxParticles = 700;
            main.gravityModifier = 0.12f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.6f));
            var emission = ps.emission;
            emission.rateOverTime = 100f;
            var shape = ps.shape;
            shape.scale = new Vector3(55f, 1f, 55f);

            var renderer = snow.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                var mat = LoadMat("Assets/SourceFiles/VFX/SnowMaterial.mat") ?? CreateSnowParticleMat();
                if (mat != null)
                    renderer.sharedMaterial = mat;
            }

            ps.Play();
        }

        static Material CreateSnowParticleMat()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit");
            if (shader == null) return null;
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.65f));
            return mat;
        }

        static void EnsureVignettes(Transform canvas)
        {
            EnsureVignette(canvas, TopVigName, true);
            EnsureVignette(canvas, BotVigName, false);
        }

        static void EnsureVignette(Transform canvas, string name, bool top)
        {
            if (canvas.Find(name) != null) return;
            var go = new GameObject(name);
            go.transform.SetParent(canvas, false);
            go.transform.SetSiblingIndex(1);
            var rt = go.AddComponent<RectTransform>();
            if (top) { rt.anchorMin = new Vector2(0f, 0.82f); rt.anchorMax = new Vector2(1f, 1f); }
            else     { rt.anchorMin = new Vector2(0f, 0f);    rt.anchorMax = new Vector2(1f, 0.42f); }
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = top
                ? new Color(0.02f, 0.04f, 0.10f, 0.35f)
                : new Color(0.02f, 0.04f, 0.10f, 0.65f);
            img.raycastTarget = false;
        }

        static void ApplyPanels()
        {
            StylePanel("FurnacePanel", FurnaceSheet, 0f, 0.52f);
            StylePanel("BasePanel", SheetBg, 0.12f, 0.88f);
            StylePanel("TroopsPanel", SheetBg, 0.12f, 0.88f);
            StylePanel("HeroesPanel", SheetBg, 0.12f, 0.88f);
            StyleFurnaceWidgets();
        }

        static void StylePanel(string name, Color bg, float anchorMinY, float anchorMaxY)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, anchorMinY);
                rt.anchorMax = new Vector2(1f, anchorMaxY);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var img = go.GetComponent<Image>();
            if (img != null) img.color = bg;
            foreach (var childImg in go.GetComponentsInChildren<Image>(true))
            {
                if (childImg.gameObject.name.Contains("Viewport"))
                    childImg.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        static void StyleFurnaceWidgets()
        {
            var temp = GameObject.Find("TempBig");
            if (temp != null)
            {
                var t = temp.GetComponent<TextMeshProUGUI>();
                if (t != null) { t.fontSize = 48; t.fontStyle = FontStyles.Bold; t.color = new Color(1f, 0.62f, 0.12f); }
            }
            var fill = GameObject.Find("Fill");
            if (fill != null)
            {
                var img = fill.GetComponent<Image>();
                if (img != null) img.color = new Color(0.95f, 0.45f, 0.08f, 1f);
            }
        }

        static void ApplyHud()
        {
            var stats = GameObject.Find("Stats");
            if (stats == null) return;
            var rt = stats.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0.915f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var img = stats.GetComponent<Image>();
            if (img != null) img.color = HudBg;
        }

        static void ApplyTabBar()
        {
            var tabBar = GameObject.Find("TabBar");
            if (tabBar == null) return;
            var rt = tabBar.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0.115f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            var img = tabBar.GetComponent<Image>();
            if (img != null) img.color = TabBarBg;
        }

        public static void StyleCard(GameObject card, Color accent, Color buttonColor, string buttonName)
        {
            if (card == null) return;
            var bg = card.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.04f, 0.07f, 0.13f, 0.92f);
            var le = card.GetComponent<LayoutElement>() ?? card.AddComponent<LayoutElement>();
            le.preferredHeight = card.name.Contains("Troop") ? 96f : 112f;
            EnsureAccent(card.transform, accent);
            EnsureCardFrame(card.transform);
            foreach (var tmp in card.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var n = tmp.gameObject.name.ToLowerInvariant();
                if (n.Contains("name") || n == "l") tmp.color = GoldText;
                else if (n.Contains("level") || n.Contains("lvl")) tmp.color = BlueLevel;
                else if (n.Contains("cost") || n.Contains("upgcost")) tmp.color = AmberCost;
            }
            FixIconLayout(card.transform.Find("IconImg") ?? card.transform.Find("Icon"));
            var btn = card.transform.Find(buttonName);
            if (btn != null) { var btnImg = btn.GetComponent<Image>(); if (btnImg != null) btnImg.color = buttonColor; }
        }

        static void EnsureCardFrame(Transform card)
        {
            if (card.Find("WOS_CardFrame") != null) return;
            var frame = new GameObject("WOS_CardFrame");
            frame.transform.SetParent(card, false);
            frame.transform.SetAsFirstSibling();
            var rt = frame.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2, 2);
            rt.offsetMax = new Vector2(-2, -2);
            var img = frame.AddComponent<Image>();
            img.color = new Color(0.55f, 0.42f, 0.12f, 0.85f);
            img.raycastTarget = false;
        }

        static void EnsureAccent(Transform card, Color accent)
        {
            var accentT = card.Find("Accent");
            if (accentT == null)
            {
                var go = new GameObject("Accent");
                go.transform.SetParent(card, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f); rt.sizeDelta = new Vector2(4f, 0f);
                var img = go.AddComponent<Image>();
                img.color = accent; img.raycastTarget = false;
                accentT = go.transform;
            }
            accentT.SetAsFirstSibling();
        }

        static void FixIconLayout(Transform iconT)
        {
            if (iconT == null) return;
            var img = iconT.GetComponent<Image>();
            if (img != null) { img.preserveAspect = true; img.type = Image.Type.Simple; }
            var le = iconT.GetComponent<LayoutElement>() ?? iconT.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = le.minWidth = le.minHeight = 72f;
            le.flexibleWidth = le.flexibleHeight = 0f;
        }

        public static void FixIconLayoutPublic(Transform iconT) => FixIconLayout(iconT);
    }
}
