using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace VNGame
{
    public static class SaveSystem
    {
        public const int NormalSlotCount = 12;
        public const int QuickSlotCount = 12;
        private const int QuickSlotOffset = 1000;

        public static int ToQuickSlotId(int quickSlot)
        {
            return QuickSlotOffset + Mathf.Clamp(quickSlot, 1, QuickSlotCount);
        }

        public static int FromQuickSlotId(int slotId)
        {
            return Mathf.Clamp(slotId - QuickSlotOffset, 1, QuickSlotCount);
        }

        public static SaveData Load(int slotId)
        {
            var path = VNPaths.GetSaveJsonPath(slotId);
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path, Encoding.UTF8));
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

            File.WriteAllText(VNPaths.GetSaveJsonPath(slotId), JsonUtility.ToJson(data, true), Encoding.UTF8);
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
}
