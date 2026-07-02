using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VNGame
{
    public enum GameMode
    {
        MainMenu,
        Novel,
        Map,
        Task
    }

    public enum OverlayMode
    {
        None,
        Load,
        Save,
        Settings,
        Backlog,
        Credits
    }

    [Serializable]
    public sealed class GameSettings
    {
        public float textSpeed = 45f;
        public float autoDelay = 1.2f;
        public bool skipUnread = false;
        public float masterVolume = 1f;
        public float bgmVolume = 0.8f;
        public float sfxVolume = 0.8f;
    }

    [Serializable]
    public sealed class NamedInt
    {
        public string key;
        public int value;
    }

    [Serializable]
    public sealed class NamedString
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class CharacterState
    {
        public string id;
        public int affection;
        public int routeProgress;
    }

    [Serializable]
    public sealed class GameState
    {
        public GameMode currentMode = GameMode.Novel;
        public string scenarioId = "prologue";
        public string lineId = "prologue_001";
        public List<string> readLineIds = new List<string>();
        public List<NamedInt> variables = new List<NamedInt>();
        public List<CharacterState> characters = new List<CharacterState>();
        public List<NamedString> questStates = new List<NamedString>();
        public List<NamedString> inventory = new List<NamedString>();
        public int money;

        public bool HasRead(string id)
        {
            return readLineIds.Contains(id);
        }

        public void MarkRead(string id)
        {
            if (!string.IsNullOrEmpty(id) && !readLineIds.Contains(id))
            {
                readLineIds.Add(id);
            }
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int slotId;
        public string savedAtIso;
        public string displayTime;
        public string screenshotFileName;
        public GameState gameState = new GameState();
    }

    [Serializable]
    public sealed class QuickSaveIndex
    {
        public int nextSlot = 1;
        public int latestSlot = 0;
    }

    [Serializable]
    public sealed class DialogueLine
    {
        public string id;
        public string speaker;
        public string text;
        public string bg;
        public string leftChar;
        public string centerChar;
        public string rightChar;
        public string bgm;
        public string sfx;
        public string command;
        public string next;
    }

    public sealed class Scenario
    {
        public string id;
        public readonly List<DialogueLine> lines = new List<DialogueLine>();
        public readonly Dictionary<string, DialogueLine> byId = new Dictionary<string, DialogueLine>();

        public DialogueLine GetLine(string lineId)
        {
            if (!string.IsNullOrEmpty(lineId) && byId.TryGetValue(lineId, out var line))
            {
                return line;
            }

            return lines.Count > 0 ? lines[0] : null;
        }

        public DialogueLine GetNext(DialogueLine current)
        {
            if (current == null)
            {
                return lines.Count > 0 ? lines[0] : null;
            }

            if (!string.IsNullOrWhiteSpace(current.next))
            {
                return GetLine(current.next);
            }

            var index = lines.IndexOf(current);
            var nextIndex = index + 1;
            return nextIndex >= 0 && nextIndex < lines.Count ? lines[nextIndex] : null;
        }
    }

    public static class VNPaths
    {
        public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "VNGame", "Saves");
        public static string SettingsPath => Path.Combine(Application.persistentDataPath, "VNGame", "settings.json");
        public static string QuickIndexPath => Path.Combine(SaveDirectory, "quick_index.json");

        public static string GetScenarioPath(string scenarioId)
        {
            return Path.Combine(Application.streamingAssetsPath, "VNGame", "Scenarios", scenarioId + ".csv");
        }

        public static string GetSaveJsonPath(int slotId)
        {
            return Path.Combine(SaveDirectory, $"slot_{slotId:000}.json");
        }

        public static string GetSaveScreenshotPath(int slotId)
        {
            return Path.Combine(SaveDirectory, $"slot_{slotId:000}.png");
        }
    }

    public static class ScenarioCsvLoader
    {
        public static Scenario Load(string scenarioId)
        {
            var path = VNPaths.GetScenarioPath(scenarioId);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Scenario not found: {path}");
                return CreateFallbackScenario(scenarioId);
            }

            var text = File.ReadAllText(path, Encoding.UTF8);
            return Parse(scenarioId, text);
        }

        private static Scenario Parse(string scenarioId, string csvText)
        {
            var scenario = new Scenario { id = scenarioId };
            var rows = ParseCsv(csvText);
            if (rows.Count <= 1)
            {
                return CreateFallbackScenario(scenarioId);
            }

            var headers = rows[0];
            var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                index[headers[i].Trim()] = i;
            }

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var line = new DialogueLine
                {
                    id = Get(row, index, "id"),
                    speaker = Get(row, index, "speaker"),
                    text = Get(row, index, "text"),
                    bg = Get(row, index, "bg"),
                    leftChar = Get(row, index, "leftChar"),
                    centerChar = Get(row, index, "centerChar"),
                    rightChar = Get(row, index, "rightChar"),
                    bgm = Get(row, index, "bgm"),
                    sfx = Get(row, index, "sfx"),
                    command = Get(row, index, "command"),
                    next = Get(row, index, "next")
                };

                if (string.IsNullOrWhiteSpace(line.id))
                {
                    line.id = $"{scenarioId}_{i:000}";
                }

                scenario.lines.Add(line);
                scenario.byId[line.id] = line;
            }

            return scenario.lines.Count > 0 ? scenario : CreateFallbackScenario(scenarioId);
        }

        private static string Get(List<string> row, Dictionary<string, int> index, string key)
        {
            if (!index.TryGetValue(key, out var i) || i < 0 || i >= row.Count)
            {
                return string.Empty;
            }

            return row[i];
        }

        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        row.Add(cell.ToString());
                        cell.Length = 0;
                    }
                    else if (c == '\n')
                    {
                        row.Add(cell.ToString().TrimEnd('\r'));
                        rows.Add(row);
                        row = new List<string>();
                        cell.Length = 0;
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }

        private static Scenario CreateFallbackScenario(string scenarioId)
        {
            var scenario = new Scenario { id = scenarioId };
            var line = new DialogueLine
            {
                id = "fallback_001",
                speaker = "System",
                text = "找不到劇本 CSV。請確認 StreamingAssets/VNGame/Scenarios/prologue.csv 是否存在。",
                bg = "fallback"
            };
            scenario.lines.Add(line);
            scenario.byId[line.id] = line;
            return scenario;
        }
    }

    public static class SaveSystem
    {
        public const int NormalSlotCount = 12;
        public const int QuickSlotCount = 12;
        private const int QuickSlotOffset = 1000;

        public static int ToQuickSlotId(int quickSlot)
        {
            return QuickSlotOffset + Mathf.Clamp(quickSlot, 1, QuickSlotCount);
        }

        public static bool IsQuickSlotId(int slotId)
        {
            return slotId > QuickSlotOffset;
        }

        public static int FromQuickSlotId(int slotId)
        {
            return Mathf.Clamp(slotId - QuickSlotOffset, 1, QuickSlotCount);
        }

        public static bool HasSave(int slotId)
        {
            return File.Exists(VNPaths.GetSaveJsonPath(slotId));
        }

        public static SaveData Load(int slotId)
        {
            var path = VNPaths.GetSaveJsonPath(slotId);
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonUtility.FromJson<SaveData>(json);
        }

        public static QuickSaveIndex LoadQuickIndex()
        {
            if (!File.Exists(VNPaths.QuickIndexPath))
            {
                return new QuickSaveIndex();
            }

            try
            {
                var index = JsonUtility.FromJson<QuickSaveIndex>(File.ReadAllText(VNPaths.QuickIndexPath, Encoding.UTF8));
                index.nextSlot = Mathf.Clamp(index.nextSlot, 1, QuickSlotCount);
                index.latestSlot = Mathf.Clamp(index.latestSlot, 0, QuickSlotCount);
                return index;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load quick save index: {ex.Message}");
                return new QuickSaveIndex();
            }
        }

        public static void SaveQuickIndex(QuickSaveIndex index)
        {
            Directory.CreateDirectory(VNPaths.SaveDirectory);
            File.WriteAllText(VNPaths.QuickIndexPath, JsonUtility.ToJson(index, true), Encoding.UTF8);
        }

        public static void SaveMetadata(int slotId, GameState state)
        {
            Directory.CreateDirectory(VNPaths.SaveDirectory);
            var now = DateTime.Now;
            var data = new SaveData
            {
                slotId = slotId,
                savedAtIso = now.ToString("O", CultureInfo.InvariantCulture),
                displayTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
                screenshotFileName = Path.GetFileName(VNPaths.GetSaveScreenshotPath(slotId)),
                gameState = state
            };

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(VNPaths.GetSaveJsonPath(slotId), json, Encoding.UTF8);
        }

        public static void SaveSettings(GameSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(VNPaths.SettingsPath));
            File.WriteAllText(VNPaths.SettingsPath, JsonUtility.ToJson(settings, true), Encoding.UTF8);
        }

        public static GameSettings LoadSettings()
        {
            if (!File.Exists(VNPaths.SettingsPath))
            {
                return new GameSettings();
            }

            try
            {
                return JsonUtility.FromJson<GameSettings>(File.ReadAllText(VNPaths.SettingsPath, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load settings: {ex.Message}");
                return new GameSettings();
            }
        }
    }

    public sealed class VNGameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateOnLoad()
        {
            if (FindFirstObjectByType<VNGameApp>() != null)
            {
                return;
            }

            var go = new GameObject("VNGameApp");
            go.AddComponent<VNGameApp>();
        }
    }

    public sealed class VNGameApp : MonoBehaviour
    {
        private GameSettings settings;
        private GameState state;
        private Scenario scenario;
        private DialogueLine currentLine;
        private OverlayMode overlay = OverlayMode.None;
        private GameMode mode = GameMode.MainMenu;

        private Canvas canvas;
        private GameObject mainMenuRoot;
        private GameObject novelRoot;
        private GameObject overlayRoot;
        private Image backgroundImage;
        private Text speakerText;
        private Text dialogueText;
        private Text centerCharacterText;
        private Text statusText;
        private Text modeTitleText;
        private readonly List<Text> backlogTexts = new List<Text>();
        private readonly List<string> backlog = new List<string>();

        private Coroutine typingRoutine;
        private string visibleFullText = string.Empty;
        private bool isTyping;
        private bool autoMode;
        private bool skipMode;
        private float autoTimer;

        private static readonly Color PanelColor = new Color(0.08f, 0.1f, 0.14f, 0.86f);
        private static readonly Color ButtonColor = new Color(0.78f, 0.9f, 0.58f, 0.95f);
        private static readonly Color AccentColor = new Color(1f, 0.58f, 0.72f, 1f);

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            settings = SaveSystem.LoadSettings();
            state = new GameState();
            EnsureEventSystem();
            BuildUi();
            ShowMainMenu();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || WasPressed(keyboard?.escapeKey))
            {
                Back();
                return;
            }

            if (mode == GameMode.Novel && overlay == OverlayMode.None)
            {
                if ((mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi()) || WasPressed(keyboard?.spaceKey) || WasPressed(keyboard?.enterKey))
                {
                    AdvanceNovel();
                }

                if (WasPressed(keyboard?.aKey))
                {
                    ToggleAuto();
                }

                if (WasPressed(keyboard?.sKey))
                {
                    ToggleSkip();
                }

                if (autoMode || skipMode)
                {
                    autoTimer += Time.deltaTime;
                    var delay = skipMode ? 0.08f : settings.autoDelay;
                    if (autoTimer >= delay && !isTyping)
                    {
                        autoTimer = 0f;
                        AdvanceNovel();
                    }
                }
            }
        }

        private static bool WasPressed(ButtonControl button)
        {
            return button != null && button.wasPressedThisFrame;
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                var target = result.gameObject;
                if (target.GetComponentInParent<Button>() != null ||
                    target.GetComponentInParent<Slider>() != null ||
                    target.GetComponentInParent<Scrollbar>() != null ||
                    target.GetComponentInParent<Toggle>() != null ||
                    target.GetComponentInParent<Dropdown>() != null ||
                    target.GetComponentInParent<InputField>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void BuildUi()
        {
            canvas = CreateCanvas();
            mainMenuRoot = CreatePanel(canvas.transform, "MainMenu", StretchFull());
            novelRoot = CreatePanel(canvas.transform, "Novel", StretchFull());
            overlayRoot = CreatePanel(canvas.transform, "Overlay", StretchFull());

            BuildMainMenu();
            BuildNovel();
            overlayRoot.SetActive(false);
        }

        private Canvas CreateCanvas()
        {
            var go = new GameObject("VNGameCanvas");
            DontDestroyOnLoad(go);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        private void BuildMainMenu()
        {
            var title = CreateText(mainMenuRoot.transform, "VNGame", 78, TextAnchor.MiddleLeft, new Color(0.98f, 0.98f, 0.95f));
            SetRect(title.rectTransform, new Vector2(100, -120), new Vector2(780, 120), new Vector2(0, 1), new Vector2(0, 1));

            var subtitle = CreateText(mainMenuRoot.transform, "Visual Novel Prototype", 28, TextAnchor.MiddleLeft, new Color(0.92f, 0.95f, 0.9f));
            SetRect(subtitle.rectTransform, new Vector2(106, -205), new Vector2(780, 60), new Vector2(0, 1), new Vector2(0, 1));

            var buttons = new (string, Action)[]
            {
                ("開始遊戲", StartNewGame),
                ("讀取進度", () => ShowSaveLoad(false, false)),
                ("遊戲設定", ShowSettings),
                ("製作名單", ShowCredits),
                ("離開遊戲", QuitGame)
            };

            for (var i = 0; i < buttons.Length; i++)
            {
                var button = CreateButton(mainMenuRoot.transform, buttons[i].Item1, buttons[i].Item2);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(120, -330 - i * 86), new Vector2(390, 62), new Vector2(0, 1), new Vector2(0, 1));
            }

            var note = CreateText(mainMenuRoot.transform, "第一版以功能為主：UI 美術之後可替換。右鍵/Esc 可返回或關閉視窗。", 24, TextAnchor.LowerLeft, new Color(0.92f, 0.95f, 0.9f));
            SetRect(note.rectTransform, new Vector2(90, 45), new Vector2(1100, 80), new Vector2(0, 0), new Vector2(0, 0));
        }

        private void BuildNovel()
        {
            backgroundImage = novelRoot.GetComponent<Image>();
            backgroundImage.color = new Color(0.22f, 0.27f, 0.34f);

            modeTitleText = CreateText(novelRoot.transform, "Novel Mode", 28, TextAnchor.MiddleLeft, Color.white);
            SetRect(modeTitleText.rectTransform, new Vector2(28, -22), new Vector2(500, 48), new Vector2(0, 1), new Vector2(0, 1));

            centerCharacterText = CreateText(novelRoot.transform, "Character", 58, TextAnchor.MiddleCenter, Color.white);
            SetRect(centerCharacterText.rectTransform, new Vector2(0, 80), new Vector2(760, 680), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            centerCharacterText.color = new Color(1f, 1f, 1f, 0.9f);

            var dialoguePanel = CreatePanel(novelRoot.transform, "DialoguePanel", new RectSpec(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 126), new Vector2(1500, 210)));
            dialoguePanel.GetComponent<Image>().color = PanelColor;

            speakerText = CreateText(dialoguePanel.transform, "", 32, TextAnchor.MiddleLeft, new Color(0.72f, 0.86f, 1f));
            SetRect(speakerText.rectTransform, new Vector2(34, -18), new Vector2(500, 54), new Vector2(0, 1), new Vector2(0, 1));

            dialogueText = CreateText(dialoguePanel.transform, "", 34, TextAnchor.UpperLeft, Color.white);
            SetRect(dialogueText.rectTransform, new Vector2(38, -76), new Vector2(1420, 116), new Vector2(0, 1), new Vector2(0, 1));

            statusText = CreateText(novelRoot.transform, "", 22, TextAnchor.MiddleLeft, new Color(0.9f, 0.95f, 1f));
            SetRect(statusText.rectTransform, new Vector2(155, 72), new Vector2(700, 40), new Vector2(0, 0), new Vector2(0, 0));

            var bottomButtons = new (string, Action)[]
            {
                ("Log", ShowBacklog),
                ("Q.Save", () => StartCoroutine(QuickSave())),
                ("Q.Load", QuickLoadLatest),
                ("Save", () => ShowSaveLoad(true, false)),
                ("Load", () => ShowSaveLoad(false, false)),
                ("設定", ShowSettings),
                ("主選單", ConfirmMainMenu)
            };

            for (var i = 0; i < bottomButtons.Length; i++)
            {
                var button = CreateButton(novelRoot.transform, bottomButtons[i].Item1, bottomButtons[i].Item2, 20);
                SetRect(button.GetComponent<RectTransform>(), new Vector2(915 + i * 132, 42), new Vector2(118, 44), new Vector2(0, 0), new Vector2(0, 0));
            }

            var autoButton = CreateButton(novelRoot.transform, "Auto", ToggleAuto, 20);
            SetRect(autoButton.GetComponent<RectTransform>(), new Vector2(36, 42), new Vector2(92, 44), new Vector2(0, 0), new Vector2(0, 0));

            var skipButton = CreateButton(novelRoot.transform, "Skip", ToggleSkip, 20);
            SetRect(skipButton.GetComponent<RectTransform>(), new Vector2(138, 42), new Vector2(92, 44), new Vector2(0, 0), new Vector2(0, 0));
        }

        private void StartNewGame()
        {
            state = new GameState { currentMode = GameMode.Novel, scenarioId = "prologue", lineId = "prologue_001" };
            StartNovel(state);
        }

        private void StartNovel(GameState loadedState)
        {
            state = loadedState ?? new GameState();
            mode = GameMode.Novel;
            state.currentMode = GameMode.Novel;
            scenario = ScenarioCsvLoader.Load(state.scenarioId);
            currentLine = scenario.GetLine(state.lineId);
            mainMenuRoot.SetActive(false);
            novelRoot.SetActive(true);
            CloseOverlay();
            ShowLine(currentLine);
        }

        private void ShowMainMenu()
        {
            mode = GameMode.MainMenu;
            mainMenuRoot.SetActive(true);
            novelRoot.SetActive(false);
            CloseOverlay();
        }

        private void ShowLine(DialogueLine line)
        {
            if (line == null)
            {
                visibleFullText = "劇本已結束。";
                speakerText.text = "System";
                dialogueText.text = visibleFullText;
                centerCharacterText.text = "";
                return;
            }

            currentLine = line;
            state.lineId = line.id;
            visibleFullText = line.text ?? string.Empty;
            speakerText.text = line.speaker ?? string.Empty;
            centerCharacterText.text = string.IsNullOrWhiteSpace(line.centerChar) ? "立繪\nPlaceholder" : line.centerChar;
            backgroundImage.color = ColorFromKey(line.bg);
            modeTitleText.text = $"Scenario: {state.scenarioId} / {line.id}";

            var logLine = string.IsNullOrWhiteSpace(line.speaker) ? visibleFullText : $"{line.speaker}: {visibleFullText}";
            if (!string.IsNullOrWhiteSpace(logLine))
            {
                backlog.Add(logLine);
            }

            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
            }

            typingRoutine = StartCoroutine(TypeText(visibleFullText));
            state.MarkRead(line.id);
        }

        private IEnumerator TypeText(string text)
        {
            isTyping = true;
            dialogueText.text = string.Empty;
            if (settings.textSpeed <= 0f)
            {
                dialogueText.text = text;
                isTyping = false;
                yield break;
            }

            var delay = 1f / settings.textSpeed;
            for (var i = 0; i < text.Length; i++)
            {
                dialogueText.text = text.Substring(0, i + 1);
                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
        }

        private void AdvanceNovel()
        {
            if (overlay != OverlayMode.None)
            {
                return;
            }

            if (isTyping)
            {
                CompleteTyping();
                return;
            }

            if (skipMode && currentLine != null && !settings.skipUnread && !state.HasRead(currentLine.id))
            {
                skipMode = false;
                UpdateStatus();
                return;
            }

            var next = scenario.GetNext(currentLine);
            if (next == null)
            {
                autoMode = false;
                skipMode = false;
                speakerText.text = "System";
                dialogueText.text = "目前範例劇本已播放完畢。";
                centerCharacterText.text = "";
                UpdateStatus();
                return;
            }

            ShowLine(next);
            autoTimer = 0f;
            UpdateStatus();
        }

        private void CompleteTyping()
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
            }

            dialogueText.text = visibleFullText;
            isTyping = false;
        }

        private void ToggleAuto()
        {
            autoMode = !autoMode;
            if (autoMode)
            {
                skipMode = false;
            }

            autoTimer = 0f;
            UpdateStatus();
        }

        private void ToggleSkip()
        {
            skipMode = !skipMode;
            if (skipMode)
            {
                autoMode = false;
                CompleteTyping();
            }

            autoTimer = 0f;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            statusText.text = $"Auto: {(autoMode ? "ON" : "OFF")}    Skip: {(skipMode ? "ON" : "OFF")}    Right Click/Esc: Back";
        }

        private void ShowSaveLoad(bool saveMode, bool quickPage)
        {
            overlay = saveMode ? OverlayMode.Save : OverlayMode.Load;
            overlayRoot.SetActive(true);
            ClearOverlay();
            var window = CreateOverlayWindow(saveMode ? "Save" : "Load");

            if (!saveMode)
            {
                var loadTab = CreateButton(window.transform, "Load", () => ShowSaveLoad(false, false), 24);
                SetRect(loadTab.GetComponent<RectTransform>(), new Vector2(260, -88), new Vector2(180, 54), new Vector2(0, 1), new Vector2(0, 1));
                loadTab.GetComponent<Image>().color = quickPage ? ButtonColor : AccentColor;

                var qLoadTab = CreateButton(window.transform, "QLoad", () => ShowSaveLoad(false, true), 24);
                SetRect(qLoadTab.GetComponent<RectTransform>(), new Vector2(460, -88), new Vector2(180, 54), new Vector2(0, 1), new Vector2(0, 1));
                qLoadTab.GetComponent<Image>().color = quickPage ? AccentColor : ButtonColor;
            }

            var content = CreatePanel(window.transform, "SaveLoadContent", new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -36), new Vector2(1340, 650)));
            content.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.09f, 0.5f);

            var count = quickPage ? SaveSystem.QuickSlotCount : SaveSystem.NormalSlotCount;
            for (var i = 0; i < count; i++)
            {
                var displaySlot = i + 1;
                var slotId = quickPage ? SaveSystem.ToQuickSlotId(displaySlot) : displaySlot;
                var x = 28 + (i % 4) * 324;
                var y = -28 - (i / 4) * 198;
                CreateSaveSlot(content.transform, slotId, displaySlot, new Vector2(x, y), saveMode && !quickPage, quickPage);
            }

            if (saveMode)
            {
                var hint = CreateText(window.transform, "選擇一格儲存目前進度。Q.Save 會在遊戲中自動輪替快速存檔。", 24, TextAnchor.MiddleLeft, Color.white);
                SetRect(hint.rectTransform, new Vector2(150, -805), new Vector2(1000, 46), new Vector2(0, 1), new Vector2(0, 1));
            }
        }

        private void CreateSaveSlot(Transform parent, int slotId, int displaySlot, Vector2 anchoredPosition, bool canSave, bool quickSlot)
        {
            var panel = CreatePanel(parent, $"Slot{slotId:000}", new RectSpec(new Vector2(0, 1), new Vector2(0, 1), anchoredPosition, new Vector2(290, 156)));
            panel.GetComponent<Image>().color = new Color(0.86f, 0.93f, 0.65f, 0.92f);

            var prefix = quickSlot ? "Q" : "Slot ";
            var title = CreateText(panel.transform, $"{prefix}{displaySlot:000}", 22, TextAnchor.MiddleLeft, new Color(0.18f, 0.25f, 0.16f));
            SetRect(title.rectTransform, new Vector2(14, -10), new Vector2(120, 30), new Vector2(0, 1), new Vector2(0, 1));

            var data = SaveSystem.Load(slotId);
            var timeText = data != null ? data.displayTime : "Empty";
            var time = CreateText(panel.transform, timeText, 18, TextAnchor.LowerCenter, new Color(0.18f, 0.25f, 0.16f));
            SetRect(time.rectTransform, new Vector2(0, 8), new Vector2(266, 30), new Vector2(0.5f, 0), new Vector2(0.5f, 0));

            var thumbnail = CreatePanel(panel.transform, "Thumbnail", new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(240, 78)));
            thumbnail.GetComponent<Image>().color = new Color(0.16f, 0.2f, 0.24f, 0.92f);
            TryApplyThumbnail(thumbnail.GetComponent<Image>(), slotId);

            var button = panel.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                if (canSave)
                {
                    StartCoroutine(SaveToSlot(slotId, () => ShowSaveLoad(true, false)));
                }
                else
                {
                    LoadFromSlot(slotId);
                }
            });
        }

        private IEnumerator QuickSave()
        {
            var index = SaveSystem.LoadQuickIndex();
            var quickSlot = Mathf.Clamp(index.nextSlot, 1, SaveSystem.QuickSlotCount);
            var slotId = SaveSystem.ToQuickSlotId(quickSlot);
            yield return SaveToSlot(slotId, null);
            index.latestSlot = quickSlot;
            index.nextSlot = quickSlot >= SaveSystem.QuickSlotCount ? 1 : quickSlot + 1;
            SaveSystem.SaveQuickIndex(index);
        }

        private void QuickLoadLatest()
        {
            var index = SaveSystem.LoadQuickIndex();
            if (index.latestSlot <= 0)
            {
                return;
            }

            LoadFromSlot(SaveSystem.ToQuickSlotId(index.latestSlot));
        }

        private IEnumerator SaveToSlot(int slotId, Action afterSaved)
        {
            if (mode != GameMode.Novel)
            {
                yield break;
            }

            var restoreOverlay = overlayRoot.activeSelf;
            if (restoreOverlay)
            {
                overlayRoot.SetActive(false);
            }

            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(VNPaths.SaveDirectory);
            var screenshotPath = VNPaths.GetSaveScreenshotPath(slotId);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(screenshotPath, texture.EncodeToPNG());
            Destroy(texture);

            SaveSystem.SaveMetadata(slotId, CloneState(state));
            if (restoreOverlay)
            {
                overlayRoot.SetActive(true);
            }

            afterSaved?.Invoke();
        }

        private void LoadFromSlot(int slotId)
        {
            var data = SaveSystem.Load(slotId);
            if (data == null)
            {
                return;
            }

            StartNovel(data.gameState);
        }

        private static GameState CloneState(GameState source)
        {
            return JsonUtility.FromJson<GameState>(JsonUtility.ToJson(source));
        }

        private void TryApplyThumbnail(Image image, int slotId)
        {
            var path = VNPaths.GetSaveScreenshotPath(slotId);
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);
                if (!tex.LoadImage(bytes))
                {
                    Destroy(tex);
                    return;
                }

                image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                image.preserveAspect = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load thumbnail: {ex.Message}");
            }
        }

        private void ShowBacklog()
        {
            overlay = OverlayMode.Backlog;
            overlayRoot.SetActive(true);
            ClearOverlay();
            CreateOverlayWindow("Log");
            backlogTexts.Clear();

            var start = Mathf.Max(0, backlog.Count - 12);
            for (var i = start; i < backlog.Count; i++)
            {
                var text = CreateText(overlayRoot.transform, backlog[i], 25, TextAnchor.MiddleLeft, Color.white);
                SetRect(text.rectTransform, new Vector2(250, -180 - (i - start) * 58), new Vector2(1420, 52), new Vector2(0, 1), new Vector2(0, 1));
                backlogTexts.Add(text);
            }
        }

        private void ShowSettings()
        {
            overlay = OverlayMode.Settings;
            overlayRoot.SetActive(true);
            ClearOverlay();
            CreateOverlayWindow("Setting");

            CreateSettingSlider("文字速度", settings.textSpeed, 5f, 120f, new Vector2(420, -230), value => settings.textSpeed = value);
            CreateSettingSlider("Auto 等待", settings.autoDelay, 0.2f, 4f, new Vector2(420, -330), value => settings.autoDelay = value);
            CreateSettingSlider("全部音量", settings.masterVolume, 0f, 1f, new Vector2(420, -430), value =>
            {
                settings.masterVolume = value;
                AudioListener.volume = value;
            });

            var skipButton = CreateButton(overlayRoot.transform, $"Skip 未讀: {(settings.skipUnread ? "允許" : "只略過已讀")}", () =>
            {
                settings.skipUnread = !settings.skipUnread;
                ShowSettings();
            }, 24);
            SetRect(skipButton.GetComponent<RectTransform>(), new Vector2(420, -540), new Vector2(380, 58), new Vector2(0, 1), new Vector2(0, 1));

            var saveButton = CreateButton(overlayRoot.transform, "套用並儲存", () =>
            {
                SaveSystem.SaveSettings(settings);
                CloseOverlay();
            }, 24);
            SetRect(saveButton.GetComponent<RectTransform>(), new Vector2(420, -640), new Vector2(250, 58), new Vector2(0, 1), new Vector2(0, 1));
        }

        private void CreateSettingSlider(string label, float value, float min, float max, Vector2 pos, Action<float> onChanged)
        {
            var text = CreateText(overlayRoot.transform, $"{label}: {value:0.##}", 26, TextAnchor.MiddleLeft, Color.white);
            SetRect(text.rectTransform, pos, new Vector2(300, 50), new Vector2(0, 1), new Vector2(0, 1));

            var sliderGo = new GameObject(label + "Slider");
            sliderGo.transform.SetParent(overlayRoot.transform, false);
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(pos.x + 330, pos.y), new Vector2(520, 42), new Vector2(0, 1), new Vector2(0, 1));

            var bg = CreatePanel(sliderGo.transform, "Background", StretchFull());
            bg.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.25f, 1f);
            slider.targetGraphic = bg.GetComponent<Image>();

            var fill = CreatePanel(sliderGo.transform, "Fill", StretchFull());
            fill.GetComponent<Image>().color = AccentColor;
            slider.fillRect = fill.GetComponent<RectTransform>();

            var handle = CreatePanel(sliderGo.transform, "Handle", new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 48)));
            handle.GetComponent<Image>().color = Color.white;
            slider.handleRect = handle.GetComponent<RectTransform>();

            slider.onValueChanged.AddListener(v =>
            {
                text.text = $"{label}: {v:0.##}";
                onChanged(v);
            });
        }

        private void ShowCredits()
        {
            overlay = OverlayMode.Credits;
            overlayRoot.SetActive(true);
            ClearOverlay();
            CreateOverlayWindow("製作名單");
            var credits = CreateText(overlayRoot.transform, "VNGame Prototype\n\nGame Design / Programming: You + Codex\n\n這裡之後可以替換成正式 credits UI。", 32, TextAnchor.UpperLeft, Color.white);
            SetRect(credits.rectTransform, new Vector2(340, -240), new Vector2(1000, 360), new Vector2(0, 1), new Vector2(0, 1));
        }

        private void ConfirmMainMenu()
        {
            ShowMainMenu();
        }

        private void Back()
        {
            if (overlay != OverlayMode.None)
            {
                CloseOverlay();
                return;
            }

            if (mode == GameMode.Novel)
            {
                ShowMainMenu();
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CloseOverlay()
        {
            overlay = OverlayMode.None;
            overlayRoot.SetActive(false);
            ClearOverlay();
        }

        private void ClearOverlay()
        {
            for (var i = overlayRoot.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(overlayRoot.transform.GetChild(i).gameObject);
            }
        }

        private GameObject CreateOverlayWindow(string title)
        {
            var dim = overlayRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);

            var panel = CreatePanel(overlayRoot.transform, "Window", new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 880)));
            panel.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.12f, 0.94f);

            var titleText = CreateText(overlayRoot.transform, title, 48, TextAnchor.MiddleLeft, Color.white);
            SetRect(titleText.rectTransform, new Vector2(210, -94), new Vector2(700, 70), new Vector2(0, 1), new Vector2(0, 1));

            var close = CreateButton(overlayRoot.transform, "X", CloseOverlay, 30);
            SetRect(close.GetComponent<RectTransform>(), new Vector2(-210, -94), new Vector2(64, 64), new Vector2(1, 1), new Vector2(1, 1));
            return panel;
        }

        private GameObject CreatePanel(Transform parent, string name, RectSpec spec)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            SetRect(go.GetComponent<RectTransform>(), spec.anchoredPosition, spec.size, spec.anchorMin, spec.anchorMax);
            return go;
        }

        private Text CreateText(Transform parent, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var uiText = go.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = color;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Truncate;
            return uiText;
        }

        private Button CreateButton(Transform parent, string label, Action onClick, int fontSize = 28)
        {
            var go = CreatePanel(parent, "Button_" + label, new RectSpec(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 70)));
            var image = go.GetComponent<Image>();
            image.color = ButtonColor;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            var text = CreateText(go.transform, label, fontSize, TextAnchor.MiddleCenter, new Color(0.18f, 0.25f, 0.16f));
            SetRect(text.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
            return button;
        }

        private static RectSpec StretchFull()
        {
            return new RectSpec(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;

            if (anchorMin == Vector2.zero && anchorMax == Vector2.one && size == Vector2.zero)
            {
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static Color ColorFromKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return new Color(0.22f, 0.27f, 0.34f);
            }

            var hash = key.GetHashCode();
            var r = 0.22f + Mathf.Abs(hash % 100) / 220f;
            var g = 0.25f + Mathf.Abs((hash / 101) % 100) / 240f;
            var b = 0.32f + Mathf.Abs((hash / 10001) % 100) / 240f;
            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b));
        }

        private readonly struct RectSpec
        {
            public readonly Vector2 anchorMin;
            public readonly Vector2 anchorMax;
            public readonly Vector2 anchoredPosition;
            public readonly Vector2 size;

            public RectSpec(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.anchoredPosition = anchoredPosition;
                this.size = size;
            }
        }
    }
}


