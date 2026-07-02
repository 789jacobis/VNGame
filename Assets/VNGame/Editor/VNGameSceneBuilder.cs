using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VNGame.EditorTools
{
    public static class VNGameSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/VNGameScene.unity";
        private const string PrefabFolder = "Assets/VNGame/Prefabs";
        private const string SaveSlotPrefabPath = PrefabFolder + "/SaveSlotView.prefab";

        [InitializeOnLoadMethod]
        private static void AutoBuildSceneOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (!File.Exists(ScenePath))
                {
                    BuildViewScene();
                }
            };
        }

        [MenuItem("VNGame/Build View Scene")]
        public static void BuildViewScene()
        {
            EnsureFolders();
            var saveSlotPrefab = BuildSaveSlotPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("VNGameRoot");
            var controller = root.AddComponent<VNGameController>();
            var spriteProvider = root.AddComponent<SpriteAssetProvider>();
            controller.spriteProvider = spriteProvider;

            var canvas = CreateCanvas(root.transform);
            var mainMenu = BuildMainMenu(canvas.transform);
            var novel = BuildNovel(canvas.transform);
            var saveLoad = BuildSaveLoad(canvas.transform, saveSlotPrefab);
            var settings = BuildSettings(canvas.transform);
            var backlog = BuildBacklog(canvas.transform);
            var credits = BuildCredits(canvas.transform);

            controller.mainMenuView = mainMenu;
            controller.novelView = novel;
            controller.saveLoadView = saveLoad;
            controller.settingsView = settings;
            controller.backlogView = backlog;
            controller.creditsView = credits;

            CreateEventSystem();
            CreateCamera();

            mainMenu.Show(true);
            novel.Show(false);
            saveLoad.Show(false);
            settings.Show(false);
            backlog.Show(false);
            credits.Show(false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"VNGame view scene generated: {ScenePath}");
        }

        public static void BuildFromCommandLine()
        {
            BuildViewScene();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/VNGame", "Prefabs");
            }
        }

        private static SaveSlotView BuildSaveSlotPrefab()
        {
            var root = CreateRectObject("SaveSlotView", null, new Vector2(290, 156));
            var image = root.AddComponent<Image>();
            image.color = new Color(0.86f, 0.93f, 0.65f, 0.95f);
            var button = root.AddComponent<Button>();
            button.targetGraphic = image;

            var title = CreateText("SlotNameText", root.transform, "Slot 001", 22, TextAnchor.MiddleLeft, new Color(0.18f, 0.25f, 0.16f));
            SetRect(title.rectTransform, new Vector2(14, -10), new Vector2(140, 30), new Vector2(0, 1));

            var thumb = CreatePanel("ThumbnailImage", root.transform, new Color(0.16f, 0.2f, 0.24f, 0.92f));
            SetRect(thumb.rectTransform, new Vector2(25, -42), new Vector2(240, 78), new Vector2(0, 1));

            var time = CreateText("DateTimeText", root.transform, "Empty", 18, TextAnchor.LowerCenter, new Color(0.18f, 0.25f, 0.16f));
            SetRect(time.rectTransform, new Vector2(12, 8), new Vector2(266, 30), new Vector2(0, 0));

            var view = root.AddComponent<SaveSlotView>();
            view.frameImage = image;
            view.thumbnailImage = thumb;
            view.slotNameText = title;
            view.dateTimeText = time;
            view.button = button;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SaveSlotPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SaveSlotView>();
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static MainMenuView BuildMainMenu(Transform parent)
        {
            var root = CreatePanel("MainMenuView", parent, new Color(0.1f, 0.13f, 0.2f, 1f));
            Stretch(root.rectTransform);

            var title = CreateText("TitleText", root.transform, "VNGame", 78, TextAnchor.MiddleLeft, Color.white);
            SetRect(title.rectTransform, new Vector2(100, -120), new Vector2(780, 120), new Vector2(0, 1));

            var subtitle = CreateText("SubtitleText", root.transform, "Visual Novel Prototype", 28, TextAnchor.MiddleLeft, new Color(0.92f, 0.95f, 0.9f));
            SetRect(subtitle.rectTransform, new Vector2(106, -205), new Vector2(780, 60), new Vector2(0, 1));

            var view = root.gameObject.AddComponent<MainMenuView>();
            view.startButton = CreateButton(root.transform, "StartButton", "開始遊戲", new Vector2(120, -330), new Vector2(390, 62), new Vector2(0, 1));
            view.loadButton = CreateButton(root.transform, "LoadButton", "讀取進度", new Vector2(120, -416), new Vector2(390, 62), new Vector2(0, 1));
            view.settingsButton = CreateButton(root.transform, "SettingsButton", "遊戲設定", new Vector2(120, -502), new Vector2(390, 62), new Vector2(0, 1));
            view.creditsButton = CreateButton(root.transform, "CreditsButton", "製作名單", new Vector2(120, -588), new Vector2(390, 62), new Vector2(0, 1));
            view.quitButton = CreateButton(root.transform, "QuitButton", "離開遊戲", new Vector2(120, -674), new Vector2(390, 62), new Vector2(0, 1));
            return view;
        }

        private static NovelView BuildNovel(Transform parent)
        {
            var root = CreatePanel("NovelView", parent, new Color(0.22f, 0.27f, 0.34f, 1f));
            Stretch(root.rectTransform);
            var view = root.gameObject.AddComponent<NovelView>();
            view.backgroundImage = root;

            view.debugLineText = CreateText("DebugLineText", root.transform, "", 24, TextAnchor.MiddleLeft, Color.white);
            SetRect(view.debugLineText.rectTransform, new Vector2(24, -24), new Vector2(600, 40), new Vector2(0, 1));

            view.leftCharacterImage = CreatePanel("LeftCharacterImage", root.transform, new Color(1f, 1f, 1f, 0f));
            SetRect(view.leftCharacterImage.rectTransform, new Vector2(280, 180), new Vector2(520, 720), new Vector2(0, 0));
            view.centerCharacterImage = CreatePanel("CenterCharacterImage", root.transform, new Color(1f, 1f, 1f, 0f));
            SetRect(view.centerCharacterImage.rectTransform, new Vector2(700, 180), new Vector2(520, 720), new Vector2(0, 0));
            view.rightCharacterImage = CreatePanel("RightCharacterImage", root.transform, new Color(1f, 1f, 1f, 0f));
            SetRect(view.rightCharacterImage.rectTransform, new Vector2(1120, 180), new Vector2(520, 720), new Vector2(0, 0));

            var dialogueBox = CreatePanel("DialogueBox", root.transform, new Color(0.08f, 0.1f, 0.14f, 0.86f));
            SetRect(dialogueBox.rectTransform, new Vector2(210, 126), new Vector2(1500, 210), new Vector2(0, 0));
            view.speakerText = CreateText("SpeakerText", dialogueBox.transform, "", 32, TextAnchor.MiddleLeft, new Color(0.72f, 0.86f, 1f));
            SetRect(view.speakerText.rectTransform, new Vector2(34, -18), new Vector2(500, 54), new Vector2(0, 1));
            view.dialogueText = CreateText("DialogueText", dialogueBox.transform, "", 34, TextAnchor.UpperLeft, Color.white);
            SetRect(view.dialogueText.rectTransform, new Vector2(38, -76), new Vector2(1420, 116), new Vector2(0, 1));

            view.statusText = CreateText("StatusText", root.transform, "", 22, TextAnchor.MiddleLeft, Color.white);
            SetRect(view.statusText.rectTransform, new Vector2(155, 72), new Vector2(700, 40), new Vector2(0, 0));
            view.autoButton = CreateButton(root.transform, "AutoButton", "Auto", new Vector2(36, 42), new Vector2(92, 44), new Vector2(0, 0));
            view.skipButton = CreateButton(root.transform, "SkipButton", "Skip", new Vector2(138, 42), new Vector2(92, 44), new Vector2(0, 0));

            var labels = new[] { "Log", "Q.Save", "Q.Load", "Save", "Load", "設定", "主選單" };
            var buttons = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                buttons[i] = CreateButton(root.transform, labels[i].Replace(".", "") + "Button", labels[i], new Vector2(915 + i * 132, 42), new Vector2(118, 44), new Vector2(0, 0));
            }

            view.logButton = buttons[0];
            view.quickSaveButton = buttons[1];
            view.quickLoadButton = buttons[2];
            view.saveButton = buttons[3];
            view.loadButton = buttons[4];
            view.settingsButton = buttons[5];
            view.mainMenuButton = buttons[6];
            return view;
        }

        private static SaveLoadView BuildSaveLoad(Transform parent, SaveSlotView slotPrefab)
        {
            var root = CreatePanel("SaveLoadView", parent, new Color(0f, 0f, 0f, 0.45f));
            Stretch(root.rectTransform);
            var window = CreatePanel("Window", root.transform, new Color(0.08f, 0.12f, 0.12f, 0.94f));
            SetRect(window.rectTransform, new Vector2(160, -100), new Vector2(1600, 880), new Vector2(0, 1));
            var view = root.gameObject.AddComponent<SaveLoadView>();
            view.titleText = CreateText("TitleText", window.transform, "Load", 48, TextAnchor.MiddleLeft, Color.white);
            SetRect(view.titleText.rectTransform, new Vector2(50, -30), new Vector2(500, 70), new Vector2(0, 1));
            view.loadTabButton = CreateButton(window.transform, "LoadTabButton", "Load", new Vector2(260, -88), new Vector2(180, 54), new Vector2(0, 1));
            view.qLoadTabButton = CreateButton(window.transform, "QLoadTabButton", "QLoad", new Vector2(460, -88), new Vector2(180, 54), new Vector2(0, 1));
            view.closeButton = CreateButton(window.transform, "CloseButton", "X", new Vector2(-50, -30), new Vector2(64, 64), new Vector2(1, 1));
            var content = CreatePanel("SlotContainer", window.transform, new Color(0.05f, 0.08f, 0.09f, 0.5f));
            SetRect(content.rectTransform, new Vector2(130, -150), new Vector2(1340, 650), new Vector2(0, 1));
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(290, 156);
            grid.spacing = new Vector2(34, 42);
            grid.padding = new RectOffset(28, 28, 28, 28);
            view.slotContainer = content.transform;
            view.slotPrefab = slotPrefab;
            view.hintText = CreateText("HintText", window.transform, "", 24, TextAnchor.MiddleLeft, Color.white);
            SetRect(view.hintText.rectTransform, new Vector2(50, -805), new Vector2(1200, 46), new Vector2(0, 1));
            return view;
        }

        private static SettingsView BuildSettings(Transform parent)
        {
            var root = CreateOverlayWindow("SettingsView", parent, "Setting");
            var view = root.gameObject.AddComponent<SettingsView>();
            view.textSpeedSlider = CreateSlider(root.transform, "TextSpeedSlider", new Vector2(460, -230), 5, 120, 45, out view.textSpeedValueText, "文字速度");
            view.autoDelaySlider = CreateSlider(root.transform, "AutoDelaySlider", new Vector2(460, -330), 0.2f, 4, 1.2f, out view.autoDelayValueText, "Auto 等待");
            view.masterVolumeSlider = CreateSlider(root.transform, "MasterVolumeSlider", new Vector2(460, -430), 0, 1, 1, out view.masterVolumeValueText, "全部音量");
            view.skipUnreadToggle = CreateToggle(root.transform, "SkipUnreadToggle", "Skip 未讀", new Vector2(460, -530));
            view.applyButton = CreateButton(root.transform, "ApplyButton", "套用並儲存", new Vector2(460, -640), new Vector2(260, 58), new Vector2(0, 1));
            view.closeButton = root.transform.Find("Window/CloseButton").GetComponent<Button>();
            return view;
        }

        private static BacklogView BuildBacklog(Transform parent)
        {
            var root = CreateOverlayWindow("BacklogView", parent, "Log");
            var view = root.gameObject.AddComponent<BacklogView>();
            view.contentText = CreateText("ContentText", root.transform, "", 26, TextAnchor.UpperLeft, Color.white);
            SetRect(view.contentText.rectTransform, new Vector2(280, -190), new Vector2(1360, 620), new Vector2(0, 1));
            view.closeButton = root.transform.Find("Window/CloseButton").GetComponent<Button>();
            return view;
        }

        private static CreditsView BuildCredits(Transform parent)
        {
            var root = CreateOverlayWindow("CreditsView", parent, "製作名單");
            var view = root.gameObject.AddComponent<CreditsView>();
            view.contentText = CreateText("ContentText", root.transform, "", 32, TextAnchor.UpperLeft, Color.white);
            SetRect(view.contentText.rectTransform, new Vector2(340, -240), new Vector2(1000, 360), new Vector2(0, 1));
            view.closeButton = root.transform.Find("Window/CloseButton").GetComponent<Button>();
            return view;
        }

        private static RectTransform CreateOverlayWindow(string name, Transform parent, string title)
        {
            var root = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0.45f));
            Stretch(root.rectTransform);
            var window = CreatePanel("Window", root.transform, new Color(0.08f, 0.12f, 0.12f, 0.94f));
            SetRect(window.rectTransform, new Vector2(160, -100), new Vector2(1600, 880), new Vector2(0, 1));
            var titleText = CreateText("TitleText", window.transform, title, 48, TextAnchor.MiddleLeft, Color.white);
            SetRect(titleText.rectTransform, new Vector2(50, -30), new Vector2(700, 70), new Vector2(0, 1));
            CreateButton(window.transform, "CloseButton", "X", new Vector2(-50, -30), new Vector2(64, 64), new Vector2(1, 1));
            return root.rectTransform;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static void CreateCamera()
        {
            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            cameraGo.tag = "MainCamera";
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static GameObject CreateRectObject(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            return go;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, TextAnchor align, Color color)
        {
            var go = CreateRectObject(name, parent, Vector2.zero);
            var uiText = go.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.text = text;
            uiText.fontSize = size;
            uiText.alignment = align;
            uiText.color = color;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Truncate;
            return uiText;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            var panel = CreatePanel(name, parent, new Color(0.78f, 0.9f, 0.58f, 0.95f));
            SetRect(panel.rectTransform, pos, size, anchor);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            var text = CreateText("Label", panel.transform, label, 22, TextAnchor.MiddleCenter, new Color(0.18f, 0.25f, 0.16f));
            Stretch(text.rectTransform);
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 pos, float min, float max, float value, out Text valueText, string label)
        {
            var labelText = CreateText(label + "Label", parent, label, 26, TextAnchor.MiddleLeft, Color.white);
            SetRect(labelText.rectTransform, new Vector2(pos.x, pos.y), new Vector2(260, 50), new Vector2(0, 1));
            valueText = CreateText(label + "Value", parent, value.ToString("0.##"), 24, TextAnchor.MiddleLeft, Color.white);
            SetRect(valueText.rectTransform, new Vector2(pos.x + 880, pos.y), new Vector2(120, 50), new Vector2(0, 1));

            var go = CreateRectObject(name, parent, new Vector2(520, 42));
            SetRect(go.GetComponent<RectTransform>(), new Vector2(pos.x + 300, pos.y), new Vector2(520, 42), new Vector2(0, 1));
            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            var bg = CreatePanel("Background", go.transform, new Color(0.2f, 0.25f, 0.25f, 1f));
            Stretch(bg.rectTransform);
            var fill = CreatePanel("Fill", go.transform, new Color(1f, 0.58f, 0.72f, 1f));
            Stretch(fill.rectTransform);
            var handle = CreatePanel("Handle", go.transform, Color.white);
            SetRect(handle.rectTransform, Vector2.zero, new Vector2(24, 48), new Vector2(0.5f, 0.5f));
            slider.targetGraphic = bg;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos)
        {
            var go = CreateRectObject(name, parent, new Vector2(360, 54));
            SetRect(go.GetComponent<RectTransform>(), pos, new Vector2(360, 54), new Vector2(0, 1));
            var toggle = go.AddComponent<Toggle>();
            var bg = CreatePanel("CheckmarkBackground", go.transform, new Color(0.2f, 0.25f, 0.25f, 1f));
            SetRect(bg.rectTransform, new Vector2(0, 0), new Vector2(42, 42), new Vector2(0, 0.5f));
            var mark = CreatePanel("Checkmark", bg.transform, new Color(1f, 0.58f, 0.72f, 1f));
            Stretch(mark.rectTransform);
            var text = CreateText("Label", go.transform, label, 26, TextAnchor.MiddleLeft, Color.white);
            SetRect(text.rectTransform, new Vector2(60, 0), new Vector2(260, 54), new Vector2(0, 0.5f));
            toggle.targetGraphic = bg;
            toggle.graphic = mark;
            return toggle;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 pos, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }
    }
}
