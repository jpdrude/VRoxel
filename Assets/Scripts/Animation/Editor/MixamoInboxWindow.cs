using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Files raw Mixamo downloads under their checklist name and records the Mixamo title in the manifest.
// Download into Inbox~ and keep Mixamo's file name (the animation title); assign each file here.
public class MixamoInboxWindow : EditorWindow
{
    class InboxFile
    {
        public string path;
        public string title;
        public int choice;
        public string notes = "";
    }

    List<InboxFile> inbox = new List<InboxFile>();
    List<MixamoManifest.ChecklistItem> checklist = new List<MixamoManifest.ChecklistItem>();
    Dictionary<string, string> recordedTitles = new Dictionary<string, string>();
    HashSet<string> recorded = new HashSet<string>();
    HashSet<string> local = new HashSet<string>();
    string[] choiceLabels = new string[0];
    Vector2 scroll;
    bool showMissing = true;

    [MenuItem("VRoxel/Mixamo Inbox")]
    static void Open()
    {
        GetWindow<MixamoInboxWindow>("Mixamo Inbox").Reload();
    }

    void OnFocus()
    {
        Reload();
    }

    void Reload()
    {
        Directory.CreateDirectory(MixamoManifest.InboxFolder);

        checklist = MixamoManifest.LoadChecklist();
        recordedTitles = MixamoManifest.Load().GroupBy(e => e.name).ToDictionary(group => group.Key, group => group.First().mixamoTitle);
        recorded = new HashSet<string>(recordedTitles.Keys);
        local = new HashSet<string>(Directory.Exists(MixamoManifest.ClipFolder)
            ? Directory.GetFiles(MixamoManifest.ClipFolder, "*.fbx").Select(Path.GetFileNameWithoutExtension)
            : Enumerable.Empty<string>());

        choiceLabels = checklist.Select(item => item.name + (recorded.Contains(item.name) ? "   (replace)" : "")).ToArray();

        // Keep choices for files that are still in the inbox
        Dictionary<string, InboxFile> previous = inbox.ToDictionary(file => file.path);
        inbox = Directory.GetFiles(MixamoManifest.InboxFolder, "*.fbx")
            .OrderBy(path => path)
            .Select(path => previous.ContainsKey(path) ? previous[path] : new InboxFile { path = path, title = MixamoManifest.TitleFromFileName(path), choice = Guess(path) })
            .ToList();

        Repaint();
    }

    // A title recorded in the manifest wins (reproducing someone else's set); otherwise prefers
    // clips not recorded yet whose search terms match the title
    int Guess(string path)
    {
        string title = MixamoManifest.Normalize(MixamoManifest.TitleFromFileName(path));
        int best = -1;
        int bestScore = 0;

        for (int i = 0; i < checklist.Count; i++)
        {
            string recordedTitle;
            if (recordedTitles.TryGetValue(checklist[i].name, out recordedTitle) && MixamoManifest.Normalize(recordedTitle) == title && !local.Contains(checklist[i].name))
                return i;

            foreach (string term in checklist[i].searchTerms)
            {
                string normalized = MixamoManifest.Normalize(term);
                int score = normalized == title ? 3 : (normalized.Length > 3 && (title.Contains(normalized) || normalized.Contains(title)) ? 2 : 0);
                if (score > 0 && !recorded.Contains(checklist[i].name))
                    score++;

                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
        }

        return Mathf.Max(best, 0);
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Download from mixamo.com onto X Bot: FBX for Unity, Without Skin, 30 fps, keyframe reduction none, In Place off. " +
            "Save into " + MixamoManifest.InboxFolder + " and keep Mixamo's file name, then assign each file below.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open Inbox Folder"))
                EditorUtility.RevealInFinder(Path.GetFullPath(MixamoManifest.InboxFolder));
            if (GUILayout.Button("Refresh"))
                Reload();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inbox (" + inbox.Count + ")", EditorStyles.boldLabel);
        if (inbox.Count == 0)
            EditorGUILayout.LabelField("No downloads waiting.");

        foreach (InboxFile file in inbox.ToList())
            DrawInboxFile(file);

        EditorGUILayout.Space();
        DrawMissing();

        EditorGUILayout.EndScrollView();
    }

    void DrawInboxFile(InboxFile file)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Mixamo title", file.title, EditorStyles.boldLabel);

            if (checklist.Count == 0)
            {
                EditorGUILayout.HelpBox("No clips found in " + MixamoManifest.ChecklistPath, MessageType.Warning);
                return;
            }

            file.choice = EditorGUILayout.Popup("Clip name", file.choice, choiceLabels);
            MixamoManifest.ChecklistItem item = checklist[file.choice];
            EditorGUILayout.LabelField(" ", item.section + (item.searchTerms.Count > 0 ? "   search: " + string.Join(" / ", item.searchTerms) : ""), EditorStyles.miniLabel);
            file.notes = EditorGUILayout.TextField("Notes", file.notes);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Assign"))
                    Assign(file, item.name);
                if (GUILayout.Button("Discard", GUILayout.Width(80)) &&
                    EditorUtility.DisplayDialog("Discard download", "Delete " + Path.GetFileName(file.path) + " from the inbox?", "Delete", "Cancel"))
                {
                    File.Delete(file.path);
                    Reload();
                }
            }
        }
    }

    void Assign(InboxFile file, string name)
    {
        string target = MixamoManifest.ClipFolder + name + ".fbx";
        if (File.Exists(target) && !EditorUtility.DisplayDialog("Replace clip", name + " already exists. Replace it with \"" + file.title + "\"?", "Replace", "Cancel"))
            return;

        Directory.CreateDirectory(MixamoManifest.ClipFolder);

        // Overwriting keeps the existing .meta, so references to the clip survive the replacement
        File.Copy(file.path, target, true);
        File.Delete(file.path);

        MixamoManifest.Save(new MixamoManifest.Entry { name = name, mixamoTitle = file.title, notes = file.notes });
        MixamoManifest.SetChecklistDone(name, true);

        AssetDatabase.Refresh();
        Debug.Log("Mixamo Inbox: \"" + file.title + "\" saved as " + target);
        Reload();
    }

    void DrawMissing()
    {
        List<MixamoManifest.ChecklistItem> missing = checklist.Where(item => !recorded.Contains(item.name) || !local.Contains(item.name)).ToList();
        int core = missing.Count(item => item.core);

        showMissing = EditorGUILayout.Foldout(showMissing, "Still to download: " + missing.Count + " of " + checklist.Count + " (" + core + " core)", true);
        if (!showMissing) return;

        foreach (MixamoManifest.ChecklistItem item in missing.OrderByDescending(item => item.core))
        {
            // Recorded clips show their exact Mixamo title, the rest the checklist's search terms
            string recordedTitle;
            string search = recordedTitles.TryGetValue(item.name, out recordedTitle)
                ? "\"" + recordedTitle + "\""
                : string.Join(" / ", item.searchTerms);
            string state = local.Contains(item.name) ? "   [local, title not recorded]" : "";

            EditorGUILayout.LabelField((item.core ? "★ " : "   ") + item.name, search + state);
        }
    }
}
