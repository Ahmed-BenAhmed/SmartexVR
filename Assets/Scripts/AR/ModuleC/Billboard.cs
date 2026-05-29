using UnityEngine;

namespace Smartex.AR.ModuleC
{
    public class Billboard : MonoBehaviour
    {
        private Transform mainCameraTransform;

        void Start()
        {
            // Automatically find the main camera tracking your viewpoint
            if (Camera.main != null)
            {
                mainCameraTransform = Camera.main.transform;
            }
        }

        // LateUpdate runs after regular object movements to prevent the UI from jittering
        void LateUpdate()
        {
            if (mainCameraTransform != null)
            {
                // Forces the UI to look directly at the camera position smoothly
                transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                                 mainCameraTransform.rotation * Vector3.up);
            }
        }
    }
}