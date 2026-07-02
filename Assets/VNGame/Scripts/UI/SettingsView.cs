using System;
using UnityEngine;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class SettingsView : MonoBehaviour
    {
        public Slider textSpeedSlider;
        public Text textSpeedValueText;
        public Slider autoDelaySlider;
        public Text autoDelayValueText;
        public Slider masterVolumeSlider;
        public Text masterVolumeValueText;
        public Toggle skipUnreadToggle;
        public Button applyButton;
        public Button closeButton;

        public event Action<GameSettings> ApplyRequested;
        public event Action CloseRequested;

        private GameSettings editing;

        private void Awake()
        {
            if (textSpeedSlider != null)
            {
                textSpeedSlider.onValueChanged.AddListener(value =>
                {
                    if (editing != null) editing.textSpeed = value;
                    RefreshLabels();
                });
            }

            if (autoDelaySlider != null)
            {
                autoDelaySlider.onValueChanged.AddListener(value =>
                {
                    if (editing != null) editing.autoDelay = value;
                    RefreshLabels();
                });
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(value =>
                {
                    if (editing != null) editing.masterVolume = value;
                    RefreshLabels();
                });
            }

            if (skipUnreadToggle != null)
            {
                skipUnreadToggle.onValueChanged.AddListener(value =>
                {
                    if (editing != null) editing.skipUnread = value;
                });
            }

            if (applyButton != null)
            {
                applyButton.onClick.AddListener(() => ApplyRequested?.Invoke(editing));
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            }
        }

        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Edit(GameSettings settings)
        {
            editing = Clone(settings);
            if (textSpeedSlider != null) textSpeedSlider.value = editing.textSpeed;
            if (autoDelaySlider != null) autoDelaySlider.value = editing.autoDelay;
            if (masterVolumeSlider != null) masterVolumeSlider.value = editing.masterVolume;
            if (skipUnreadToggle != null) skipUnreadToggle.isOn = editing.skipUnread;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (editing == null)
            {
                return;
            }

            if (textSpeedValueText != null) textSpeedValueText.text = editing.textSpeed.ToString("0.##");
            if (autoDelayValueText != null) autoDelayValueText.text = editing.autoDelay.ToString("0.##");
            if (masterVolumeValueText != null) masterVolumeValueText.text = editing.masterVolume.ToString("0.##");
        }

        private static GameSettings Clone(GameSettings source)
        {
            return new GameSettings
            {
                textSpeed = source.textSpeed,
                autoDelay = source.autoDelay,
                skipUnread = source.skipUnread,
                masterVolume = source.masterVolume,
                bgmVolume = source.bgmVolume,
                sfxVolume = source.sfxVolume
            };
        }
    }
}
