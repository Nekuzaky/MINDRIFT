using System;
using Mindrift.Core;
using Mindrift.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

#if MINDRIFT_DISCORD_SDK
using Discord;
#endif

namespace Mindrift.Online.Presence
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-7000)]
    public sealed class DiscordRichPresenceManager : MonoBehaviour
    {
        private enum PresenceKind
        {
            None,
            MainMenu,
            InRun,
            Paused,
            GameOver,
            GameplayIdle
        }

        private DiscordRichPresenceConfig config;
        private long runStartTimestampUnix;

#if MINDRIFT_DISCORD_SDK
        private float nextRefreshAt;
        private PresencePayload lastPublished;
        private global::Discord.Discord discord;
        private ActivityManager activityManager;
        private bool sdkReady;
#endif

        private LivesSystem livesSystem;
        private RunSessionManager runSessionManager;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            config = DiscordRichPresenceConfig.Active;
            runStartTimestampUnix = 0;

#if MINDRIFT_DISCORD_SDK
            nextRefreshAt = 0f;
            TryInitializeSdk();
#else
            if (config.EnabledByDefault && config.VerboseLogging)
            {
                Debug.Log("[MINDRIFT][Discord] Rich Presence disabled at compile time. Add scripting define symbol 'MINDRIFT_DISCORD_SDK' after importing Discord Game SDK.");
            }

            enabled = false;
#endif
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
#if MINDRIFT_DISCORD_SDK
            if (!sdkReady)
            {
                return;
            }

            try
            {
                discord.RunCallbacks();
            }
            catch (Exception exception)
            {
                Log($"Callback error: {exception.Message}");
            }

            if (Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.unscaledTime + config.RefreshIntervalSeconds;
            PublishCurrentPresence(force: false);
#endif
        }

        private void OnApplicationQuit()
        {
#if MINDRIFT_DISCORD_SDK
            try
            {
                if (activityManager != null)
                {
                    activityManager.ClearActivity(_ => { });
                }
            }
            catch
            {
                // Ignore shutdown errors.
            }

            if (discord != null)
            {
                discord.Dispose();
                discord = null;
            }
#endif
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveRuntimeReferences(force: true);
            PublishCurrentPresence(force: true);
        }

        private void ResolveRuntimeReferences(bool force)
        {
            if (force || livesSystem == null)
            {
                livesSystem = FindFirstObjectByType<LivesSystem>(FindObjectsInactive.Include);
            }

            if (force || runSessionManager == null)
            {
                runSessionManager = FindFirstObjectByType<RunSessionManager>(FindObjectsInactive.Include);
            }
        }

        private PresencePayload BuildPayload()
        {
            ResolveRuntimeReferences(force: false);

            string activeSceneName = SceneManager.GetActiveScene().name;
            if (IsMainMenuScene(activeSceneName))
            {
                runStartTimestampUnix = 0;
                return new PresencePayload(
                    PresenceKind.MainMenu,
                    config.MainMenuDetails,
                    config.MainMenuState,
                    0,
                    config.SmallImageKeyMenu,
                    config.SmallImageTextMenu);
            }

            if (livesSystem != null && livesSystem.IsGameOver)
            {
                runStartTimestampUnix = 0;
                return new PresencePayload(
                    PresenceKind.GameOver,
                    config.GameOverDetails,
                    config.GameOverState,
                    0,
                    config.SmallImageKeyGameOver,
                    config.SmallImageTextGameOver);
            }

            if (GameplayPauseController.IsPaused || IsPauseSceneLoaded())
            {
                return new PresencePayload(
                    PresenceKind.Paused,
                    config.PausedDetails,
                    config.PausedState,
                    0,
                    config.SmallImageKeyPause,
                    config.SmallImageTextPause);
            }

            if (runSessionManager != null && runSessionManager.IsRunning)
            {
                if (runStartTimestampUnix <= 0)
                {
                    runStartTimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }

                long startTimestamp = config.ShowRunTimer ? runStartTimestampUnix : 0;
                string state = $"{config.InRunState} | Falls: {runSessionManager.FallCount}";

                return new PresencePayload(
                    PresenceKind.InRun,
                    config.InRunDetails,
                    state,
                    startTimestamp,
                    config.SmallImageKeyRun,
                    config.SmallImageTextRun);
            }

            return new PresencePayload(
                PresenceKind.GameplayIdle,
                config.GameplayIdleDetails,
                config.GameplayIdleState,
                0,
                config.SmallImageKeyRun,
                "Idle");
        }

        private static bool IsMainMenuScene(string sceneName)
        {
            return string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(sceneName, "MainMenue", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPauseSceneLoaded()
        {
            Scene pauseScene = SceneManager.GetSceneByName("Break");
            return pauseScene.IsValid() && pauseScene.isLoaded;
        }

        private void PublishCurrentPresence(bool force)
        {
#if MINDRIFT_DISCORD_SDK
            if (!sdkReady || activityManager == null)
            {
                return;
            }

            PresencePayload payload = BuildPayload();
            if (!force && payload.Equals(lastPublished))
            {
                return;
            }

            Activity activity = new Activity
            {
                Type = ActivityType.Playing,
                Details = Truncate(payload.Details, 128),
                State = Truncate(payload.State, 128),
                Assets =
                {
                    LargeImage = Truncate(config.LargeImageKey, 128),
                    LargeText = Truncate(config.LargeImageText, 128),
                    SmallImage = Truncate(payload.SmallImageKey, 128),
                    SmallText = Truncate(payload.SmallImageText, 128)
                }
            };

            if (payload.StartTimestampUnix > 0)
            {
                activity.Timestamps.Start = payload.StartTimestampUnix;
            }

            ActivityButton[] buttons = BuildButtons();
            if (buttons != null && buttons.Length > 0)
            {
                activity.Buttons = buttons;
            }

            activityManager.UpdateActivity(activity, result =>
            {
                if (result != Result.Ok)
                {
                    Log($"UpdateActivity failed: {result}");
                    return;
                }

                lastPublished = payload;
                Log($"Presence updated: {payload.Kind} | {payload.Details} | {payload.State}");
            });
#endif
        }

#if MINDRIFT_DISCORD_SDK
        private ActivityButton[] BuildButtons()
        {
            bool includePrimary = config.ShowPrimaryButton;
            bool includeSecondary = config.ShowSecondaryButton;

            if (!includePrimary && !includeSecondary)
            {
                return null;
            }

            if (includePrimary && includeSecondary)
            {
                return new[]
                {
                    new ActivityButton
                    {
                        Label = Truncate(config.PrimaryButtonLabel, 32),
                        Url = Truncate(config.PrimaryButtonUrl, 512)
                    },
                    new ActivityButton
                    {
                        Label = Truncate(config.SecondaryButtonLabel, 32),
                        Url = Truncate(config.SecondaryButtonUrl, 512)
                    }
                };
            }

            if (includePrimary)
            {
                return new[]
                {
                    new ActivityButton
                    {
                        Label = Truncate(config.PrimaryButtonLabel, 32),
                        Url = Truncate(config.PrimaryButtonUrl, 512)
                    }
                };
            }

            return new[]
            {
                new ActivityButton
                {
                    Label = Truncate(config.SecondaryButtonLabel, 32),
                    Url = Truncate(config.SecondaryButtonUrl, 512)
                }
            };
        }
#endif

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string safe = value.Trim();
            if (safe.Length <= maxLength)
            {
                return safe;
            }

            return safe.Substring(0, maxLength);
        }

        private void Log(string message)
        {
            if (!config.VerboseLogging)
            {
                return;
            }

            Debug.Log($"[MINDRIFT][Discord] {message}");
        }

#if MINDRIFT_DISCORD_SDK
        private void TryInitializeSdk()
        {
            if (!config.EnabledByDefault)
            {
                enabled = false;
                return;
            }

            if (config.ApplicationId <= 0)
            {
                Debug.LogWarning("[MINDRIFT][Discord] Invalid Application ID. Rich Presence disabled.");
                enabled = false;
                return;
            }

            try
            {
                discord = new global::Discord.Discord(config.ApplicationId, (ulong)CreateFlags.NoRequireDiscord);
                activityManager = discord.GetActivityManager();
                sdkReady = activityManager != null;
            }
            catch (Exception exception)
            {
                sdkReady = false;
                Debug.LogWarning($"[MINDRIFT][Discord] Failed to initialize Discord SDK: {exception.Message}");
            }

            if (!sdkReady)
            {
                enabled = false;
                return;
            }

            PublishCurrentPresence(force: true);
            Log("Discord Rich Presence initialized.");
        }
#endif

        private readonly struct PresencePayload : IEquatable<PresencePayload>
        {
            public readonly PresenceKind Kind;
            public readonly string Details;
            public readonly string State;
            public readonly long StartTimestampUnix;
            public readonly string SmallImageKey;
            public readonly string SmallImageText;

            public PresencePayload(
                PresenceKind kind,
                string details,
                string state,
                long startTimestampUnix,
                string smallImageKey,
                string smallImageText)
            {
                Kind = kind;
                Details = details ?? string.Empty;
                State = state ?? string.Empty;
                StartTimestampUnix = startTimestampUnix;
                SmallImageKey = smallImageKey ?? string.Empty;
                SmallImageText = smallImageText ?? string.Empty;
            }

            public bool Equals(PresencePayload other)
            {
                return Kind == other.Kind &&
                       string.Equals(Details, other.Details, StringComparison.Ordinal) &&
                       string.Equals(State, other.State, StringComparison.Ordinal) &&
                       StartTimestampUnix == other.StartTimestampUnix &&
                       string.Equals(SmallImageKey, other.SmallImageKey, StringComparison.Ordinal) &&
                       string.Equals(SmallImageText, other.SmallImageText, StringComparison.Ordinal);
            }
        }
    }
}
