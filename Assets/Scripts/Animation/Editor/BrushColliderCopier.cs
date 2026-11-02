using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Gives the Mixamo avatar the brush colliders of the motion capture avatar, so CharacterSpawner can add or remove blocks
// with its body. The rigs' bone axes differ (xybot vs stock Mixamo), so each collider is measured in world space with both
// prefabs in their rest pose (T-pose), stretched by the ratio of the bones' lengths, and added on a child of the matching
// bone that lines up with it. Rerunning replaces the copies.
public static class BrushColliderCopier
{
    const string SourcePath = "Assets/Geometry/testing/JPD_Animation/AvatarJPD.prefab";
    const string TargetPath = AnimationLibraryBuilder.AvatarPath;
    const string Prefix = "Brush_";

    [MenuItem("VRoxel/Copy Brush Colliders To Mixamo Avatar")]
    static void Copy()
    {
        GameObject source = PrefabUtility.LoadPrefabContents(SourcePath);
        GameObject target = PrefabUtility.LoadPrefabContents(TargetPath);
        try
        {
            Dictionary<string, Transform> targetBones = target.GetComponentsInChildren<Transform>(true)
                .GroupBy(bone => bone.name).ToDictionary(group => group.Key, group => group.First());

            foreach (Transform old in targetBones.Values.Where(bone => bone.name.StartsWith(Prefix)).ToList())
                Object.DestroyImmediate(old.gameObject);

            List<string> copied = new List<string>();
            List<string> missing = new List<string>();

            foreach (Collider collider in source.GetComponentsInChildren<Collider>(true))
            {
                Transform sourceBone = collider.transform;
                Transform targetBone;
                if (!targetBones.TryGetValue(sourceBone.name, out targetBone))
                {
                    missing.Add(sourceBone.name);
                    continue;
                }

                float stretch = BoneLength(targetBone) / Mathf.Max(BoneLength(sourceBone), 1e-4f);
                if (BoneLength(sourceBone) < 1e-4f || BoneLength(targetBone) < 1e-4f)
                    stretch = 1;

                GameObject brush = new GameObject(Prefix + targetBone.name.Replace("mixamorig:", ""));
                brush.layer = collider.gameObject.layer;
                brush.transform.SetParent(targetBone, false);

                if (collider is CapsuleCollider capsule)
                {
                    Vector3 axis = capsule.direction == 0 ? Vector3.right : capsule.direction == 1 ? Vector3.up : Vector3.forward;
                    Vector3 a = sourceBone.TransformPoint(capsule.center - axis * (capsule.height / 2 - capsule.radius));
                    Vector3 b = sourceBone.TransformPoint(capsule.center + axis * (capsule.height / 2 - capsule.radius));
                    Vector3 newA = targetBone.position + (a - sourceBone.position) * stretch;
                    Vector3 newB = targetBone.position + (b - sourceBone.position) * stretch;

                    // The child's Y axis runs along the capsule, so it keeps its exact direction on any rig
                    brush.transform.position = (newA + newB) / 2;
                    brush.transform.rotation = Quaternion.FromToRotation(Vector3.up, (newB - newA).sqrMagnitude > 1e-8f ? newB - newA : Vector3.up);

                    // Sizes go through world space, as the rigs' bones may be scaled differently (FBX unit scale)
                    float radius = capsule.radius * MaxScale(sourceBone) / MaxScale(brush.transform);
                    CapsuleCollider copy = brush.AddComponent<CapsuleCollider>();
                    copy.direction = 1;
                    copy.radius = radius;
                    copy.height = Vector3.Distance(newA, newB) / Mathf.Abs(brush.transform.lossyScale.y) + 2 * radius;
                    copy.isTrigger = capsule.isTrigger;
                }
                else if (collider is SphereCollider sphere)
                {
                    brush.transform.position = targetBone.position + (sourceBone.TransformPoint(sphere.center) - sourceBone.position) * stretch;

                    SphereCollider copy = brush.AddComponent<SphereCollider>();
                    copy.radius = sphere.radius * MaxScale(sourceBone) / MaxScale(brush.transform);
                    copy.isTrigger = sphere.isTrigger;
                }
                else
                {
                    Object.DestroyImmediate(brush);
                    continue;
                }

                copied.Add(brush.name);
            }

            PrefabUtility.SaveAsPrefabAsset(target, TargetPath);
            Debug.Log("BrushColliderCopier: " + copied.Count + " brush colliders copied to " + TargetPath + ": " + string.Join(", ", copied) +
                (missing.Count > 0 ? "\nNo bone of this name on the Mixamo avatar: " + string.Join(", ", missing) : ""));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
            PrefabUtility.UnloadPrefabContents(target);
        }
    }

    static float MaxScale(Transform transform)
    {
        Vector3 scale = transform.lossyScale;
        return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z), 1e-6f);
    }

    // Distance to the bone's first child bone; 0 for end bones
    static float BoneLength(Transform bone)
    {
        foreach (Transform child in bone)
            if (child.name.StartsWith("mixamorig:"))
                return Vector3.Distance(bone.position, child.position);
        return 0;
    }
}
