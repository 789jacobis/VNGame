using System;
using UnityEngine;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class MainMenuView : MonoBehaviour
    {
        public Button startButton;
        public Button loadButton;
        public Button settingsButton;
        public Button creditsButton;
        public Button quitButton;

        public event Action StartRequested;
        public event Action LoadRequested;
        public event Action SettingsRequested;
        public event Action CreditsRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            Bind(startButton, () => StartRequested?.Invoke());
            Bind(loadButton, () => LoadRequested?.Invoke());
            Bind(settingsButton, () => SettingsRequested?.Invoke());
            Bind(creditsButton, () => CreditsRequested?.Invoke());
            Bind(quitButton, () => QuitRequested?.Invoke());
        }

        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
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
