using System.IO;
using UnityEngine;

namespace VNGame
{
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
}
