// MODULE C — Real-Time AR Data Overlay  (Member 3)
// Makes the AR panel always face the user's AR camera.

using UnityEngine;

namespace Smartex.AR.Overlay
{
    /// <summary>
    /// Rotates this GameObject every frame so it faces the main camera.
    /// Attach to the root of any world-space AR panel prefab.
    /// </summary>
    public class BillboardFacer : MonoBehaviour
    {
        [Tooltip("Lock Y-axis only so panel doesn't tilt up/down as user crouches.")]
        public bool lockYAxis = true;

        void LateUpdate()
        {
            if (Camera.main == null) return;
            var dir = Camera.main.transform.position - transform.position;
            if (lockYAxis) dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(-dir);
        }
    }
}
