using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class VNGameController : MonoBehaviour
    {
        [Header("Views")]
        public MainMenuView mainMenuView;
        public NovelView novelView;
        public SaveLoadView saveLoadView;
        public SettingsView settingsView;
        public BacklogView backlogView;
        public CreditsView creditsView;
        public SpriteAssetProvider spriteProvider;

        [Header("Scenario")]
        public string initialScenarioId = "prologue";
        public string initialLineId = "prologue_001";

        private GameSettings settings;
        private GameState state;
        private Scenario scenario;
        private DialogueLine currentLine;
        private OverlayMode overlay = OverlayMode.None;
        private GameMode mode = GameMode.MainMenu;
        private Coroutine typingRoutine;
        private string visibleFullText = string.Empty;
        private bool isTyping;
        private bool autoMode;
        private bool skipMode;
        private float autoTimer;
        private readonly List<string> backlog = new List<string>();
        private bool savePageMode;
        private bool quickPageMode;

        private void Awake()
        {
            settings = SaveSystem.LoadSettings();
            state = new GameState();
            BindViews();
            ShowMainMenu();
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            var rightMousePressed = mouse != null && mouse.rightButton.wasPressedThisFrame;
            var leftMousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;

            if (rightMousePressed || WasPressed(keyboard?.escapeKey))
            {
                if (rightMousePressed && mode == GameMode.Novel && overlay == OverlayMode.None && StopAutoOrSkip())
                {
                    return;
                }

                Back();
                return;
            }

            if (mode == GameMode.Novel && overlay == OverlayMode.None)
            {
                if (leftMousePressed && !IsPointerOverInteractiveUi())
                {
                    if (StopAutoOrSkip())
                    {
                        return;
                    }

                    RevealDialogueOrAdvance();
                }
                else if (WasPressed(keyboard?.spaceKey) || WasPressed(keyboard?.enterKey))
                {
                    RevealDialogueOrAdvance();
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

        private void BindViews()
        {
            if (mainMenuView != null)
            {
                mainMenuView.StartRequested += StartNewGame;
                mainMenuView.LoadRequested += () => ShowSaveLoad(false, false);
                mainMenuView.SettingsRequested += ShowSettings;
                mainMenuView.CreditsRequested += ShowCredits;
                mainMenuView.QuitRequested += QuitGame;
            }

            if (novelView != null)
            {
                novelView.LogRequested += ShowBacklog;
                novelView.HideRequested += ToggleDialogueVisibility;
                novelView.QuickSaveRequested += () => StartCoroutine(QuickSave());
                novelView.QuickLoadRequested += QuickLoadLatest;
                novelView.SaveRequested += () => ShowSaveLoad(true, false);
                novelView.LoadRequested += () => ShowSaveLoad(false, false);
                novelView.SettingsRequested += ShowSettings;
                novelView.MainMenuRequested += ShowMainMenu;
                novelView.AutoRequested += ToggleAuto;
                novelView.SkipRequested += ToggleSkip;
            }

            if (saveLoadView != null)
            {
                saveLoadView.LoadTabRequested += () => ShowSaveLoad(false, false);
                saveLoadView.QLoadTabRequested += () => ShowSaveLoad(false, true);
                saveLoadView.CloseRequested += CloseOverlay;
                saveLoadView.SlotSelected += OnSaveSlotSelected;
            }

            if (settingsView != null)
            {
                settingsView.ApplyRequested += ApplySettings;
                settingsView.CloseRequested += CloseOverlay;
            }

            if (backlogView != null)
            {
                backlogView.CloseRequested += CloseOverlay;
            }

            if (creditsView != null)
            {
                creditsView.CloseRequested += CloseOverlay;
            }
        }

        private void StartNewGame()
        {
            state = new GameState
            {
                currentMode = GameMode.Novel,
                scenarioId = initialScenarioId,
                lineId = initialLineId
            };
            StartNovel(state);
        }

        private void StartNovel(GameState loadedState)
        {
            state = loadedState ?? new GameState();
            mode = GameMode.Novel;
            state.currentMode = GameMode.Novel;
            scenario = ScenarioCsvLoader.Load(state.scenarioId);
            currentLine = scenario.GetLine(state.lineId);
            mainMenuView.Show(false);
            novelView.Show(true);
            CloseOverlay();
            ShowLine(currentLine);
        }

        private void ShowMainMenu()
        {
            mode = GameMode.MainMenu;
            mainMenuView.Show(true);
            novelView.Show(false);
            CloseOverlay();
        }

        private void ShowLine(DialogueLine line)
        {
            novelView.SetDialogueVisible(true);

            if (line == null)
            {
                visibleFullText = "劇本已結束。";
                novelView.SetDialogueText(visibleFullText);
                return;
            }

            currentLine = line;
            state.lineId = line.id;
            visibleFullText = line.text ?? string.Empty;
            var background = spriteProvider != null ? spriteProvider.GetBackground(line.bg) : null;
            var left = spriteProvider != null ? spriteProvider.GetCharacter(line.leftChar) : null;
            var center = spriteProvider != null ? spriteProvider.GetCharacter(line.centerChar) : null;
            var right = spriteProvider != null ? spriteProvider.GetCharacter(line.rightChar) : null;
            novelView.SetLine(line, background, left, center, right);

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
            UpdateStatus();
        }

        private IEnumerator TypeText(string text)
        {
            isTyping = true;
            novelView.SetDialogueText(string.Empty);
            if (settings.textSpeed <= 0f)
            {
                novelView.SetDialogueText(text);
                isTyping = false;
                yield break;
            }

            var delay = 1f / settings.textSpeed;
            for (var i = 0; i < text.Length; i++)
            {
                novelView.SetDialogueText(text.Substring(0, i + 1));
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

            var next = scenario.GetNext(currentLine);
            if (next == null)
            {
                autoMode = false;
                skipMode = false;
                novelView.SetDialogueText("目前範例劇本已播放完畢。");
                UpdateStatus();
                return;
            }

            ShowLine(next);
            autoTimer = 0f;
        }

        private void RevealDialogueOrAdvance()
        {
            if (!novelView.IsDialogueVisible())
            {
                novelView.SetDialogueVisible(true);
                return;
            }

            AdvanceNovel();
        }

        private void CompleteTyping()
        {
            if (typingRoutine != null)
            {
                StopCoroutine(typingRoutine);
            }

            novelView.SetDialogueText(visibleFullText);
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

        private bool StopAutoOrSkip()
        {
            if (!autoMode && !skipMode)
            {
                return false;
            }

            autoMode = false;
            skipMode = false;
            autoTimer = 0f;
            UpdateStatus();
            return true;
        }

        private void UpdateStatus()
        {
            novelView.SetStatus($"Auto: {(autoMode ? "ON" : "OFF")}    Skip: {(skipMode ? "ON" : "OFF")}    Right Click/Esc: Close");
        }

        private void ToggleDialogueVisibility()
        {
            if (mode != GameMode.Novel || overlay != OverlayMode.None)
            {
                return;
            }

            novelView.SetDialogueVisible(!novelView.IsDialogueVisible());
        }

        private void ShowSaveLoad(bool saveMode, bool quickPage)
        {
            overlay = saveMode ? OverlayMode.Save : OverlayMode.Load;
            savePageMode = saveMode;
            quickPageMode = quickPage;
            HideOverlays();

            var slots = new List<SaveSlotPresentation>();
            var count = quickPage ? SaveSystem.QuickSlotCount : SaveSystem.NormalSlotCount;
            for (var i = 1; i <= count; i++)
            {
                var slotId = quickPage ? SaveSystem.ToQuickSlotId(i) : i;
                slots.Add(new SaveSlotPresentation
                {
                    slotId = slotId,
                    displayName = quickPage ? $"Q{i:000}" : $"Slot {i:000}",
                    data = SaveSystem.Load(slotId),
                    thumbnail = LoadThumbnail(slotId)
                });
            }

            saveLoadView.Show(true);
            saveLoadView.Populate(saveMode ? "Save" : "Load", saveMode, quickPage, slots);
        }

        private void OnSaveSlotSelected(int slotId)
        {
            if (savePageMode && !quickPageMode)
            {
                StartCoroutine(SaveToSlot(slotId, () => ShowSaveLoad(true, false)));
                return;
            }

            LoadFromSlot(slotId);
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

            var restore = saveLoadView.gameObject.activeSelf;
            if (restore)
            {
                saveLoadView.Show(false);
            }

            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(VNPaths.SaveDirectory);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(VNPaths.GetSaveScreenshotPath(slotId), texture.EncodeToPNG());
            Destroy(texture);

            SaveSystem.SaveMetadata(slotId, CloneState(state));
            if (restore)
            {
                saveLoadView.Show(true);
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

        private Sprite LoadThumbnail(int slotId)
        {
            var path = VNPaths.GetSaveScreenshotPath(slotId);
            if (!File.Exists(path))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2);
            return tex.LoadImage(bytes) ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        }

        private void ShowBacklog()
        {
            overlay = OverlayMode.Backlog;
            HideOverlays();
            backlogView.Show(true);
            backlogView.SetLines(backlog);
        }

        private void ShowSettings()
        {
            overlay = OverlayMode.Settings;
            HideOverlays();
            settingsView.Show(true);
            settingsView.Edit(settings);
        }

        private void ApplySettings(GameSettings newSettings)
        {
            settings = newSettings ?? settings;
            AudioListener.volume = settings.masterVolume;
            SaveSystem.SaveSettings(settings);
            CloseOverlay();
        }

        private void ShowCredits()
        {
            overlay = OverlayMode.Credits;
            HideOverlays();
            creditsView.Show(true);
            creditsView.SetContent("VNGame Prototype\n\nGame Design / Programming: You + Codex\n\n這裡之後可以替換成正式 credits UI。");
        }

        private void Back()
        {
            if (overlay != OverlayMode.None)
            {
                CloseOverlay();
                return;
            }

            if (mode == GameMode.Novel && !novelView.IsDialogueVisible())
            {
                novelView.SetDialogueVisible(true);
            }
        }

        private void CloseOverlay()
        {
            overlay = OverlayMode.None;
            HideOverlays();
        }

        private void HideOverlays()
        {
            saveLoadView.Show(false);
            settingsView.Show(false);
            backlogView.Show(false);
            creditsView.Show(false);
        }

        private static GameState CloneState(GameState source)
        {
            return JsonUtility.FromJson<GameState>(JsonUtility.ToJson(source));
        }

        private static bool WasPressed(ButtonControl button)
        {
            return button != null && button.wasPressedThisFrame;
        }

        private static bool IsPointerOverInteractiveUi()
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

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
