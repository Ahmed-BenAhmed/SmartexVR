using UnityEngine;

namespace Smartex.AR.Contracts
{
    /// <summary>
    /// Tiny service locator so consumers don't have to FindFirstObjectByType
    /// every frame, and don't have to worry about whether an interface is
    /// backed by a MonoBehaviour or a plain class.
    ///
    /// Wiring rule:
    ///   - Mocks register themselves in Awake when the dev sandbox scene loads
    ///     (see ContractsSandboxDriver).
    ///   - Real implementations register themselves in Awake when the
    ///     production bootstrapper runs.
    ///
    /// If a consumer asks for a service that hasn't been registered yet, it
    /// gets null — log a warning, don't crash.
    /// </summary>
    public static class ARServices
    {
        public static IMachineRecognizer  Recognizer  { get; private set; }
        public static IMaintenanceService Maintenance { get; private set; }
        public static IRemoteAssistService RemoteAssist { get; private set; }
        public static ITrainingService    Training    { get; private set; }

        public static void Register(IMachineRecognizer  s) { Recognizer   = s; Log(nameof(IMachineRecognizer),   s); }
        public static void Register(IMaintenanceService s) { Maintenance  = s; Log(nameof(IMaintenanceService),  s); }
        public static void Register(IRemoteAssistService s){ RemoteAssist = s; Log(nameof(IRemoteAssistService), s); }
        public static void Register(ITrainingService    s) { Training     = s; Log(nameof(ITrainingService),     s); }

        /// <summary>Call this when a scene unloads or on app quit to avoid stale references.</summary>
        public static void ClearAll()
        {
            Recognizer = null; Maintenance = null; RemoteAssist = null; Training = null;
        }

        static void Log(string iface, object impl)
            => Debug.Log($"[ARServices] {iface} ← {impl?.GetType().Name ?? "null"}");
    }
}
