// MODULE B — Machine Recognition  (Member 2)
// Fallback UI when QR label is obscured or camera can't see it.
// Shows a scrollable list of all 8 machine IDs; tap one to trigger
// the same OnMachineRecognised event as QR tracking.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Smartex.Core;

namespace Smartex.AR.Recognition
{
    public class ManualMachineSelector : MonoBehaviour
    {
        [Header("UI")]
        public GameObject    panelRoot;
        public Transform     listContainer;
        public GameObject    rowPrefab;     // Button + TMP label

        void Start()
        {
            if (DataManager.Instance == null) return;
            // TODO Member 2: populate list from DataManager.LastSnapshot.machines
            // and on row click call:
            //   MachineQRTracker.OnMachineRecognised?.Invoke(deviceId, defaultPose);
        }

        public void Show() => panelRoot?.SetActive(true);
        public void Hide() => panelRoot?.SetActive(false);
    }
}
