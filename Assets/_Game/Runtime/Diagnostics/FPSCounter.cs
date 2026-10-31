using System.Globalization;
using UnityEngine;

namespace Game.Runtime.Diagnostics
{
    // Small FPS readout at the top right of the safe area, for testing on a device.
    // Drawn with OnGUI, so it needs no Canvas. Put it on its own root GameObject (DontDestroyOnLoad keeps it).
    // Its Update is a deliberate exception to "no polling in Update": measuring frames is its job.
    public sealed class FPSCounter : MonoBehaviour
    {
        private static readonly Color Good = new Color(0.2f, 0.9f, 0.3f);
        private static readonly Color Okay = new Color(1f, 0.85f, 0.1f);
        private static readonly Color Bad = new Color(1f, 0.25f, 0.25f);
        private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.75f);

        [Tooltip("Seconds the FPS is averaged over")]
        [SerializeField, Min(0.05f)] private float updateInterval = 0.5f;
        [Tooltip("Set Application.targetFrameRate in Start (Android locks to 30 FPS otherwise)")]
        [SerializeField] private bool setTargetFrameRate = true;
        [SerializeField] private int targetFrameRate = 60;
        [Tooltip("Destroy itself in release (non-development) builds")]
        [SerializeField] private bool developmentBuildOnly = true;
        [Tooltip("Text height as a share of the screen height")]
        [SerializeField] private float fontScreenShare = 0.025f;

        private int frames;
        private float elapsed;
        private string text = string.Empty;   // rebuilt only once per interval
        private Color color = Good;

        private GUIStyle style;               // rebuilt only when the screen or safe area changes
        private Rect area;
        private Rect shadowArea;
        private int screenWidth;
        private int screenHeight;
        private Rect safeArea;

        // Invariant culture: a Turkish phone would print "17,2 ms".
        public static string Label(int frameCount, float seconds) =>
            string.Format(CultureInfo.InvariantCulture, "{0} FPS ({1:0.0} ms)",
                Mathf.RoundToInt(frameCount / seconds), seconds * 1000f / frameCount);

        public static Color ColorFor(float fps) => fps >= 50f ? Good : fps >= 30f ? Okay : Bad;

        private void Awake()
        {
            if (developmentBuildOnly && !Debug.isDebugBuild)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }

        // In Start, after every Awake: GameBootstrap sets 60 in its Awake, so this value wins.
        private void Start()
        {
            if (setTargetFrameRate) Application.targetFrameRate = targetFrameRate;
        }

        private void Update()
        {
            frames++;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < updateInterval) return;

            text = Label(frames, elapsed);
            color = ColorFor(frames / elapsed);
            frames = 0;
            elapsed = 0f;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (style == null || Screen.width != screenWidth || Screen.height != screenHeight || Screen.safeArea != safeArea)
                Layout();

            style.normal.textColor = Shadow;
            GUI.Label(shadowArea, text, style);
            style.normal.textColor = color;
            GUI.Label(area, text, style);
        }

        // Top right of the safe area, so it stays clear of notches and camera holes. GUI y runs top-down.
        private void Layout()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            safeArea = Screen.safeArea;

            int fontSize = Mathf.Max(10, Mathf.RoundToInt(screenHeight * fontScreenShare));
            style ??= new GUIStyle { alignment = TextAnchor.UpperRight, fontStyle = FontStyle.Bold };
            style.fontSize = fontSize;

            float margin = fontSize * 0.5f;
            float top = screenHeight - safeArea.yMax + margin;
            area = new Rect(safeArea.xMin, top, safeArea.width - margin, fontSize * 1.5f);
            float offset = Mathf.Max(1f, fontSize * 0.06f);
            shadowArea = new Rect(area.x + offset, area.y + offset, area.width, area.height);
        }
    }
}
