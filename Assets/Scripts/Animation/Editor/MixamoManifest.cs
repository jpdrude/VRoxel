using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// The committed record of which Mixamo animation each clip is, so others can download the same set.
// Mixamo data itself is gitignored; this file holds only names, links and download settings.
public static class MixamoManifest
{
    public const string ManifestPath = "Assets/Animation/Mixamo/MixamoManifest.csv";
    public const string ChecklistPath = "Assets/Animation/Mixamo/ClipChecklist.md";
    public const string ClipFolder = "Assets/Animation/Mixamo/Clips/";
    // Unity ignores folders ending in "~", so raw downloads are not imported with the wrong settings
    public const string InboxFolder = "Assets/Animation/Mixamo/Inbox~/";

    const string Header = "name,mixamo_title,mixamo_link,character,fps,skin,in_place,notes";

    public class Entry
    {
        public string name;
        public string mixamoTitle;
        public string character = "X Bot";
        public string fps = "30";
        public string skin = "Without Skin";
        public string inPlace = "false";
        public string notes = "";

        // Mixamo has no per-animation links; a search for the exact title is the closest
        public string Link { get { return "https://www.mixamo.com/#/?page=1&query=" + Uri.EscapeDataString(mixamoTitle) + "&type=Motion%2CMotionPack"; } }
    }

    public class ChecklistItem
    {
        public string name;
        public string section;
        public bool core;
        public bool done;
        public List<string> searchTerms = new List<string>();
        public string notes;
    }

    public static List<Entry> Load()
    {
        List<Entry> entries = new List<Entry>();
        if (!File.Exists(ManifestPath))
            return entries;

        foreach (string line in File.ReadAllLines(ManifestPath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            List<string> fields = ParseCsvLine(line);
            while (fields.Count < 8) fields.Add("");

            entries.Add(new Entry
            {
                name = fields[0],
                mixamoTitle = fields[1],
                character = fields[3],
                fps = fields[4],
                skin = fields[5],
                inPlace = fields[6],
                notes = fields[7]
            });
        }

        return entries;
    }

    // Adds or replaces the entry with the same name and keeps the file sorted by name
    public static void Save(Entry entry)
    {
        List<Entry> entries = Load().Where(e => e.name != entry.name).ToList();
        entries.Add(entry);

        StringBuilder csv = new StringBuilder(Header + "\n");
        foreach (Entry e in entries.OrderBy(e => e.name, StringComparer.Ordinal))
            csv.AppendLine(string.Join(",", new[] { e.name, e.mixamoTitle, e.Link, e.character, e.fps, e.skin, e.inPlace, e.notes }.Select(Quote)));

        File.WriteAllText(ManifestPath, csv.ToString().Replace("\r\n", "\n"));
    }

    // Lines like: - [ ] ★ `idle_neutral` | Search "Idle" / "Breathing Idle" | calm baseline pose
    static readonly Regex itemPattern = new Regex(@"^- \[( |x)\] (★ )?`(\w+)`\s*\|([^|]*)\|?(.*)$");

    public static List<ChecklistItem> LoadChecklist()
    {
        List<ChecklistItem> items = new List<ChecklistItem>();
        if (!File.Exists(ChecklistPath))
            return items;

        string section = "";
        foreach (string line in File.ReadAllLines(ChecklistPath))
        {
            if (line.StartsWith("## "))
                section = line.Substring(3).Trim();

            Match match = itemPattern.Match(line.Trim());
            if (!match.Success) continue;

            ChecklistItem item = new ChecklistItem
            {
                done = match.Groups[1].Value == "x",
                core = match.Groups[2].Success,
                name = match.Groups[3].Value,
                section = section,
                notes = match.Groups[5].Value.Trim()
            };

            foreach (Match term in Regex.Matches(match.Groups[4].Value, "\"([^\"]+)\""))
                item.searchTerms.Add(term.Groups[1].Value);

            items.Add(item);
        }

        return items;
    }

    public static void SetChecklistDone(string name, bool done)
    {
        if (!File.Exists(ChecklistPath)) return;

        string[] lines = File.ReadAllLines(ChecklistPath);
        string marker = "`" + name + "`";
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains(marker) || !itemPattern.IsMatch(lines[i].Trim())) continue;

            lines[i] = done ? lines[i].Replace("- [ ]", "- [x]") : lines[i].Replace("- [x]", "- [ ]");
        }

        File.WriteAllText(ChecklistPath, string.Join("\n", lines) + "\n");
    }

    // Mixamo saves downloads under the animation title; browsers add " (1)" for repeated downloads
    public static string TitleFromFileName(string path)
    {
        string title = Path.GetFileNameWithoutExtension(path);
        return Regex.Replace(title, @"\s*\(\d+\)$", "").Trim();
    }

    // Lowercase letters and digits only, so "Standing Idle" matches "standing-idle"
    public static string Normalize(string text)
    {
        return Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]", "");
    }

    static string Quote(string field)
    {
        field = field ?? "";
        if (field.IndexOfAny(new[] { ',', '"', '\n' }) < 0)
            return field;

        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }

    static List<string> ParseCsvLine(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder field = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { fields.Add(field.ToString()); field.Clear(); }
            else field.Append(c);
        }

        fields.Add(field.ToString());
        return fields;
    }
}
