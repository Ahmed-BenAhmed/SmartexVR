// MODULE G — QA + DevOps + Docs  (Member 7)
// Owner   : assign to member 7
// Purpose : Runtime performance monitor for AR builds.
//           Target: 60 fps sustained on mid-range Android (Snapdragon 665+).
//           Also houses the AR anchor placement test harness.
//
// CI/CD setup → see Docs/AR/ci-cd-setup.md (create this file)
// GitHub Actions workflow → .github/workflows/unity-build.yml (create this file)
//
// Performance budgets:
//   CPU frame time    < 16.6 ms  (60 fps)
//   AR tracking       < 5 ms
//   Overlay UI update < 2 ms
//   Memory           < 800 MB on Android

using UnityEngine;
using TMPro;

namespace Smartex.AR.QA
{
    /// <summary>
    /// Overlays FPS + frame time in dev builds. Disabled in release.
    /// </summary>
    public class ARPerformanceProfiler : MonoBehaviour
    {
        [Header("HUD (assign in scene — disable GO in release builds)")]
        public TextMeshProUGUI fpsLabel;
        public TextMeshProUGUI memLabel;

        [Header("Thresholds")]
        public float targetFPS    = 60f;
        public float warningFPS   = 45f;

        private float _deltaAccum  = 0f;
        private int   _frameCount  = 0;
        private float _updateEvery = 0.5f;

        void Update()
        {
            _deltaAccum += Time.unscaledDeltaTime;
            _frameCount++;

            if (_deltaAccum >= _updateEvery)
            {
                float fps = _frameCount / _deltaAccum;
                _deltaAccum = _frameCount = 0;

                if (fpsLabel != null)
                {
                    fpsLabel.text  = $"{fps:F0} fps  ({1000f/fps:F1} ms)";
                    fpsLabel.color = fps >= targetFPS  ? Color.green :
                                     fps >= warningFPS ? Color.yellow : Color.red;
                }

                if (memLabel != null)
                {
                    long mb = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024*1024);
                    memLabel.text  = $"{mb} MB";
                    memLabel.color = mb < 600 ? Color.green : mb < 800 ? Color.yellow : Color.red;
                }
            }
        }

        // TODO Member 7: add Unity Test Framework play-mode tests for:
        //   - ARSessionManager singleton initialises in < 1 frame
        //   - MachineQRTracker fires OnMachineRecognised with correct device_id
        //   - MachineARPanel.Refresh() updates all labels correctly
        //   - BillboardFacer faces camera within 1 degree
    }
}
