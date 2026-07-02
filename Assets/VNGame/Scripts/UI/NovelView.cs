using System;
using UnityEngine;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class NovelView : MonoBehaviour
    {
        public Image backgroundImage;
        public Image leftCharacterImage;
        public Image centerCharacterImage;
        public Image rightCharacterImage;
        public Text speakerText;
        public Text dialogueText;
        public Text statusText;
        public Text debugLineText;

        public Button logButton;
        public Button quickSaveButton;
        public Button quickLoadButton;
        public Button saveButton;
        public Button loadButton;
        public Button settingsButton;
        public Button mainMenuButton;
        public Button autoButton;
        public Button skipButton;

        public event Action LogRequested;
        public event Action QuickSaveRequested;
        public event Action QuickLoadRequested;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action SettingsRequested;
        public event Action MainMenuRequested;
        public event Action AutoRequested;
        public event Action SkipRequested;

        private void Awake()
        {
            Bind(logButton, () => LogRequested?.Invoke());
            Bind(quickSaveButton, () => QuickSaveRequested?.Invoke());
            Bind(quickLoadButton, () => QuickLoadRequested?.Invoke());
            Bind(saveButton, () => SaveRequested?.Invoke());
            Bind(loadButton, () => LoadRequested?.Invoke());
            Bind(settingsButton, () => SettingsRequested?.Invoke());
            Bind(mainMenuButton, () => MainMenuRequested?.Invoke());
            Bind(autoButton, () => AutoRequested?.Invoke());
            Bind(skipButton, () => SkipRequested?.Invoke());
        }

        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetLine(DialogueLine line, Sprite background, Sprite left, Sprite center, Sprite right)
        {
            if (speakerText != null)
            {
                speakerText.text = line?.speaker ?? string.Empty;
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = background;
                backgroundImage.enabled = true;
            }

            SetImage(leftCharacterImage, left, !string.IsNullOrWhiteSpace(line?.leftChar));
            SetImage(centerCharacterImage, center, !string.IsNullOrWhiteSpace(line?.centerChar));
            SetImage(rightCharacterImage, right, !string.IsNullOrWhiteSpace(line?.rightChar));

            if (debugLineText != null)
            {
                debugLineText.text = line != null ? line.id : string.Empty;
            }
        }

        public void SetDialogueText(string text)
        {
            if (dialogueText != null)
            {
                dialogueText.text = text;
            }
        }

        public void SetStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
        }

        private static void SetImage(Image image, Sprite sprite, bool visible)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = visible && sprite != null;
        }

        private static void Bind(Button button, Action action)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => action());
            }
        }
    }
}
