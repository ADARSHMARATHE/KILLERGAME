using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KillerGame
{
    /// <summary>
    /// Runtime-built Wild Brawl modal (WOS-style arena PvP event).
    /// </summary>
    public class WildBrawlUI : MonoBehaviour
    {
        static readonly Color PanelBg   = new Color(0.05f, 0.08f, 0.15f, 0.95f);
        static readonly Color GoldText  = new Color(1f, 0.86f, 0.32f);
        static readonly Color AmberNum  = new Color(0.96f, 0.65f, 0.14f);
        static readonly Color FightRed  = new Color(0.85f, 0.18f, 0.12f);
        static readonly Color SkipGrey  = new Color(0.25f, 0.28f, 0.35f);

        GameObject _root;
        TextMeshProUGUI _title;
        TextMeshProUGUI _subtitle;
        TextMeshProUGUI _yourPower;
        TextMeshProUGUI _enemyPower;
        TextMeshProUGUI _timer;
        TextMeshProUGUI _result;
        Button _fightBtn;
        Button _skipBtn;

        KillerGameManager _gm;

        void Awake()
        {
            TryBuildModal();
            if (_root != null) _root.SetActive(false);
        }

        void Start()
        {
            if (_root == null) TryBuildModal();
            if (_root == null) { Debug.LogWarning("WildBrawlUI: no Canvas found — modal disabled."); return; }

            _gm = KillerGameManager.Instance;
            if (_gm == null) { Debug.LogWarning("WildBrawlUI: KillerGameManager not found."); return; }

            _gm.OnWildBrawlStarted.AddListener(Show);
            _gm.OnStateChanged.AddListener(OnStateChanged);

            _fightBtn.onClick.AddListener(OnFight);
            _skipBtn.onClick.AddListener(OnSkip);
        }

        void OnDestroy()
        {
            if (_gm == null) return;
            _gm.OnWildBrawlStarted.RemoveListener(Show);
            _gm.OnStateChanged.RemoveListener(OnStateChanged);
        }

        void OnStateChanged()
        {
            if (_gm == null || !_root.activeSelf) return;
            RefreshDisplay();
        }

        void Show()
        {
            if (_root == null) return;
            _result.text = "";
            _result.gameObject.SetActive(false);
            _fightBtn.gameObject.SetActive(true);
            _skipBtn.gameObject.SetActive(true);
            RefreshDisplay();
            _root.SetActive(true);
        }

        void RefreshDisplay()
        {
            var b = _gm.State.wildBrawl;
            _subtitle.text = b.enemyName;
            _yourPower.text = $"Your Army\n<size=120%><color=#F0A820>{_gm.CalcTroopPower():N0}</color></size>";
            _enemyPower.text = $"Enemy Army\n<size=120%><color=#E84530>{b.enemyPower:N0}</color></size>";
            _timer.text = b.timeRemaining > 0f ? $"Time left: {Mathf.CeilToInt(b.timeRemaining)}s" : "";
        }

        void OnFight()
        {
            string outcome = _gm.FightWildBrawl();
            _result.text = outcome;
            _result.color = outcome.StartsWith("VICTORY")
                ? new Color(0.4f, 0.95f, 0.5f)
                : new Color(0.95f, 0.35f, 0.3f);
            _result.gameObject.SetActive(true);
            _fightBtn.gameObject.SetActive(false);
            _skipBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Close";
            _skipBtn.gameObject.SetActive(true);
            _timer.text = "";
        }

        void OnSkip()
        {
            if (_gm.State.wildBrawl.active)
                _gm.SkipWildBrawl();
            _root.SetActive(false);
            _skipBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Skip";
        }

        public void Hide() => _root?.SetActive(false);

        void TryBuildModal()
        {
            if (_root != null) return;
            BuildModal();
        }

        void BuildModal()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            _root = new GameObject("WildBrawlModal");
            _root.transform.SetParent(canvas.transform, false);

            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var dim = new GameObject("Dim");
            dim.transform.SetParent(_root.transform, false);
            Stretch(dim.AddComponent<RectTransform>());
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.55f);
            dimImg.raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_root.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.08f, 0.28f);
            panelRt.anchorMax = new Vector2(0.92f, 0.72f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = PanelBg;

            var border = new GameObject("Border");
            border.transform.SetParent(panel.transform, false);
            var borderRt = border.AddComponent<RectTransform>();
            borderRt.anchorMin = new Vector2(0f, 1f);
            borderRt.anchorMax = new Vector2(1f, 1f);
            borderRt.pivot = new Vector2(0.5f, 1f);
            borderRt.sizeDelta = new Vector2(0f, 4f);
            border.AddComponent<Image>().color = GoldText;

            _title = MakeText(panel.transform, "Title", "WILD BRAWL", 32, GoldText, FontStyles.Bold,
                new Vector2(0.5f, 0.88f), new Vector2(400f, 48f));

            _subtitle = MakeText(panel.transform, "Subtitle", "Frost Raiders", 20, AmberNum, FontStyles.Italic,
                new Vector2(0.5f, 0.76f), new Vector2(360f, 36f));

            MakeText(panel.transform, "Desc", "Send your troops into the arena!\nWin gold and glory — lose troops if defeated.",
                14, new Color(0.75f, 0.78f, 0.85f), FontStyles.Normal,
                new Vector2(0.5f, 0.62f), new Vector2(340f, 50f));

            _yourPower = MakeText(panel.transform, "YourPower", "Your Army\n0", 16, Color.white, FontStyles.Normal,
                new Vector2(0.28f, 0.46f), new Vector2(140f, 70f));
            _yourPower.alignment = TextAlignmentOptions.Center;

            MakeText(panel.transform, "VS", "VS", 28, new Color(0.9f, 0.3f, 0.2f), FontStyles.Bold,
                new Vector2(0.5f, 0.46f), new Vector2(60f, 40f)).alignment = TextAlignmentOptions.Center;

            _enemyPower = MakeText(panel.transform, "EnemyPower", "Enemy Army\n0", 16, Color.white, FontStyles.Normal,
                new Vector2(0.72f, 0.46f), new Vector2(140f, 70f));
            _enemyPower.alignment = TextAlignmentOptions.Center;

            _timer = MakeText(panel.transform, "Timer", "", 13, new Color(0.6f, 0.65f, 0.75f), FontStyles.Normal,
                new Vector2(0.5f, 0.30f), new Vector2(200f, 24f));

            _result = MakeText(panel.transform, "Result", "", 15, new Color(0.4f, 0.95f, 0.5f), FontStyles.Bold,
                new Vector2(0.5f, 0.22f), new Vector2(340f, 60f));
            _result.gameObject.SetActive(false);

            _fightBtn = MakeButton(panel.transform, "FightBtn", "FIGHT!", FightRed,
                new Vector2(0.32f, 0.10f), new Vector2(130f, 44f));
            _skipBtn = MakeButton(panel.transform, "SkipBtn", "Skip", SkipGrey,
                new Vector2(0.68f, 0.10f), new Vector2(100f, 44f));

            var closeBtn = MakeButton(panel.transform, "CloseBtn", "Close", SkipGrey,
                new Vector2(0.5f, 0.10f), new Vector2(100f, 44f));
            closeBtn.gameObject.SetActive(false);
            closeBtn.onClick.AddListener(() => _root.SetActive(false));
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color,
            FontStyles style, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        static Button MakeButton(Transform parent, string name, string label, Color bg,
            Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            go.AddComponent<Image>().color = bg;
            var btn = go.AddComponent<Button>();

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            Stretch(txtGo.AddComponent<RectTransform>());
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
