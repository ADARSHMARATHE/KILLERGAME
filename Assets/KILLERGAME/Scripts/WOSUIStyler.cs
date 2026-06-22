using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KillerGame
{
    /// <summary>
    /// Applies Whiteout Survival-style UI: frosted sheets, resource icon slots, procedural tab icons.
    /// </summary>
    public static class WOSUIStyler
    {
        static readonly Color Gold       = new Color(0.92f, 0.74f, 0.22f);
        static readonly Color GoldDim    = new Color(0.55f, 0.42f, 0.12f);
        static readonly Color PanelBg    = new Color(0.03f, 0.05f, 0.10f, 0.78f);
        static readonly Color FrostBg    = new Color(0.04f, 0.07f, 0.13f, 0.42f);
        static readonly Color HudBg      = new Color(0.02f, 0.04f, 0.09f, 0.88f);
        static readonly Color TabBarBg   = new Color(0.02f, 0.03f, 0.07f, 0.94f);
        static readonly Color SlotBg     = new Color(0.06f, 0.09f, 0.16f, 0.92f);
        static readonly Color BtnOrange  = new Color(0.88f, 0.48f, 0.06f);
        static readonly Color BtnOrangeD = new Color(0.62f, 0.32f, 0.04f);
        static readonly Color AmberCost  = new Color(0.96f, 0.65f, 0.14f);

        static readonly HashSet<string> FurnaceDockVisible = new HashSet<string>
        {
            "FuelBarBG", "FuelBtns", "F10", "F25", "F50", "UpgradeBtn", "Fill"
        };

        public static void Apply(Canvas canvas)
        {
            if (canvas == null) return;
            StyleHud();
            StyleTabBar();
            StylePanels();
            LayoutFurnaceContent();
            EnsureCenterTempGauge(canvas.transform);
            StyleBuildScroll();
            StyleAllButtons(canvas.transform);

            var gauge = canvas.transform.Find("WOS_TempGauge");
            if (gauge != null) gauge.SetAsLastSibling();
            var storm = canvas.transform.Find("WOS_StormBanner");
            if (storm != null) storm.SetAsLastSibling();
        }

        static void StyleHud()
        {
            var hud = GameObject.Find("HUD");
            if (hud == null) return;

            var rt = hud.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0.905f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }

            var bg = hud.GetComponent<Image>() ?? hud.AddComponent<Image>();
            bg.color = HudBg;
            bg.raycastTarget = false;
            EnsureBorder(hud.transform, "WOS_HudBorderBot", false, Gold, 2f);

            EnsureAvatar(hud.transform);
            StyleDayBadge();
            var tempGo = GameObject.Find("Temp");
            if (tempGo != null) tempGo.SetActive(false);

            var res = GameObject.Find("Res");
            if (res != null)
            {
                var rrt = res.GetComponent<RectTransform>();
                if (rrt != null)
                {
                    rrt.anchorMin = new Vector2(0.12f, 0f);
                    rrt.anchorMax = new Vector2(0.88f, 1f);
                    rrt.offsetMin = rrt.offsetMax = Vector2.zero;
                }
                var hlg = res.GetComponent<HorizontalLayoutGroup>() ?? res.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = hlg.childControlHeight = true;
                hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
                hlg.padding = new RectOffset(0, 0, 4, 4);

                StyleResourceSlot("foodIcon", "Food", Gold);
                StyleResourceSlot("woodIcon", "Wood", Gold);
                StyleResourceSlot("coalIcon", "Coal", Gold);
                StyleResourceSlot("ironIcon", "Iron", Gold);
                StyleResourceSlot("goldIcon", "Gold", Gold);
            }

            var notif = GameObject.Find("NotifBar");
            if (notif != null) notif.SetActive(false);
        }

        static void EnsureAvatar(Transform hud)
        {
            if (hud.Find("WOS_Avatar") != null) return;
            var go = new GameObject("WOS_Avatar");
            go.transform.SetParent(hud, false);
            go.transform.SetAsFirstSibling();
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(10f, 0f);
            rt.sizeDelta = new Vector2(72f, 72f);

            CreateRingImage(go.transform, "Ring", 72f, new Color(0.35f, 0.62f, 0.95f, 0.95f));
            CreateRingImage(go.transform, "Inner", 62f, SlotBg);
            var face = new GameObject("Face");
            face.transform.SetParent(go.transform, false);
            var frt = face.AddComponent<RectTransform>();
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(48f, 48f);
            var ftmp = face.AddComponent<TextMeshProUGUI>();
            ftmp.text = "G";
            ftmp.fontSize = 32;
            ftmp.fontStyle = FontStyles.Bold;
            ftmp.color = new Color(0.75f, 0.85f, 1f);
            ftmp.alignment = TextAlignmentOptions.Center;
            ftmp.raycastTarget = false;
        }

        static void StyleDayBadge()
        {
            var day = GameObject.Find("Day");
            if (day == null) return;
            StyleTmpObject(day, Gold, 22, FontStyles.Bold);
            var drt = day.GetComponent<RectTransform>();
            if (drt != null)
            {
                drt.anchorMin = new Vector2(0f, 0.5f);
                drt.anchorMax = new Vector2(0f, 0.5f);
                drt.pivot = new Vector2(0f, 0.5f);
                drt.anchoredPosition = new Vector2(88f, 0f);
                drt.sizeDelta = new Vector2(120f, 36f);
            }
        }

        /// <summary>WOS-style large blue temperature ring centered at top of screen.</summary>
        static void EnsureCenterTempGauge(Transform canvas)
        {
            var gaugeT = canvas.Find("WOS_TempGauge");
            GameObject gauge;
            if (gaugeT == null)
            {
                gauge = new GameObject("WOS_TempGauge");
                gauge.transform.SetParent(canvas, false);
                var rt = gauge.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -8f);
                rt.sizeDelta = new Vector2(120f, 120f);

                CreateRingImage(gauge.transform, "Outer", 118f, new Color(0.22f, 0.48f, 0.82f, 0.95f));
                CreateRingImage(gauge.transform, "Mid", 106f, new Color(0.08f, 0.14f, 0.26f, 0.98f));
                CreateRingImage(gauge.transform, "Inner", 94f, new Color(0.12f, 0.28f, 0.52f, 0.92f));

                var flake = new GameObject("Snowflake");
                flake.transform.SetParent(gauge.transform, false);
                var frt = flake.AddComponent<RectTransform>();
                frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.62f);
                frt.sizeDelta = new Vector2(28f, 28f);
                var ftmp = flake.AddComponent<TextMeshProUGUI>();
                ftmp.text = "*";
                ftmp.fontSize = 26;
                ftmp.color = new Color(0.75f, 0.92f, 1f);
                ftmp.alignment = TextAlignmentOptions.Center;
                ftmp.raycastTarget = false;
            }
            else
            {
                gauge = gaugeT.gameObject;
            }

            var tempBig = GameObject.Find("TempBig");
            if (tempBig != null)
            {
                tempBig.SetActive(true);
                tempBig.transform.SetParent(gauge.transform, false);
                var trt = tempBig.GetComponent<RectTransform>();
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.38f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.sizeDelta = new Vector2(110f, 44f);
                trt.anchoredPosition = Vector2.zero;
                StyleTmpObject(tempBig, Color.white, 26, FontStyles.Bold);
            }
            else
            {
                var tempSrc = GameObject.Find("Temp");
                if (tempSrc != null)
                {
                    tempSrc.SetActive(true);
                    tempSrc.transform.SetParent(gauge.transform, false);
                    var trt = tempSrc.GetComponent<RectTransform>();
                    trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.38f);
                    trt.pivot = new Vector2(0.5f, 0.5f);
                    trt.sizeDelta = new Vector2(110f, 44f);
                    trt.anchoredPosition = Vector2.zero;
                    StyleTmpObject(tempSrc, Color.white, 26, FontStyles.Bold);
                }
            }

            var stormT = canvas.Find("WOS_StormBanner");
            if (stormT == null)
            {
                var storm = new GameObject("WOS_StormBanner");
                storm.transform.SetParent(canvas, false);
                var srt = storm.AddComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
                srt.pivot = new Vector2(0.5f, 1f);
                srt.anchoredPosition = new Vector2(0f, -132f);
                srt.sizeDelta = new Vector2(340f, 36f);
                var sbg = storm.AddComponent<Image>();
                sbg.color = new Color(0.05f, 0.08f, 0.14f, 0.72f);
                sbg.raycastTarget = false;

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(storm.transform, false);
                var lrt = labelGo.AddComponent<RectTransform>();
                Stretch(lrt);
                var stmp = labelGo.AddComponent<TextMeshProUGUI>();
                stmp.text = "A storm is coming...";
                stmp.fontSize = 16;
                stmp.fontStyle = FontStyles.Italic;
                stmp.color = new Color(0.65f, 0.78f, 0.95f);
                stmp.alignment = TextAlignmentOptions.Center;
                stmp.raycastTarget = false;
            }
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void StyleResourceSlot(string iconName, string textName, Color rim)
        {
            var iconGo = GameObject.Find(iconName);
            var textGo = GameObject.Find(textName);
            if (iconGo == null || textGo == null) return;

            var res = GameObject.Find("Res");
            if (res == null) return;

            string slotName = "Slot_" + textName;
            var existing = res.transform.Find(slotName);
            Transform slot;
            if (existing != null)
                slot = existing;
            else
            {
                var slotGo = new GameObject(slotName);
                slotGo.transform.SetParent(res.transform, false);
                slot = slotGo.transform;
                var vlg = slotGo.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 0;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = vlg.childControlHeight = true;
                vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;
                var le = slotGo.AddComponent<LayoutElement>();
                le.preferredWidth = 96;
                le.preferredHeight = 72;
            }

            iconGo.transform.SetParent(slot, false);
            textGo.transform.SetParent(slot, false);
            iconGo.transform.SetSiblingIndex(0);

            var iconRt = iconGo.GetComponent<RectTransform>();
            var iconLe = iconGo.GetComponent<LayoutElement>() ?? iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = iconLe.preferredHeight = 40;

            var iconImg = iconGo.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }

            EnsureSlotCircle(iconGo.transform, rim);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize = 20;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
            }
            var textLe = textGo.GetComponent<LayoutElement>() ?? textGo.AddComponent<LayoutElement>();
            textLe.preferredHeight = 24;
        }

        static void EnsureSlotCircle(Transform icon, Color rim)
        {
            if (icon.Find("WOS_SlotBg") != null) return;
            var bg = new GameObject("WOS_SlotBg");
            bg.transform.SetParent(icon.parent, false);
            bg.transform.SetSiblingIndex(icon.GetSiblingIndex());
            var rt = bg.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(46f, 46f);
            var outer = bg.AddComponent<Image>();
            outer.color = rim;
            outer.raycastTarget = false;
            var inner = new GameObject("Inner");
            inner.transform.SetParent(bg.transform, false);
            var irt = inner.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(2f, 2f);
            irt.offsetMax = new Vector2(-2f, -2f);
            var iimg = inner.AddComponent<Image>();
            iimg.color = SlotBg;
            iimg.raycastTarget = false;
        }

        static void StyleTabBar()
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

            EnsureBorder(tabBar.transform, "WOS_TabBorderTop", true, Gold, 2f);

            StyleTab("FurnaceTab", TabIconKind.Furnace, "Furnace", new Color(0.90f, 0.50f, 0.08f));
            StyleTab("BaseTab", TabIconKind.Build, "Build", new Color(0.22f, 0.55f, 0.90f));
            StyleTab("TroopsTab", TabIconKind.Train, "Train", new Color(0.30f, 0.75f, 0.35f));
            StyleTab("HeroesTab", TabIconKind.Heroes, "Heroes", new Color(0.65f, 0.35f, 0.90f));
        }

        enum TabIconKind { Furnace, Build, Train, Heroes }

        static void StyleTab(string tabName, TabIconKind kind, string label, Color accent)
        {
            var tab = GameObject.Find(tabName);
            if (tab == null) return;

            var iconT = tab.transform.Find("TabIcon");
            if (iconT != null)
            {
                var tmp = iconT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) UnityEngine.Object.Destroy(tmp);
            }
            else
            {
                var go = new GameObject("TabIcon");
                go.transform.SetParent(tab.transform, false);
                iconT = go.transform;
            }

            var irt = iconT.GetComponent<RectTransform>() ?? iconT.gameObject.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.58f);
            irt.anchorMax = new Vector2(0.5f, 0.58f);
            irt.sizeDelta = new Vector2(52f, 52f);
            irt.anchoredPosition = Vector2.zero;

            foreach (Transform c in iconT)
                UnityEngine.Object.Destroy(c.gameObject);

            var sprite = LoadTabSprite(kind);
            if (sprite != null)
            {
                var imgGo = new GameObject("Sprite");
                imgGo.transform.SetParent(iconT, false);
                var srt = imgGo.AddComponent<RectTransform>();
                srt.anchorMin = Vector2.zero;
                srt.anchorMax = Vector2.one;
                srt.offsetMin = srt.offsetMax = Vector2.zero;
                var img = imgGo.AddComponent<Image>();
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            else
                BuildProceduralTabIcon(iconT, kind, accent);

            var labelT = tab.transform.Find("TabLabel");
            if (labelT == null)
            {
                var go = new GameObject("TabLabel");
                go.transform.SetParent(tab.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0.42f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.text = label;
                tmp.fontSize = 20;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.85f, 0.85f, 0.90f);
                tmp.raycastTarget = false;
                labelT = go.transform;
            }

            var tabImg = tab.GetComponent<Image>();
            if (tabImg != null) tabImg.color = new Color(0.04f, 0.06f, 0.11f, 0.6f);
        }

        static Sprite LoadTabSprite(TabIconKind kind)
        {
            string spriteName = kind switch
            {
                TabIconKind.Furnace => "furnace",
                TabIconKind.Build => "sawmill",
                TabIconKind.Train => "barracks",
                TabIconKind.Heroes => "market",
                _ => null
            };
            if (spriteName == null) return null;

            foreach (var s in Resources.LoadAll<Sprite>("Sprites/building_icons"))
                if (s.name == spriteName) return s;

#if UNITY_EDITOR
            var path = $"Assets/KILLERGAME/Sprites/building_icons/{spriteName}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }

        static void BuildProceduralTabIcon(Transform parent, TabIconKind kind, Color accent)
        {
            switch (kind)
            {
                case TabIconKind.Furnace:
                    CreateRingImage(parent, "Bg", 48f, accent);
                    CreateFlameBar(parent, "Flame", 0f, 28f, new Color(1f, 0.7f, 0.15f, 0.95f));
                    break;
                case TabIconKind.Build:
                    CreateRectIcon(parent, "Head", new Vector2(28f, 14f), new Vector2(0f, 6f), accent);
                    CreateRectIcon(parent, "Handle", new Vector2(8f, 22f), new Vector2(0f, -8f), accent * 0.85f);
                    break;
                case TabIconKind.Train:
                    CreateRectIcon(parent, "Blade", new Vector2(8f, 30f), new Vector2(0f, 4f), accent);
                    CreateRectIcon(parent, "Guard", new Vector2(22f, 6f), new Vector2(0f, -4f), accent * 0.9f);
                    break;
                case TabIconKind.Heroes:
                    CreateRingImage(parent, "Star", 40f, accent);
                    CreateRectIcon(parent, "Core", new Vector2(14f, 14f), Vector2.zero, new Color(0.12f, 0.08f, 0.18f, 0.9f));
                    break;
            }
        }

        static void CreateRectIcon(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        static void StylePanels()
        {
            StyleWOSCityFurnaceDock();
            StyleSheet("BasePanel", 0.12f, 0.88f, false);
            StyleSheet("TroopsPanel", 0.12f, 0.88f, false);
            StyleSheet("HeroesPanel", 0.12f, 0.88f, false);
        }

        /// <summary>WOS home screen: full city visible, thin fuel dock above tab bar.</summary>
        static void StyleWOSCityFurnaceDock()
        {
            var panel = GameObject.Find("FurnacePanel");
            if (panel == null) return;

            var scroll = panel.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.enabled = false;
                scroll.vertical = false;
                scroll.horizontal = false;
            }

            var rt = panel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0.115f);
                rt.anchorMax = new Vector2(1f, 0.205f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }

            var img = panel.GetComponent<Image>();
            if (img != null) img.color = new Color(0.03f, 0.05f, 0.10f, 0.55f);

            RemoveChild(panel.transform, "WOS_Frame");
            EnsureBorder(panel.transform, "WOS_FrostTop", true, new Color(Gold.r, Gold.g, Gold.b, 0.45f), 2f);

            var viewport = panel.transform.Find("Viewport");
            if (viewport != null)
            {
                var vrt = viewport.GetComponent<RectTransform>();
                if (vrt != null)
                {
                    vrt.anchorMin = Vector2.zero;
                    vrt.anchorMax = Vector2.one;
                    vrt.offsetMin = vrt.offsetMax = Vector2.zero;
                }
            }

            var fcontent = GameObject.Find("FContent");
            if (fcontent == null) return;

            var fcrt = fcontent.GetComponent<RectTransform>();
            if (fcrt != null)
            {
                fcrt.anchorMin = Vector2.zero;
                fcrt.anchorMax = Vector2.one;
                fcrt.offsetMin = fcrt.offsetMax = Vector2.zero;
            }

            var csf = fcontent.GetComponent<ContentSizeFitter>();
            if (csf != null) csf.enabled = false;

            foreach (var vlg in fcontent.GetComponents<VerticalLayoutGroup>())
                UnityEngine.Object.Destroy(vlg);

            var hlg = fcontent.GetComponent<HorizontalLayoutGroup>() ?? fcontent.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            foreach (Transform child in fcontent.transform)
            {
                bool keep = FurnaceDockVisible.Contains(child.name);
                child.gameObject.SetActive(keep);
            }

            var stats = GameObject.Find("Stats");
            if (stats != null) stats.SetActive(false);

            foreach (var n in new[] { "SubLbl", "TempBig", "Emoji", "FuelPct", "FuelLabel", "Burn Rate", "Max Temp", "Furnace Lvl", "UpgCost" })
            {
                var o = GameObject.Find(n);
                if (o != null) o.SetActive(false);
            }

            SetRowHeight("FuelBarBG", 36);
            var fuelBar = GameObject.Find("FuelBarBG");
            if (fuelBar != null)
            {
                var le = fuelBar.GetComponent<LayoutElement>() ?? fuelBar.AddComponent<LayoutElement>();
                le.preferredWidth = 220;
                le.flexibleWidth = 1;
            }
            StyleFuelBar();

            var fuelBtns = GameObject.Find("FuelBtns");
            if (fuelBtns != null)
            {
                var fle = fuelBtns.GetComponent<LayoutElement>() ?? fuelBtns.AddComponent<LayoutElement>();
                fle.preferredWidth = 280;
                var fbHlg = fuelBtns.GetComponent<HorizontalLayoutGroup>() ?? fuelBtns.AddComponent<HorizontalLayoutGroup>();
                fbHlg.spacing = 4;
                fbHlg.childAlignment = TextAnchor.MiddleCenter;
            }

            StyleFuelButton("F10", "+10%");
            StyleFuelButton("F25", "+25%");
            StyleFuelButton("F50", "+50%");

            var upgrade = GameObject.Find("UpgradeBtn");
            if (upgrade != null)
            {
                var ule = upgrade.GetComponent<LayoutElement>() ?? upgrade.AddComponent<LayoutElement>();
                ule.preferredWidth = 140;
                StyleUpgradeButton("UpgradeBtn");
                var ulbl = upgrade.GetComponentInChildren<TextMeshProUGUI>();
                if (ulbl != null) ulbl.text = "Upgrade";
            }
        }

        static void StyleSheet(string name, float minY, float maxY, bool compact)
        {
            var go = GameObject.Find(name);
            if (go == null) return;

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, minY);
                rt.anchorMax = new Vector2(1f, maxY);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }

            var img = go.GetComponent<Image>();
            if (img != null) img.color = compact ? FrostBg : PanelBg;

            if (compact)
            {
                RemoveChild(go.transform, "WOS_Frame");
                EnsureBorder(go.transform, "WOS_FrostTop", true, new Color(Gold.r, Gold.g, Gold.b, 0.55f), 2f);
            }
            else
            {
                EnsureFrame(go.transform, 8f);
            }
        }

        static void RemoveChild(Transform parent, string name)
        {
            var c = parent.Find(name);
            if (c != null) UnityEngine.Object.Destroy(c.gameObject);
        }

        static void LayoutFurnaceContent()
        {
            // Furnace controls laid out in StyleWOSCityFurnaceDock (horizontal mini dock).
        }

        static void EnsureFurnaceTitle(Transform parent)
        {
            var existing = parent.Find("WOS_FurnaceTitle");
            if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

            var go = new GameObject("WOS_FurnaceTitle");
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 28;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "FURNACE";
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Gold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        static void StyleFuelButton(string name, string label)
        {
            var btn = GameObject.Find(name);
            if (btn == null) return;
            var btnGo = btn.GetComponent<Button>() != null ? btn : btn.transform.parent?.gameObject;
            if (btnGo == null) return;
            StyleButton(btnGo, BtnOrange, BtnOrangeD);
            var lbl = btnGo.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
            {
                lbl.text = label;
                lbl.fontSize = 22;
                lbl.fontStyle = FontStyles.Bold;
                lbl.color = Color.white;
            }
        }

        static void EnsureFurnaceRing(Transform parent)
        {
            var existing = parent.Find("WOS_FurnaceRing");
            if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

            var ring = new GameObject("WOS_FurnaceRing");
            ring.transform.SetParent(parent, false);
            ring.transform.SetAsFirstSibling();
            ring.AddComponent<RectTransform>();
            var le = ring.AddComponent<LayoutElement>();
            le.preferredHeight = 96;
            le.preferredWidth = 96;

            CreateRingImage(ring.transform, "Outer", 96, new Color(0.92f, 0.74f, 0.22f, 0.95f));
            CreateRingImage(ring.transform, "Mid", 84, new Color(0.55f, 0.38f, 0.08f, 0.9f));
            CreateRingImage(ring.transform, "Inner", 72, new Color(0.04f, 0.06f, 0.11f, 0.98f));
            CreateRingImage(ring.transform, "GlowHot", 58, new Color(1f, 0.55f, 0.08f, 0.45f));
            CreateRingImage(ring.transform, "GlowCore", 42, new Color(1f, 0.72f, 0.15f, 0.65f));

            CreateFlameBar(ring.transform, "FlameL", -12f, 32f, new Color(1f, 0.45f, 0.05f, 0.85f));
            CreateFlameBar(ring.transform, "FlameC", 0f, 38f, new Color(1f, 0.62f, 0.10f, 0.95f));
            CreateFlameBar(ring.transform, "FlameR", 12f, 32f, new Color(1f, 0.45f, 0.05f, 0.85f));

            var furnaceSprite = LoadTabSprite(TabIconKind.Furnace);
            var iconGo = new GameObject("FurnaceIconFlame");
            iconGo.transform.SetParent(ring.transform, false);
            var irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(0f, 4f);
            var img = iconGo.AddComponent<Image>();
            img.raycastTarget = false;
            if (furnaceSprite != null)
            {
                irt.sizeDelta = new Vector2(52f, 52f);
                img.sprite = furnaceSprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                irt.sizeDelta = new Vector2(24f, 32f);
                img.color = new Color(1f, 0.55f, 0.08f, 0.95f);
            }

            var tempBig = GameObject.Find("TempBig");
            if (tempBig != null)
            {
                tempBig.SetActive(true);
                tempBig.transform.SetParent(ring.transform, false);
                var trt = tempBig.GetComponent<RectTransform>();
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f);
                trt.pivot = new Vector2(0.5f, 1f);
                trt.sizeDelta = new Vector2(120, 30);
                trt.anchoredPosition = new Vector2(0f, -4f);
                StyleTmpObject(tempBig, new Color(1f, 0.72f, 0.18f), 28, FontStyles.Bold);
            }

            var emoji = GameObject.Find("Emoji");
            if (emoji != null) emoji.SetActive(false);
        }

        static void CreateFlameBar(Transform parent, string name, float x, float h, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(10f, h);
            rt.anchoredPosition = new Vector2(x, -8f);
            rt.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? 8f : x > 0f ? -8f : 0f);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        static void StyleBuildScroll()
        {
            var basePanel = GameObject.Find("BasePanel");
            if (basePanel == null) return;

            var scroll = basePanel.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.vertical = true;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Elastic;
                scroll.scrollSensitivity = 24f;
            }

            var viewport = basePanel.transform.Find("BViewport");
            if (viewport != null)
            {
                var mask = viewport.GetComponent<Mask>() ?? viewport.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                var vpImg = viewport.GetComponent<Image>();
                if (vpImg != null) vpImg.color = new Color(0f, 0f, 0f, 0f);
            }

            var content = GameObject.Find("BContent");
            if (content != null)
            {
                var vlg = content.GetComponent<VerticalLayoutGroup>() ?? content.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(14, 14, 10, 14);
                vlg.spacing = 10;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                var csf = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        static GameObject CreateRingImage(Transform parent, string name, float size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        static void StyleFuelBar()
        {
            var bg = GameObject.Find("FuelBarBG");
            if (bg == null) return;
            var bgImg = bg.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(0.08f, 0.10f, 0.16f, 0.9f);

            var fill = GameObject.Find("Fill");
            if (fill != null)
            {
                var img = fill.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.95f, 0.45f, 0.08f, 1f);
                    img.type = Image.Type.Filled;
                    img.fillMethod = Image.FillMethod.Horizontal;
                }
            }
        }

        static void StyleUpgradeButton(string name)
        {
            var btn = GameObject.Find(name);
            if (btn == null) return;
            StyleButton(btn, new Color(0.82f, 0.52f, 0.08f), new Color(0.55f, 0.32f, 0.04f));
            EnsureBorder(btn.transform, "WOS_BtnBorder", true, GoldDim, 2f);
            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) { lbl.fontSize = 26; lbl.fontStyle = FontStyles.Bold; lbl.color = Color.white; }
        }

        static void StyleAllButtons(Transform root)
        {
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                if (btn.name == "UpgradeBtn" || btn.name.StartsWith("F")) continue;
                StyleButton(btn.gameObject, new Color(0.18f, 0.28f, 0.45f), new Color(0.10f, 0.16f, 0.28f));
            }
        }

        static void StyleButton(GameObject go, Color face, Color shadow)
        {
            var img = go.GetComponent<Image>();
            if (img != null) img.color = face;
            var colors = go.GetComponent<Button>()?.colors;
            if (go.GetComponent<Button>() != null)
            {
                var cb = go.GetComponent<Button>().colors;
                cb.normalColor = face;
                cb.highlightedColor = face * 1.1f;
                cb.pressedColor = shadow;
                cb.selectedColor = face;
                go.GetComponent<Button>().colors = cb;
            }
        }

        static void EnsureFrame(Transform parent, float inset)
        {
            if (parent.Find("WOS_Frame") != null) return;
            var frame = new GameObject("WOS_Frame");
            frame.transform.SetParent(parent, false);
            frame.transform.SetAsFirstSibling();
            var rt = frame.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            var outer = frame.AddComponent<Image>();
            outer.color = GoldDim;
            outer.raycastTarget = false;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(frame.transform, false);
            var irt = inner.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(3, 3);
            irt.offsetMax = new Vector2(-3, -3);
            var iimg = inner.AddComponent<Image>();
            iimg.color = new Color(0.05f, 0.08f, 0.14f, 0.88f);
            iimg.raycastTarget = false;
        }

        static void EnsureBorder(Transform parent, string name, bool top, Color color, float thickness)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var img = existing.GetComponent<Image>();
                if (img != null) img.color = color;
                return;
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (top)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0, thickness);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(0, thickness);
            }
            var img2 = go.AddComponent<Image>();
            img2.color = color;
            img2.raycastTarget = false;
        }

        static void SetRowHeight(string name, float h)
        {
            var go = GameObject.Find(name);
            if (go == null || h <= 0) return;
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = h;
        }

        static void StyleTmp(string name, Color c, float size, FontStyles style)
        {
            var go = GameObject.Find(name);
            StyleTmpObject(go, c, size, style);
        }

        static void StyleTmpByParent(string name, Color c, float size)
        {
            StyleTmpObject(GameObject.Find(name), c, size);
        }

        static void StyleTmpObject(GameObject go, Color c, float size, FontStyles style = FontStyles.Normal)
        {
            if (go == null) return;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (t == null) return;
            t.color = c;
            t.fontSize = size;
            t.fontStyle = style;
        }
    }
}
