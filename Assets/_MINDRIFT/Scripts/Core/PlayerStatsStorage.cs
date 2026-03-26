using System;
using System.IO;
using System.Collections.Generic;
using Mindrift.Auth;
using UnityEngine;

namespace Mindrift.Core
{
    public static class PlayerStatsStorage
    {
        [Serializable]
        private sealed class PlayerStatsProfileRecord
        {
            public string profileKey = "guest";
            public PlayerStatsData stats = new PlayerStatsData();

            public void Sanitize()
            {
                profileKey = string.IsNullOrWhiteSpace(profileKey) ? "guest" : profileKey.Trim();
                stats ??= new PlayerStatsData();
                stats.Sanitize();
            }
        }

        [Serializable]
        private sealed class PlayerStatsStore
        {
            public List<PlayerStatsProfileRecord> profiles = new List<PlayerStatsProfileRecord>();

            public void Sanitize()
            {
                profiles ??= new List<PlayerStatsProfileRecord>();
                for (int i = profiles.Count - 1; i >= 0; i--)
                {
                    if (profiles[i] == null)
                    {
                        profiles.RemoveAt(i);
                        continue;
                    }

                    profiles[i].Sanitize();
                }
            }
        }

        private const string SaveFileName = "mindrift_player_stats.json";

        private static bool isLoaded;
        private static PlayerStatsData cachedStats;
        private static PlayerStatsStore cachedStore;
        private static string cachedProfileKey;

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static PlayerStatsData Load()
        {
            string activeProfileKey = ResolveActiveProfileKey();
            if (isLoaded && cachedStats != null && string.Equals(cachedProfileKey, activeProfileKey, StringComparison.OrdinalIgnoreCase))
            {
                return cachedStats;
            }

            cachedStore = LoadStoreFromDisk();
            cachedStats = GetOrCreateStats(cachedStore, activeProfileKey);
            cachedStats.Sanitize();
            cachedProfileKey = activeProfileKey;
            isLoaded = true;
            return cachedStats;
        }

        public static void Save()
        {
            PlayerStatsData stats = Load();
            stats.Sanitize();
            stats.lastUpdatedUtc = DateTime.UtcNow.ToString("O");

            try
            {
                string directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                cachedStore ??= new PlayerStatsStore();
                cachedStore.Sanitize();
                GetOrCreateRecord(cachedStore, ResolveActiveProfileKey()).stats = stats;

                string json = JsonUtility.ToJson(cachedStore, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MINDRIFT] Failed to save player stats to '{SavePath}'. {exception.Message}");
            }
        }

        public static void Reset()
        {
            cachedStats = new PlayerStatsData();
            cachedStats.Sanitize();
            cachedStore ??= LoadStoreFromDisk();
            GetOrCreateRecord(cachedStore, ResolveActiveProfileKey()).stats = cachedStats;
            cachedProfileKey = ResolveActiveProfileKey();
            isLoaded = true;
            Save();
        }

        private static PlayerStatsStore LoadStoreFromDisk()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return new PlayerStatsStore();
                }

                string json = File.ReadAllText(SavePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new PlayerStatsStore();
                }

                if (json.IndexOf("\"profiles\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    PlayerStatsStore loadedStore = JsonUtility.FromJson<PlayerStatsStore>(json) ?? new PlayerStatsStore();
                    loadedStore.Sanitize();
                    return loadedStore;
                }

                PlayerStatsData legacyStats = JsonUtility.FromJson<PlayerStatsData>(json) ?? new PlayerStatsData();
                legacyStats.Sanitize();

                PlayerStatsStore migratedStore = new PlayerStatsStore();
                migratedStore.profiles.Add(new PlayerStatsProfileRecord
                {
                    profileKey = "guest",
                    stats = legacyStats
                });
                migratedStore.Sanitize();
                return migratedStore;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MINDRIFT] Failed to load player stats from '{SavePath}'. {exception.Message}");
                return new PlayerStatsStore();
            }
        }

        private static PlayerStatsData GetOrCreateStats(PlayerStatsStore store, string profileKey)
        {
            PlayerStatsProfileRecord record = GetOrCreateRecord(store, profileKey);
            record.Sanitize();
            return record.stats;
        }

        private static PlayerStatsProfileRecord GetOrCreateRecord(PlayerStatsStore store, string profileKey)
        {
            store ??= new PlayerStatsStore();
            store.Sanitize();

            string safeProfileKey = string.IsNullOrWhiteSpace(profileKey) ? "guest" : profileKey.Trim();
            for (int i = 0; i < store.profiles.Count; i++)
            {
                PlayerStatsProfileRecord record = store.profiles[i];
                if (record != null && string.Equals(record.profileKey, safeProfileKey, StringComparison.OrdinalIgnoreCase))
                {
                    return record;
                }
            }

            PlayerStatsProfileRecord created = new PlayerStatsProfileRecord
            {
                profileKey = safeProfileKey,
                stats = new PlayerStatsData()
            };
            created.Sanitize();
            store.profiles.Add(created);
            return created;
        }

        private static string ResolveActiveProfileKey()
        {
            AuthSessionData session = AuthRuntime.Service.CurrentSession;
            if (session == null)
            {
                return "guest";
            }

            session.Sanitize();
            return string.IsNullOrWhiteSpace(session.StatsProfileKey) ? "guest" : session.StatsProfileKey;
        }
    }
}
