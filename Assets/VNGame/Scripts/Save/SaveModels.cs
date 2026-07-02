using System;

namespace VNGame
{
    [Serializable]
    public sealed class SaveData
    {
        public int slotId;
        public string savedAtIso;
        public string displayTime;
        public string screenshotFileName;
        public GameState gameState = new GameState();
    }

    [Serializable]
    public sealed class QuickSaveIndex
    {
        public int nextSlot = 1;
        public int latestSlot;
    }
}
