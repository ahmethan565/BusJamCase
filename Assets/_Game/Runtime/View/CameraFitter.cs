using UnityEngine;

namespace Game.Runtime.View
{
    // Fixed perspective camera that frames the level content at the start of each level.
    public sealed class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float tilt = 55f;
        [SerializeField] private float verticalFov = 40f;
        [Tooltip("Extra distance; also absorbs the perspective difference caused by the tilt")]
        [SerializeField] private float margin = 0.3f;

        public void Fit(Bounds bounds)
        {
            cam.fieldOfView = verticalFov;
            cam.transform.rotation = Quaternion.Euler(tilt, 0f, 0f);

            float vFov = cam.fieldOfView * Mathf.Deg2Rad;
            float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov / 2f) * cam.aspect);

            float distForWidth = (bounds.size.x / 2f) / Mathf.Tan(hFov / 2f);
            float distForDepth = (bounds.size.z / 2f) / Mathf.Tan(vFov / 2f);
            float distance = Mathf.Max(distForWidth, distForDepth) * (1f + margin);

            cam.transform.position = bounds.center - cam.transform.forward * distance;
        }
    }
}
