using UnityEngine;

namespace Smartex.UI
{
    /// <summary>
    /// Makes a world-space panel always face the active camera.
    /// Works in both desktop (orbit/fly camera) and VR (HMD) modes.
    /// Attach to any world-space Canvas or panel GameObject.
    /// </summary>
    public class BillboardPanel : MonoBehaviour
    {
        [Tooltip("Leave empty to auto-use Camera.main.")]
        public Transform targetCamera;

        [Header("Axis lock")]
        [Tooltip("Lock Y-axis rotation only (panel stays upright). " +
                 "Uncheck for full spherical billboard (follows tilt too).")]
        public bool lockYAxisOnly = true;

        void Start()
        {
            if (targetCamera == null && Camera.main != null)
                targetCamera = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (targetCamera == null) return;

            if (lockYAxisOnly)
            {
                // Rotate only around Y so the panel stays vertical —
                // better for wall-mounted / floor-standing data panels.
                Vector3 dir = targetCamera.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(-dir);
            }
            else
            {
                // Full billboard — always points directly at the camera.
                // Useful for floating tooltip-style panels.
                transform.LookAt(transform.position +
                    (transform.position - targetCamera.position));
            }
        }
    }
}
