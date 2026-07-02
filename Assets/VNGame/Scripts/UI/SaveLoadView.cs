using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VNGame
{
    public sealed class SaveSlotPresentation
    {
        public int slotId;
        public string displayName;
        public SaveData data;
        public Sprite thumbnail;
    }

    public sealed class SaveLoadView : MonoBehaviour
    {
        public Text titleText;
        public Button loadTabButton;
        public Button qLoadTabButton;
        public Button closeButton;
        public Transform slotContainer;
        public SaveSlotView slotPrefab;
        public Text hintText;

        public event Action LoadTabRequested;
        public event Action QLoadTabRequested;
        public event Action CloseRequested;
        public event Action<int> SlotSelected;

        private readonly List<SaveSlotView> spawnedSlots = new List<SaveSlotView>();

        private void Awake()
        {
            Bind(loadTabButton, () => LoadTabRequested?.Invoke());
            Bind(qLoadTabButton, () => QLoadTabRequested?.Invoke());
            Bind(closeButton, () => CloseRequested?.Invoke());
        }

        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Populate(string title, bool saveMode, bool quickPage, IReadOnlyList<SaveSlotPresentation> slots)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (hintText != null)
            {
                hintText.text = saveMode
                    ? "選擇一格儲存目前進度。Q.Save 會在遊戲中自動輪替快速存檔。"
                    : "選擇一格讀取進度。上方可切換一般 Load / QLoad。";
            }

            if (loadTabButton != null)
            {
                loadTabButton.gameObject.SetActive(!saveMode);
            }

            if (qLoadTabButton != null)
            {
                qLoadTabButton.gameObject.SetActive(!saveMode);
            }

            ClearSlots();
            foreach (var slot in slots)
            {
                var view = Instantiate(slotPrefab, slotContainer);
                view.Setup(slot.slotId, slot.displayName, slot.data, slot.thumbnail, id => SlotSelected?.Invoke(id));
                spawnedSlots.Add(view);
            }
        }

        private void ClearSlots()
        {
            foreach (var slot in spawnedSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            spawnedSlots.Clear();
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
