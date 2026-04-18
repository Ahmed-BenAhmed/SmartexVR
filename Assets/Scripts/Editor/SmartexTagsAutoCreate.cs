#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Smartex.Editor
{
    [InitializeOnLoad]
    public static class SmartexTagsAutoCreate
    {
        static SmartexTagsAutoCreate() => EnsureTags();

        [MenuItem("Smartex/Setup/Register Tags")]
        public static void EnsureTags() => AddTagIfMissing("Generated");

        static void AddTagIfMissing(string tag)
        {
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsArray = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsArray.arraySize; i++)
                if (tagsArray.GetArrayElementAtIndex(i).stringValue == tag) return;
            tagsArray.arraySize++;
            tagsArray.GetArrayElementAtIndex(tagsArray.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[SmartexTags] Registered tag '{tag}'");
        }
    }
}
#endif
