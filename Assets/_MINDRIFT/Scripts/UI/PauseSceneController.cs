using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mindrift.UI
{
    public sealed class PauseSceneController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string gameplaySceneName = "Games";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string mainMenuSceneFallbackName = "MainMenue";

        [Header("Input")]
        [SerializeField] private KeyCode resumeKey = KeyCode.Escape;
        [SerializeField] private KeyCode mainMenuKey = KeyCode.M;

        [Header("UI")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnEnable()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumePressed);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuPressed);
            }
        }

        private void OnDisable()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumePressed);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuPressed);
            }
        }

        private void Update()
        {
            if (IsKeyPressed(resumeKey))
            {
                OnResumePressed();
                return;
            }

            if (IsKeyPressed(mainMenuKey))
            {
                OnMainMenuPressed();
            }
        }

        public void OnResumePressed()
        {
            if (GameplayPauseController.TryResumeFromPause())
            {
                return;
            }

            if (!IsSceneInBuildSettings(gameplaySceneName))
            {
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnMainMenuPressed()
        {
            if (GameplayPauseController.TryLoadMainMenu())
            {
                return;
            }

            string targetScene = ResolveSceneName(mainMenuSceneName, mainMenuSceneFallbackName, "MainMenu", "MainMenue");
            if (string.IsNullOrWhiteSpace(targetScene))
            {
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(targetScene);
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolveSceneName(params string[] candidates)
        {
            if (candidates == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (IsSceneInBuildSettings(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static bool IsKeyPressed(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && TryMapKeyCode(keyCode, out Key mappedKey))
            {
                var keyControl = Keyboard.current[mappedKey];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(keyCode);
#else
            return false;
#endif
        }

        private static bool TryMapKeyCode(KeyCode keyCode, out Key key)
        {
            switch (keyCode)
            {
                case KeyCode.Escape:
                    key = Key.Escape;
                    return true;
                case KeyCode.M:
                    key = Key.M;
                    return true;
                case KeyCode.Return:
                    key = Key.Enter;
                    return true;
                default:
                    key = Key.None;
                    return false;
            }
        }
    }
}
