using UnityEngine;

namespace StrategyGame
{
    public class FloatingTextPopup : MonoBehaviour
    {
        [SerializeField] float lifetimeSeconds = 1.75f;
        [SerializeField] float riseSpeed = 1.2f;

        float remainingLifetime;
        TextMesh textMesh;

        public static void Show(Vector3 worldPosition, string message, Color color)
        {
            var popupObject = new GameObject("FloatingTextPopup");
            popupObject.transform.position = worldPosition + Vector3.up * 1.1f;
            popupObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var text = popupObject.AddComponent<TextMesh>();
            text.text = message;
            text.fontSize = 42;
            text.characterSize = 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;

            var popup = popupObject.AddComponent<FloatingTextPopup>();
            popup.textMesh = text;
            popup.remainingLifetime = popup.lifetimeSeconds;
        }

        void Update()
        {
            transform.position += Vector3.up * riseSpeed * Time.deltaTime;
            remainingLifetime -= Time.deltaTime;

            if (textMesh != null)
            {
                var alpha = Mathf.Clamp01(remainingLifetime / lifetimeSeconds);
                var color = textMesh.color;
                color.a = alpha;
                textMesh.color = color;
            }

            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
