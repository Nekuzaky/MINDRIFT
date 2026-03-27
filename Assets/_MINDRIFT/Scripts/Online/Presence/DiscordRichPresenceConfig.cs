using UnityEngine;

namespace Mindrift.Online.Presence
{
    [CreateAssetMenu(fileName = "MindriftDiscordPresenceConfig", menuName = "MINDRIFT/Online/Discord Presence Config")]
    public sealed class DiscordRichPresenceConfig : ScriptableObject
    {
        private const string ResourcePath = "MindriftDiscordPresenceConfig";

        [Header("Connection")]
        [SerializeField] private bool enabledByDefault = true;
        [SerializeField] private long applicationId = 1486474422868119715;
        [SerializeField] [Min(0.25f)] private float refreshIntervalSeconds = 1f;
        [SerializeField] private bool verboseLogging;

        [Header("Text - Main Menu")]
        [SerializeField] private string mainMenuDetails = "Main Menu";
        [SerializeField] private string mainMenuState = "Preparing the next drift";

        [Header("Text - Gameplay")]
        [SerializeField] private string inRunDetails = "In Run";
        [SerializeField] private string inRunState = "Climbing through cognitive noise";
        [SerializeField] private string pausedDetails = "Paused";
        [SerializeField] private string pausedState = "Catching a breath";
        [SerializeField] private string gameOverDetails = "Game Over";
        [SerializeField] private string gameOverState = "Neural collapse";
        [SerializeField] private string gameplayIdleDetails = "Gameplay";
        [SerializeField] private string gameplayIdleState = "Exploring the rift";

        [Header("Images")]
        [SerializeField] private string largeImageKey = "";
        [SerializeField] private string largeImageText = "MINDRIFT";
        [SerializeField] private string smallImageKeyRun = "";
        [SerializeField] private string smallImageTextRun = "In Run";
        [SerializeField] private string smallImageKeyPause = "";
        [SerializeField] private string smallImageTextPause = "Paused";
        [SerializeField] private string smallImageKeyGameOver = "";
        [SerializeField] private string smallImageTextGameOver = "Game Over";
        [SerializeField] private string smallImageKeyMenu = "";
        [SerializeField] private string smallImageTextMenu = "Main Menu";

        [Header("Buttons")]
        [SerializeField] private bool showPrimaryButton;
        [SerializeField] private string primaryButtonLabel = "Open Website";
        [SerializeField] private string primaryButtonUrl = "https://nekuzaky.com/mindrift";
        [SerializeField] private bool showSecondaryButton;
        [SerializeField] private string secondaryButtonLabel = "Join Discord";
        [SerializeField] private string secondaryButtonUrl = "";

        [Header("Timer")]
        [SerializeField] private bool showRunTimer = true;

        private static DiscordRichPresenceConfig cached;

        public bool EnabledByDefault => enabledByDefault;
        public long ApplicationId => applicationId;
        public float RefreshIntervalSeconds => Mathf.Max(0.25f, refreshIntervalSeconds);
        public bool VerboseLogging => verboseLogging;

        public string MainMenuDetails => mainMenuDetails;
        public string MainMenuState => mainMenuState;
        public string InRunDetails => inRunDetails;
        public string InRunState => inRunState;
        public string PausedDetails => pausedDetails;
        public string PausedState => pausedState;
        public string GameOverDetails => gameOverDetails;
        public string GameOverState => gameOverState;
        public string GameplayIdleDetails => gameplayIdleDetails;
        public string GameplayIdleState => gameplayIdleState;

        public string LargeImageKey => largeImageKey;
        public string LargeImageText => largeImageText;
        public string SmallImageKeyRun => smallImageKeyRun;
        public string SmallImageTextRun => smallImageTextRun;
        public string SmallImageKeyPause => smallImageKeyPause;
        public string SmallImageTextPause => smallImageTextPause;
        public string SmallImageKeyGameOver => smallImageKeyGameOver;
        public string SmallImageTextGameOver => smallImageTextGameOver;
        public string SmallImageKeyMenu => smallImageKeyMenu;
        public string SmallImageTextMenu => smallImageTextMenu;

        public bool ShowPrimaryButton => showPrimaryButton && IsValidButton(primaryButtonLabel, primaryButtonUrl);
        public string PrimaryButtonLabel => primaryButtonLabel;
        public string PrimaryButtonUrl => primaryButtonUrl;
        public bool ShowSecondaryButton => showSecondaryButton && IsValidButton(secondaryButtonLabel, secondaryButtonUrl);
        public string SecondaryButtonLabel => secondaryButtonLabel;
        public string SecondaryButtonUrl => secondaryButtonUrl;
        public bool ShowRunTimer => showRunTimer;

        public static DiscordRichPresenceConfig Active => cached != null ? cached : (cached = LoadConfig());

        public static void Reload()
        {
            cached = LoadConfig();
        }

        private static DiscordRichPresenceConfig LoadConfig()
        {
            DiscordRichPresenceConfig loaded = Resources.Load<DiscordRichPresenceConfig>(ResourcePath);
            if (loaded != null)
            {
                return loaded;
            }

            DiscordRichPresenceConfig fallback = CreateInstance<DiscordRichPresenceConfig>();
            fallback.name = "RuntimeDiscordPresenceConfig";
            return fallback;
        }

        private static bool IsValidButton(string label, string url)
        {
            return !string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(url);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            refreshIntervalSeconds = Mathf.Max(0.25f, refreshIntervalSeconds);
        }
#endif
    }
}
