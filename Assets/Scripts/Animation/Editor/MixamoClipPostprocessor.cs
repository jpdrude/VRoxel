using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Configures every FBX in the Mixamo clip folder from its file name: category_actionName[_loop|_once].fbx
// Settings are applied on every import, so the file name (not the Inspector) is the source of truth.
public class MixamoClipPostprocessor : AssetPostprocessor
{
    const string ClipFolder = "Assets/Animation/Mixamo/Clips/";
    // Mixamo X Bot downloaded with skin in T-pose. Clips without skin have no bind pose, so an Avatar built from
    // their own skeleton takes the first animation frame as T-pose and distorts the retargeted motion.
    const string ReferencePath = "Assets/Animation/Mixamo/Reference/xbot_tpose.fbx";

    // Categories whose clips loop by default
    static readonly string[] loopingCategories = { "idle", "loco", "gesture", "activity" };
    // Categories whose clips stay on the spot, so horizontal root motion is baked into the pose
    static readonly string[] stationaryCategories = { "idle", "gesture" };
    // Categories whose clips turn the character, so root rotation must stay root motion
    static readonly string[] turningCategories = { "turn" };
    // Categories whose looping clips travel; loops elsewhere (sitting, leaning, working) stay on the spot
    static readonly string[] movingCategories = { "loco", "turn", "vert" };

    // One-shot transitions inside otherwise looping categories, e.g. loco_startWalking, sit_standToSit
    static readonly Regex transitionPattern = new Regex(@"(start|stop|(?-i:To[A-Z])|getUp|lieDown)", RegexOptions.IgnoreCase);
    // Travelling loops outside the moving categories, e.g. reach_carryBoxWalk_loop, reach_push_loop
    static readonly Regex movingPattern = new Regex(@"(walk|run|jog|push|pull)", RegexOptions.IgnoreCase);

    public override uint GetVersion()
    {
        // Bump when the rules below change so Unity reimports the clips
        return 5;
    }

    void OnPreprocessModel()
    {
        if (assetPath == ReferencePath)
        {
            var referenceImporter = (ModelImporter)assetImporter;
            referenceImporter.animationType = ModelImporterAnimationType.Human;
            referenceImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            referenceImporter.importAnimation = false;
            return;
        }

        if (!IsMixamoClip()) return;

        var importer = (ModelImporter)assetImporter;

        // Clips share the stock Mixamo rig of the reference. Copying the xybot Avatar misreads the clips,
        // because xybot's bone orientations differ from the stock Mixamo rig. Retargeting onto xybot happens at runtime.
        importer.animationType = ModelImporterAnimationType.Human;
        importer.importAnimation = true;

        // Reimports the clips once the reference has been imported (or changes)
        context.DependsOnArtifact(ReferencePath);
        Avatar reference = AssetDatabase.LoadAllAssetsAtPath(ReferencePath).OfType<Avatar>().FirstOrDefault();
        if (reference != null)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = reference;
        }
        else
        {
            Debug.LogWarning("MixamoClipPostprocessor: no reference Avatar at " + ReferencePath + ", " + Path.GetFileName(assetPath) + " uses its first frame as T-pose");
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
        }

        // Clips are downloaded without skin; skip everything that isn't animation
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
    }

    void OnPreprocessAnimation()
    {
        if (!IsMixamoClip()) return;

        var importer = (ModelImporter)assetImporter;
        ClipRules rules = GetRules(Path.GetFileNameWithoutExtension(assetPath));

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            // Mixamo names every take "mixamo.com"
            clip.name = Path.GetFileNameWithoutExtension(assetPath);

            clip.loopTime = rules.loop;
            clip.loopPose = false;

            clip.lockRootRotation = !rules.turning;
            clip.keepOriginalOrientation = true;

            clip.lockRootHeightY = true;
            clip.keepOriginalPositionY = false;
            clip.heightFromFeet = true;

            clip.lockRootPositionXZ = rules.stationary;
            clip.keepOriginalPositionXZ = true;
        }

        importer.clipAnimations = clips;
    }

    bool IsMixamoClip()
    {
        return assetPath.StartsWith(ClipFolder) && assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
    }

    struct ClipRules
    {
        public bool loop;
        public bool stationary;
        public bool turning;
    }

    static ClipRules GetRules(string fileName)
    {
        string[] parts = fileName.Split('_');
        string category = parts[0].ToLowerInvariant();
        string action = parts.Length > 1 ? parts[1] : "";
        string suffix = parts.Length > 2 ? parts[parts.Length - 1].ToLowerInvariant() : "";

        ClipRules rules = new ClipRules();

        bool isIdle = action.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0;

        rules.loop = (loopingCategories.Contains(category) || isIdle) && !transitionPattern.IsMatch(action);
        if (suffix == "loop") rules.loop = true;
        else if (suffix == "once") rules.loop = false;

        rules.stationary = stationaryCategories.Contains(category) || isIdle
            || (rules.loop && !movingCategories.Contains(category) && !movingPattern.IsMatch(action));
        rules.turning = turningCategories.Contains(category);

        return rules;
    }
}
