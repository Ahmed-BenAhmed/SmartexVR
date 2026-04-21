using System;

namespace Smartex.AR.Contracts
{
    /// <summary>
    /// Module B owns this. Implementations:
    ///   - Production:  VuforiaTargetScanner (aggregates per-target observers)
    ///   - Editor/dev:  MockMachineRecognizer (keyboard-driven fake events)
    ///
    /// Consumers (C, D, E, F) never reference Vuforia directly — they subscribe
    /// to OnMachineRecognized / OnMachineLost and parent their AR content under
    /// RecognizedMachine.AnchorTransform.
    /// </summary>
    public interface IMachineRecognizer
    {
        event Action<RecognizedMachine> OnMachineRecognized;
        event Action<string>            OnMachineLost;   // deviceId

        void StartScanning();
        void StopScanning();
    }
}
