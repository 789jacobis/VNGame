using System;
using System.Collections.Generic;

namespace VNGame
{
    public enum GameMode
    {
        MainMenu,
        Novel,
        Map,
        Task
    }

    public enum OverlayMode
    {
        None,
        Load,
        Save,
        Settings,
        Backlog,
        Credits
    }

    [Serializable]
    public sealed class GameSettings
    {
        public float textSpeed = 45f;
        public float autoDelay = 1.2f;
        public bool skipUnread;
        public float masterVolume = 1f;
        public float bgmVolume = 0.8f;
        public float sfxVolume = 0.8f;
    }

    [Serializable]
    public sealed class NamedInt
    {
        public string key;
        public int value;
    }

    [Serializable]
    public sealed class NamedString
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class CharacterState
    {
        public string id;
        public int affection;
        public int routeProgress;
    }

    [Serializable]
    public sealed class GameState
    {
        public GameMode currentMode = GameMode.Novel;
        public string scenarioId = "prologue";
        public string lineId = "prologue_001";
        public List<string> readLineIds = new List<string>();
        public List<NamedInt> variables = new List<NamedInt>();
        public List<CharacterState> characters = new List<CharacterState>();
        public List<NamedString> questStates = new List<NamedString>();
        public List<NamedString> inventory = new List<NamedString>();
        public int money;

        public bool HasRead(string id)
        {
            return readLineIds.Contains(id);
        }

        public void MarkRead(string id)
        {
            if (!string.IsNullOrEmpty(id) && !readLineIds.Contains(id))
            {
                readLineIds.Add(id);
            }
        }
    }
}
