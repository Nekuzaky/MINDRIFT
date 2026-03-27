#if UNITY_EDITOR
using Mindrift.Online.Presence;
using UnityEditor;
using UnityEngine;

namespace Mindrift.Editor
{
    public static class DiscordPresenceConfigAssetUtility
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/MindriftDiscordPresenceConfig.asset";

        [MenuItem("Tools/MINDRIFT/Online/Create Or Select Discord Presence Config")]
        public static void CreateOrSelectConfig()
        {
            EnsureFolder(ResourcesFolderPath);

            DiscordRichPresenceConfig existing = AssetDatabase.LoadAssetAtPath<DiscordRichPresenceConfig>(AssetPath);
            if (existing == null)
            {
                DiscordRichPresenceConfig config = ScriptableObject.CreateInstance<DiscordRichPresenceConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                existing = config;
                Debug.Log($"[MINDRIFT][Discord] Config created: {AssetPath}");
            }

            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
