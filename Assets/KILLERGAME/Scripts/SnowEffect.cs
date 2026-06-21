using UnityEngine;
using UnityEngine.UI;

namespace KillerGame
{
    /// <summary>
    /// Spawns snow particles on a UI Canvas using RawImage quads.
    /// Attach to a full-screen RectTransform on the UI canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SnowEffect : MonoBehaviour
    {
        [SerializeField] int   count      = 80;
        [SerializeField] float minSize    = 4f;
        [SerializeField] float maxSize    = 10f;
        [SerializeField] float minSpeed   = 40f;
        [SerializeField] float maxSpeed   = 120f;

        struct Flake
        {
            public RectTransform rt;
            public float speed;
            public float drift;
            public float phase;
        }

        Flake[]       _flakes;
        RectTransform _area;

        void Start()
        {
            _area   = GetComponent<RectTransform>();
            if (_area == null) { enabled = false; return; }

            _flakes = new Flake[count];
            SpawnFlakes();
        }

        void SpawnFlakes()
        {
            // Use screen dimensions as fallback if rect not ready
            float w = _area.rect.width  > 1f ? _area.rect.width  : Screen.width;
            float h = _area.rect.height > 1f ? _area.rect.height : Screen.height;

            for (int i = 0; i < count; i++)
            {
                var go  = new GameObject("Flake", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                go.GetComponent<Image>().color = new Color(1f, 1f, 1f, Random.Range(0.3f, 0.7f));

                var rt       = (RectTransform)go.transform;
                float sz     = Random.Range(minSize, maxSize);
                rt.sizeDelta = new Vector2(sz, sz);
                rt.anchoredPosition = new Vector2(
                    Random.Range(-w / 2f, w / 2f),
                    Random.Range(-h / 2f, h / 2f));

                _flakes[i] = new Flake {
                    rt    = rt,
                    speed = Random.Range(minSpeed, maxSpeed),
                    drift = Random.Range(-30f, 30f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                };
            }
        }

        void Update()
        {
            if (_area == null || _flakes == null) return;

            float w  = _area.rect.width  > 1f ? _area.rect.width  : Screen.width;
            float h  = _area.rect.height > 1f ? _area.rect.height : Screen.height;
            float dt = Time.deltaTime;

            for (int i = 0; i < _flakes.Length; i++)
            {
                ref var f = ref _flakes[i];
                if (f.rt == null) continue;

                var pos = f.rt.anchoredPosition;
                pos.y -= f.speed * dt;
                pos.x += Mathf.Sin(Time.time * 0.5f + f.phase) * f.drift * dt;

                if (pos.y < -h / 2f) { pos.y = h / 2f; pos.x = Random.Range(-w / 2f, w / 2f); }
                if (pos.x >  w / 2f)   pos.x = -w / 2f;
                if (pos.x < -w / 2f)   pos.x =  w / 2f;

                f.rt.anchoredPosition = pos;
            }
        }
    }
}
