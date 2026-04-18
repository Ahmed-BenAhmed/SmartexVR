// MODULE A — AR Foundation Core  (Member 1)
// Owner   : assign to member 1
// Purpose : Bootstrap ARSession, configure plane detection,
//           expose the shared ARRaycastManager for other modules.
//
// Setup checklist (do this before writing logic):
//  1. Window > XR > XR Plugin Management → enable ARCore (Android) + ARKit (iOS)
//  2. Player Settings > Android > Min API 24, target ARM64
//  3. Player Settings > iOS > Camera Usage Description filled in
//  4. Add this component to the AR Session Origin GameObject in SmartexAR scene
//
// Dependencies: AR Foundation 6.x  (already in Packages/manifest.json)

using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Smartex.AR.Core
{
    /// <summary>
    /// Singleton that owns the AR session lifecycle and exposes shared
    /// AR managers to other modules via static accessors.
    /// </summary>
    [RequireComponent(typeof(ARSession))]
    public class ARSessionManager : MonoBehaviour
    {
        public static ARSessionManager Instance { get; private set; }

        [Header("AR Managers")]
        public ARPlaneManager    planeManager;
        public ARAnchorManager   anchorManager;
        public ARRaycastManager  raycastManager;

        [Header("Settings")]
        [Tooltip("Show floor plane visualisation in dev builds; hide in release.")]
        public bool showPlaneVisualization = true;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (planeManager != null)
                planeManager.enabled = true;

            // TODO Member 1: configure plane prefab, set detection mode
            // planeManager.planePrefab = ...;
            // planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        /// <summary>
        /// Called by Module B when a machine QR is tracked.
        /// Creates a world anchor at the detected pose so the overlay persists
        /// as the user walks around.
        /// </summary>
        public ARAnchor CreateAnchor(Pose worldPose)
        {
            if (anchorManager == null) return null;
            var go = new GameObject("MachineAnchor");
            go.transform.SetPositionAndRotation(worldPose.position, worldPose.rotation);
            return go.AddComponent<ARAnchor>();
        }
    }
}
