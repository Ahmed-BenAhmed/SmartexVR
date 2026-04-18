using UnityEngine;

namespace Smartex.Factory
{
    [CreateAssetMenu(fileName = "FactoryLayout", menuName = "Smartex/Factory Layout")]
    public class FactoryLayout : ScriptableObject
    {
        [Tooltip("World-space positions for each machine (index matches ESP32_TEX_001..008)")]
        public Vector3[] positions = new Vector3[8];

        [ContextMenu("Preset - TNG-01 Tangier")]
        void PresetTangier()
        {
            positions = new Vector3[]
            {
                new Vector3(-9f, 0f, -3.5f), new Vector3(-3f, 0f, -3.5f),
                new Vector3( 3f, 0f, -3.5f), new Vector3( 9f, 0f, -3.5f),
                new Vector3(-9f, 0f,  3.5f), new Vector3(-3f, 0f,  3.5f),
                new Vector3( 3f, 0f,  3.5f), new Vector3( 9f, 0f,  3.5f),
            };
        }
    }
}
