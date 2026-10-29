using UnityEngine;

namespace Game.Runtime.UI
{
    // Fits a screen's root panel into Screen.safeArea, so notches and rounded corners never cut buttons (§12.6).
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        // Portrait is locked, so the safe area does not change after start (no Update, §16.3).
        private void Awake()
        {
            Rect area = Screen.safeArea;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
