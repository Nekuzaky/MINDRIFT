#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mindrift.UI;

namespace Mindrift.Editor
{
    public static class PauseMenuSceneBuilder
    {
        private const string BreakScenePath = "Assets/Scenes/Break.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/Games.unity";

        [MenuItem("Tools/MINDRIFT/Create Or Refresh Pause Scene")]
        public static void CreateOrRefresh()
        {
            Scene pauseScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureEventSystem();

            GameObject canvasGO = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            Stretch(canvasRect);

            GameObject background = CreateUIObject("Background", canvasGO.transform);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.02f, 0.03f, 0.08f, 0.88f);
            Stretch(background.GetComponent<RectTransform>());

            GameObject title = CreateText("Title", canvasGO.transform, "PAUSE", 92, TextAnchor.MiddleCenter, new Color(0.95f, 0.98f, 1f, 1f));
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleRect.sizeDelta = new Vector2(900f, 140f);
            titleRect.anchoredPosition = Vector2.zero;

            GameObject hint = CreateText("Hint", canvasGO.transform, "ESC = REPRENDRE  |  M = MENU PRINCIPAL", 30, TextAnchor.MiddleCenter, new Color(0.55f, 0.96f, 1f, 0.95f));
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0.62f);
            hintRect.anchorMax = new Vector2(0.5f, 0.62f);
            hintRect.sizeDelta = new Vector2(1400f, 90f);
            hintRect.anchoredPosition = Vector2.zero;

            Button resumeButton = CreateButton("ResumeButton", canvasGO.transform, "REPRENDRE", new Vector2(0.5f, 0.48f), new Vector2(440f, 96f));
            Button mainMenuButton = CreateButton("MainMenuButton", canvasGO.transform, "MENU PRINCIPAL", new Vector2(0.5f, 0.36f), new Vector2(440f, 96f));

            PauseSceneController pauseController = canvasGO.AddComponent<PauseSceneController>();
            SerializedObject pauseSO = new SerializedObject(pauseController);
            pauseSO.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            pauseSO.FindProperty("mainMenuButton").objectReferenceValue = mainMenuButton;
            pauseSO.FindProperty("gameplaySceneName").stringValue = "Games";
            pauseSO.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            pauseSO.FindProperty("mainMenuSceneFallbackName").stringValue = "MainMenue";
            pauseSO.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(pauseScene, BreakScenePath);

            AttachPauseControllerToGameplayScene();
            AddScenesToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MINDRIFT] Pause scene created at {BreakScenePath} and gameplay pause controller wired.");
        }

        private static void AttachPauseControllerToGameplayScene()
        {
            if (!File.Exists(GameplayScenePath))
            {
                Debug.LogWarning($"[MINDRIFT] Gameplay scene not found at {GameplayScenePath}.");
                return;
            }

            Scene gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            GameObject host = GameObject.Find("GameplayCanvas");
            if (host == null)
            {
                host = new GameObject("GameplayCanvas");
            }

            GameplayPauseController pauseController = host.GetComponent<GameplayPauseController>();
            if (pauseController == null)
            {
                pauseController = host.AddComponent<GameplayPauseController>();
            }

            SerializedObject so = new SerializedObject(pauseController);
            so.FindProperty("gameplaySceneName").stringValue = "Games";
            so.FindProperty("pauseSceneName").stringValue = "Break";
            so.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            so.FindProperty("mainMenuSceneFallbackName").stringValue = "MainMenue";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(gameplayScene);
            EditorSceneManager.SaveScene(gameplayScene, GameplayScenePath);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void AddScenesToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AddOrEnableScene(scenes, MainMenuScenePath);
            AddOrEnableScene(scenes, GameplayScenePath);
            AddOrEnableScene(scenes, BreakScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddOrEnableScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    scenes[i] = new EditorBuildSettingsScene(path, true);
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Text uiText = go.AddComponent<Text>();
            uiText.text = text;
            uiText.alignment = alignment;
            uiText.fontSize = fontSize;
            uiText.color = color;
            uiText.font = ResolveBuiltinFont();
            uiText.raycastTarget = false;
            return go;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 size)
        {
            GameObject buttonGO = CreateUIObject(name, parent);
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.25f, 0.92f);

            Button button = buttonGO.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.25f, 0.45f, 0.6f, 1f);
            colors.pressedColor = new Color(0.1f, 0.25f, 0.35f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            button.colors = colors;

            GameObject labelGO = CreateText("Label", buttonGO.transform, label, 30, TextAnchor.MiddleCenter, new Color(0.95f, 0.98f, 1f, 1f));
            Stretch(labelGO.GetComponent<RectTransform>());

            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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

        private static Font ResolveBuiltinFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
#endif
