using UnityEngine;

namespace Mindrift.Online.Presence
{
    public static class DiscordRichPresenceBootstrap
    {
        private const string RuntimeObjectName = "DiscordRichPresenceRuntime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            DiscordRichPresenceManager existing = Object.FindFirstObjectByType<DiscordRichPresenceManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return;
            }

            GameObject runtimeObject = new GameObject(RuntimeObjectName);
            Object.DontDestroyOnLoad(runtimeObject);
            runtimeObject.AddComponent<DiscordRichPresenceManager>();
        }
    }
}
