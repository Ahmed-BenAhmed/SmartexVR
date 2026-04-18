using UnityEngine;
using Smartex.Core;

namespace Smartex.Factory
{
    [DefaultExecutionOrder(-100)]
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Optional: drag in if already in scene")]
        public DataManager   dataManager;
        public FactoryBuilder factoryBuilder;

        void Awake()
        {
            if (dataManager == null)
                dataManager = GetComponent<DataManager>()
                           ?? gameObject.AddComponent<DataManager>();

            if (factoryBuilder == null)
                factoryBuilder = FindFirstObjectByType<FactoryBuilder>(FindObjectsInactive.Include);

            Debug.Log("[SceneBootstrap] SmartexVR scene initialised.");
        }

        [ContextMenu("Log Status")]
        void LogStatus()
        {
            var dm = DataManager.Instance;
            if (dm == null) { Debug.Log("DataManager: not found"); return; }
            Debug.Log($"DataManager: connected={dm.IsConnected}  lastUpdate={dm.LastUpdateUTC:HH:mm:ss}  machines={dm.LastSnapshot?.machines.Count ?? 0}");
        }
    }
}
