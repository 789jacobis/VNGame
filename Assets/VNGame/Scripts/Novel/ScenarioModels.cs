using System;
using System.Collections.Generic;

namespace VNGame
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string id;
        public string speaker;
        public string text;
        public string bg;
        public string leftChar;
        public string centerChar;
        public string rightChar;
        public string bgm;
        public string sfx;
        public string command;
        public string next;
    }

    public sealed class Scenario
    {
        public string id;
        public readonly List<DialogueLine> lines = new List<DialogueLine>();
        public readonly Dictionary<string, DialogueLine> byId = new Dictionary<string, DialogueLine>();

        public DialogueLine GetLine(string lineId)
        {
            if (!string.IsNullOrEmpty(lineId) && byId.TryGetValue(lineId, out var line))
            {
                return line;
            }

            return lines.Count > 0 ? lines[0] : null;
        }

        public DialogueLine GetNext(DialogueLine current)
        {
            if (current == null)
            {
                return lines.Count > 0 ? lines[0] : null;
            }

            if (!string.IsNullOrWhiteSpace(current.next))
            {
                return GetLine(current.next);
            }

            var index = lines.IndexOf(current);
            var nextIndex = index + 1;
            return nextIndex >= 0 && nextIndex < lines.Count ? lines[nextIndex] : null;
        }
    }
}
