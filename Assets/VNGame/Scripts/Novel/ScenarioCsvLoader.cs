using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace VNGame
{
    public static class ScenarioCsvLoader
    {
        public static Scenario Load(string scenarioId)
        {
            var path = VNPaths.GetScenarioPath(scenarioId);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Scenario not found: {path}");
                return CreateFallbackScenario(scenarioId);
            }

            var text = File.ReadAllText(path, Encoding.UTF8);
            return Parse(scenarioId, text);
        }

        private static Scenario Parse(string scenarioId, string csvText)
        {
            var scenario = new Scenario { id = scenarioId };
            var rows = ParseCsv(csvText);
            if (rows.Count <= 1)
            {
                return CreateFallbackScenario(scenarioId);
            }

            var headers = rows[0];
            var index = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                index[headers[i].Trim()] = i;
            }

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var line = new DialogueLine
                {
                    id = Get(row, index, "id"),
                    speaker = Get(row, index, "speaker"),
                    text = Get(row, index, "text"),
                    bg = Get(row, index, "bg"),
                    leftChar = Get(row, index, "leftChar"),
                    centerChar = Get(row, index, "centerChar"),
                    rightChar = Get(row, index, "rightChar"),
                    bgm = Get(row, index, "bgm"),
                    sfx = Get(row, index, "sfx"),
                    command = Get(row, index, "command"),
                    next = Get(row, index, "next")
                };

                if (string.IsNullOrWhiteSpace(line.id))
                {
                    line.id = $"{scenarioId}_{i:000}";
                }

                scenario.lines.Add(line);
                scenario.byId[line.id] = line;
            }

            return scenario.lines.Count > 0 ? scenario : CreateFallbackScenario(scenarioId);
        }

        private static string Get(List<string> row, Dictionary<string, int> index, string key)
        {
            return index.TryGetValue(key, out var i) && i >= 0 && i < row.Count ? row[i] : string.Empty;
        }

        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(cell.ToString());
                    cell.Length = 0;
                }
                else if (c == '\n')
                {
                    row.Add(cell.ToString().TrimEnd('\r'));
                    rows.Add(row);
                    row = new List<string>();
                    cell.Length = 0;
                }
                else
                {
                    cell.Append(c);
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }

        private static Scenario CreateFallbackScenario(string scenarioId)
        {
            var scenario = new Scenario { id = scenarioId };
            var line = new DialogueLine
            {
                id = "fallback_001",
                speaker = "System",
                text = "找不到劇本 CSV。請確認 StreamingAssets/VNGame/Scenarios/prologue.csv 是否存在。",
                bg = "fallback"
            };
            scenario.lines.Add(line);
            scenario.byId[line.id] = line;
            return scenario;
        }
    }
}
