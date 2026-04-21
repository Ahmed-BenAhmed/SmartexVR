#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Smartex.Editor
{
    /// <summary>
    /// One-click VR-ification of the currently-open scene for Quest 2 testing.
    ///
    /// What it does:
    ///   1. Disables existing CameraController (desktop orbit/fly/top-down)
    ///   2. Disables the existing Main Camera
    ///   3. Creates an XR Origin (VR) hierarchy using reflection, so the script
    ///      still compiles even before the XR packages finish resolving.
    ///   4. Positions the rig at the current Main Camera position, 1.6 m off the floor
    ///
    /// Prerequisites (see docs/vr-quest2-runbook.md):
    ///   - OpenXR + XR Interaction Toolkit packages installed
    ///   - XR Plug-in Management → Android → OpenXR loader enabled
    ///   - OpenXR → Android → Meta Quest interaction profile added
    ///
    /// Reversible: the original camera + controller are disabled, not deleted.
    /// </summary>
    public static class VRRigBuilder
    {
        const float DefaultEyeHeight = 1.6f;

        [MenuItem("Smartex VR/Convert Scene to VR (Quest 2)")]
        public static void ConvertSceneToVR()
        {
            // 1. Disable the desktop camera controller
            var controllers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int disabled = 0;
            foreach (var c in controllers)
            {
                if (c != null && c.GetType().Name == "CameraController")
                {
                    c.enabled = false;
                    disabled++;
                }
            }

            // 2. Disable the existing Main Camera (keep it so you can flip back)
            var mainCam = Camera.main;
            Vector3 rigPos = Vector3.zero;
            if (mainCam != null)
            {
                rigPos = new Vector3(mainCam.transform.position.x, 0f, mainCam.transform.position.z);
                mainCam.gameObject.SetActive(false);
            }

            // 3. Build the XR Origin (VR) hierarchy
            //    We do this by name / reflection so this editor script compiles even
            //    if someone opens the repo before packages finish resolving.
            var xrOriginType        = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            var trackedPoseDriver   = Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem");

            if (xrOriginType == null)
            {
                EditorUtility.DisplayDialog(
                    "VR packages not ready",
                    "Could not find Unity.XR.CoreUtils.XROrigin. Open Package Manager, " +
                    "let OpenXR + XR Interaction Toolkit finish resolving, then retry.\n\n" +
                    "Also check: Project Settings → XR Plug-in Management → Android → OpenXR loader is ENABLED.",
                    "OK");
                return;
            }

            // Root: "XR Origin (VR)"
            var rigRoot = new GameObject("XR Origin (VR)");
            rigRoot.transform.position = rigPos;
            var origin = rigRoot.AddComponent(xrOriginType) as MonoBehaviour;

            // Child: "Camera Offset"
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rigRoot.transform, false);
            offset.transform.localPosition = new Vector3(0f, DefaultEyeHeight, 0f);

            // Grandchild: "Main Camera"
            var camGO = new GameObject("Main Camera");
            camGO.transform.SetParent(offset.transform, false);
            var cam = camGO.AddComponent<Camera>();
            cam.tag               = "MainCamera";
            cam.nearClipPlane     = 0.05f;
            cam.farClipPlane      = 500f;
            cam.clearFlags        = CameraClearFlags.SolidColor;
            cam.backgroundColor   = new Color(0.05f, 0.06f, 0.08f);
            camGO.AddComponent<AudioListener>();
            if (trackedPoseDriver != null) camGO.AddComponent(trackedPoseDriver);

            // Wire XROrigin: set Camera + CameraFloorOffsetObject via SerializedObject
            try
            {
                var so = new SerializedObject(origin);
                var camProp = so.FindProperty("m_Camera");
                if (camProp != null) camProp.objectReferenceValue = cam;
                var offsetProp = so.FindProperty("m_CameraFloorOffsetObject");
                if (offsetProp != null) offsetProp.objectReferenceValue = offset;
                var modeProp = so.FindProperty("m_RequestedTrackingOriginMode");
                if (modeProp != null) modeProp.enumValueIndex = 1; // Floor
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception e) { Debug.LogWarning($"[VRRigBuilder] XROrigin wiring: {e.Message}"); }

            Selection.activeGameObject = rigRoot;
            EditorUtility.SetDirty(rigRoot);

            Debug.Log($"[VRRigBuilder] ✅ Scene converted. Disabled {disabled} CameraController(s). " +
                      $"Rig placed at {rigPos}. Put the headset on and hit Play (or Build and Run).");
        }

        [MenuItem("Smartex VR/Revert Scene to Desktop")]
        public static void RevertSceneToDesktop()
        {
            var rig = GameObject.Find("XR Origin (VR)");
            if (rig != null) UnityEngine.Object.DestroyImmediate(rig);

            // Re-enable any camera tagged MainCamera
            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.CompareTag("MainCamera") && !cam.gameObject.activeSelf)
                    cam.gameObject.SetActive(true);
            }

            // Re-enable CameraController
            foreach (var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c != null && c.GetType().Name == "CameraController") c.enabled = true;
            }

            Debug.Log("[VRRigBuilder] Reverted to desktop camera.");
        }
    }
}
#endif
