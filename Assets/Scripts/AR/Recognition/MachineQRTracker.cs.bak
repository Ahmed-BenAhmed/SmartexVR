// MODULE B — Machine Recognition  (Member 2)
// Owner   : assign to member 2
// Purpose : Track printed QR labels on physical looms.
//           Each label encodes the device_id (e.g. "ESP32_TEX_003").
//           On recognition → fire OnMachineRecognised so Module C can show overlay.
//
// QR label format  :  plain text = device_id  e.g.  ESP32_TEX_003
// Physical label   :  print QRLabel_Template.pdf (see Docs/AR/qr-labels/)
//                     laminate + attach to loom control panel
//
// Setup checklist:
//  1. Create an XRReferenceImageLibrary asset (Assets/AR/MarkerLibrary.asset)
//  2. Add one entry per loom QR image (or use runtime-added images for dynamic QR)
//  3. Assign the library to ARTrackedImageManager.referenceLibrary
//  4. Assign TrackedImagePrefab = a prefab that has MachineQRTracker on it
//
// Fallback: if QR is obscured, ManualMachineSelector.cs provides a list UI.

using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Smartex.Core;

namespace Smartex.AR.Recognition
{
    /// <summary>
    /// Listens to ARTrackedImageManager events and resolves device_id from
    /// the image name, then fires OnMachineRecognised for the overlay module.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class MachineQRTracker : MonoBehaviour
    {
        /// <summary>Fired when a machine QR enters tracking. Arg = device_id.</summary>
        public static event Action<string, Pose> OnMachineRecognised;

        /// <summary>Fired when a tracked QR is lost (machine moved out of view).</summary>
        public static event Action<string>       OnMachineLost;

        private ARTrackedImageManager _imageManager;

        void Awake()  => _imageManager = GetComponent<ARTrackedImageManager>();
        void OnEnable()  => _imageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        void OnDisable() => _imageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);

        void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (var img in args.added)
                HandleImage(img, added: true);

            foreach (var img in args.updated)
                if (img.trackingState == TrackingState.Tracking)
                    HandleImage(img, added: false);

            foreach (var img in args.removed)
                OnMachineLost?.Invoke(img.Value.referenceImage.name);
        }

        void HandleImage(ARTrackedImage img, bool added)
        {
            // Reference image name = device_id  (set this in the image library)
            string deviceId = img.referenceImage.name;
            var    pose     = new Pose(img.transform.position, img.transform.rotation);

            var data = DataManager.Instance?.GetMachine(deviceId);
            if (data == null)
                Debug.LogWarning($"[QRTracker] Recognised '{deviceId}' but DataManager has no data for it.");

            if (added)
                OnMachineRecognised?.Invoke(deviceId, pose);
        }
    }
}
