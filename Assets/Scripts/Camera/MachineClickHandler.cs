using UnityEngine;
using Smartex.Machines;

namespace Smartex.CameraControl
{
    public class MachineClickHandler : MonoBehaviour
    {
        public float maxDistance = 200f;
        private UnityEngine.Camera _cam;

        void Awake() => _cam = GetComponent<UnityEngine.Camera>();

        void Update()
        {
            if (_cam == null) return;
            if (!Input.GetMouseButtonDown(0)) return;

            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance)) return;

            var mc = hit.collider.GetComponentInParent<MachineController>();
            if (mc != null) mc.NotifyClicked();
        }
    }
}
