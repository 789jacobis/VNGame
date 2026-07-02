using System;
using UnityEngine;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class CreditsView : MonoBehaviour
    {
        public Text contentText;
        public Button closeButton;

        public event Action CloseRequested;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            }
        }

        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetContent(string content)
        {
            if (contentText != null)
            {
                contentText.text = content;
            }
        }
    }
}
