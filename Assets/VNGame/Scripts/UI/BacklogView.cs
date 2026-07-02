using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class BacklogView : MonoBehaviour
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

        public void SetLines(IReadOnlyList<string> lines)
        {
            if (contentText == null)
            {
                return;
            }

            var start = Mathf.Max(0, lines.Count - 20);
            contentText.text = string.Join("\n", SubList(lines, start));
        }

        private static IEnumerable<string> SubList(IReadOnlyList<string> lines, int start)
        {
            for (var i = start; i < lines.Count; i++)
            {
                yield return lines[i];
            }
        }
    }
}
