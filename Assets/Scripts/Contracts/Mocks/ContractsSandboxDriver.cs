using UnityEngine;

namespace Smartex.AR.Contracts.Mocks
{
    /// <summary>
    /// One-stop sandbox: drop this on an empty GameObject in a dev scene and
    /// it spawns all four mocks as siblings. Press 1..8 to fake machine
    /// recognition, 0 to fake loss.
    ///
    /// Delete / disable this GameObject in production scenes — real
    /// implementations should register themselves instead.
    /// </summary>
    public class ContractsSandboxDriver : MonoBehaviour
    {
        public MockMachineRecognizer  recognizer;
        public MockMaintenanceService maintenance;
        public MockRemoteAssistService remoteAssist;
        public MockTrainingService    training;

        void Awake()
        {
            if (recognizer   == null) recognizer   = gameObject.AddComponent<MockMachineRecognizer>();
            if (maintenance  == null) maintenance  = gameObject.AddComponent<MockMaintenanceService>();
            if (remoteAssist == null) remoteAssist = gameObject.AddComponent<MockRemoteAssistService>();
            if (training     == null) training     = gameObject.AddComponent<MockTrainingService>();

            recognizer.StartScanning();

            Debug.Log("[ContractsSandbox] mocks live — press 1..8 to fake recognition, 0 to fake loss.");
        }

        void OnDestroy() => ARServices.ClearAll();
    }
}
