using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KillerGame
{
    /// <summary>
    /// Applies Whiteout Survival-style atmosphere, lighting, and UI polish at runtime.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WOSVisualBootstrap : MonoBehaviour
    {
        static readonly Color PanelSheet   = new Color(0.04f, 0.07f, 0.13f, 0.58f);
        static readonly Color PanelFurnace = new Color(0.04f, 0.06f, 0.11f, 0.22f);
        static readonly Color GoldText     = new Color(1f, 0.86f, 0.32f);
        static readonly Color AmberCost    = new Color(0.96f, 0.65f, 0.14f);
        static readonly Color BlueLevel    = new Color(0.55f, 0.82f, 1f);

        void Awake()
        {
            ApplyAtmosphere();
            ApplyCamera();
            ApplyCityLighting();
            ApplySnow();
            ApplyPanels();
            ApplyHud();
            ApplyTabBar();
        }

        void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.10f, 0.14f, 0.22f);
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.17f, 0.14f);

            foreach (var light in FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(1f, 0.82f, 0.60f);
                    light.intensity = 1.6f;
                    light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
                }
                else if (light.type == LightType.Point)
                {
                    light.color = new Color(1f, 0.52f, 0.10f);
                    light.intensity = 6f;
                    light.range = 22f;
                }
            }
        }

        void ApplyCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.transform.position = new Vector3(0f, 23f, -33f);
            cam.transform.rotation = Quaternion.Euler(39f, 0f, 0f);
            cam.fieldOfView = 50f;
            cam.backgroundColor = new Color(0.07f, 0.10f, 0.17f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        void ApplyCityLighting()
        {
            var city = GameObject.Find("City3D");
            if (city == null) return;

            var core = city.transform.Find("FurnaceCoreGlow");
            if (core == null)
            {
                var go = new GameObject("FurnaceCoreGlow");
                go.transform.SetParent(city.transform, false);
                go.transform.localPosition = new Vector3(0f, 3.5f, 0f);
                core = go.transform;
            }

            var pl = core.GetComponent<Light>() ?? core.gameObject.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1f, 0.42f, 0.06f);
            pl.intensity = 9f;
            pl.range = 30f;

            // Warm bounce lights around the city base
            EnsureFireLight(city.transform, "FireGlowL", new Vector3(-10f, 1.5f, 4f), 4f);
            EnsureFireLight(city.transform, "FireGlowR", new Vector3(10f, 1.5f, 4f), 4f);
        }

        static void EnsureFireLight(Transform parent, string name, Vector3 localPos, float intensity)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPos;
                t = go.transform;
            }

            var l = t.GetComponent<Light>() ?? t.gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.48f, 0.08f);
            l.intensity = intensity;
            l.range = 16f;
        }

        void ApplySnow()
        {
            var snow = GameObject.Find("SnowParticles3D");
            if (snow == null) return;

            var ps = snow.GetComponent<ParticleSystem>();
            if (ps == null) return;

            var main = ps.main;
            main.startLifetime = 9f;
            main.startSpeed = 1.1f;
            main.startSize = 0.07f;
            main.maxParticles = 900;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.75f));

            var emission = ps.emission;
            emission.rateOverTime = 140f;

            var shape = ps.shape;
            shape.scale = new Vector3(70f, 1f, 70f);
        }

        void ApplyPanels()
        {
            StylePanel("FurnacePanel", PanelFurnace, 0.08f, 0.92f);
            StylePanel("BasePanel", PanelSheet, 0.36f, 0.90f);
            StylePanel("TroopsPanel", PanelSheet, 0.36f, 0.90f);
            StylePanel("HeroesPanel", PanelSheet, 0.36f, 0.90f);
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
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = bg;
                img.raycastTarget = true;
            }

            foreach (var childImg in go.GetComponentsInChildren<Image>(true))
            {
                if (childImg.gameObject.name.Contains("Viewport"))
                    childImg.color = new Color(0f, 0f, 0f, 0f);
            }
        }

        void ApplyHud()
        {
            var stats = GameObject.Find("Stats");
            if (stats == null) return;

            var img = stats.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.03f, 0.04f, 0.08f, 0.88f);

            foreach (var tmp in stats.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.gameObject.name == "Temp")
                    tmp.color = new Color(0.95f, 0.35f, 0.12f);
            }

            foreach (var icon in stats.GetComponentsInChildren<Image>(true))
            {
                if (icon.gameObject.name.EndsWith("Icon"))
                {
                    var le = icon.GetComponent<LayoutElement>() ?? icon.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 28f;
                    le.preferredHeight = 28f;
                }
            }
        }

        void ApplyTabBar()
        {
            var tabBar = GameObject.Find("TabBar");
            if (tabBar == null) return;

            var img = tabBar.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.02f, 0.03f, 0.06f, 0.96f);
        }

        /// <summary>Call after instantiating cards to enforce WOS card styling.</summary>
        public static void StyleCard(GameObject card, Color accent, Color buttonColor, string buttonName)
        {
            if (card == null) return;

            var bg = card.GetComponent<Image>();
            if (bg != null)
                bg.color = new Color(0.07f, 0.10f, 0.18f, 0.94f);

            var le = card.GetComponent<LayoutElement>() ?? card.AddComponent<LayoutElement>();
            if (card.name.Contains("Troop"))
                le.preferredHeight = 96f;
            else
                le.preferredHeight = 112f;

            EnsureAccent(card.transform, accent);

            foreach (var tmp in card.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var n = tmp.gameObject.name.ToLowerInvariant();
                if (n.Contains("name") || n == "l")
                    tmp.color = GoldText;
                else if (n.Contains("level") || n.Contains("lvl"))
                    tmp.color = BlueLevel;
                else if (n.Contains("cost") || n.Contains("desc") == false && n.Contains("status") == false)
                {
                    if (n.Contains("cost") || tmp.text.Contains("Food") || tmp.text.Contains("Wood") || tmp.text.Contains("Coal") || tmp.text.Contains("Iron") || tmp.text.Contains("Gold"))
                        tmp.color = AmberCost;
                }
            }

            FixIconLayout(card.transform.Find("IconImg") ?? card.transform.Find("Icon"));

            var btn = card.transform.Find(buttonName);
            if (btn != null)
            {
                var btnImg = btn.GetComponent<Image>();
                if (btnImg != null) btnImg.color = buttonColor;
            }
        }

        static void EnsureAccent(Transform card, Color accent)
        {
            var accentT = card.Find("Accent");
            if (accentT == null)
            {
                var go = new GameObject("Accent");
                go.transform.SetParent(card, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(4f, 0f);
                rt.anchoredPosition = Vector2.zero;
                var img = go.AddComponent<Image>();
                img.color = accent;
                img.raycastTarget = false;
                accentT = go.transform;
            }
            else
            {
                var img = accentT.GetComponent<Image>();
                if (img != null) img.color = accent;
            }

            accentT.SetAsFirstSibling();
        }

        static void FixIconLayout(Transform iconT)
        {
            if (iconT == null) return;

            var img = iconT.GetComponent<Image>();
            if (img != null)
            {
                img.preserveAspect = true;
                img.type = Image.Type.Simple;
            }

            var le = iconT.GetComponent<LayoutElement>() ?? iconT.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 72f;
            le.preferredHeight = 72f;
            le.minWidth = 72f;
            le.minHeight = 72f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;
        }

        public static void FixIconLayoutPublic(Transform iconT) => FixIconLayout(iconT);
    }
}
