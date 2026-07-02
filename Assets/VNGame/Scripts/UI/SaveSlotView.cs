using System;
using UnityEngine;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class SaveSlotView : MonoBehaviour
    {
        public Image frameImage;
        public Image thumbnailImage;
        public Text slotNameText;
        public Text dateTimeText;
        public Button button;

        private int slotId;
        private Action<int> selected;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(() => selected?.Invoke(slotId));
            }
        }

        public void Setup(int slotId, string displayName, SaveData data, Sprite thumbnail, Action<int> onSelected)
        {
            this.slotId = slotId;
            selected = onSelected;

            if (slotNameText != null)
            {
                slotNameText.text = displayName;
            }

            if (dateTimeText != null)
            {
                dateTimeText.text = data != null ? data.displayTime : "Empty";
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = thumbnail;
                thumbnailImage.enabled = thumbnail != null;
                thumbnailImage.preserveAspect = true;
            }
        }
    }
}
