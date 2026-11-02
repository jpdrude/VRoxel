using System.Collections.Generic;
using UnityEngine;

// Finds floor heights for avatars with physics raycasts. Blocks have no colliders, so avatars walk (and sculpt)
// through them; only floors and imported geometry on the ground layers count. Avatar colliders are skipped.
public static class GroundProbe
{
    // Default, Floor, Teleport, ImportMeshes; McpBridge overrides this from its inspector setting
    public static LayerMask Layers = (1 << 0) | (1 << 3) | (1 << 6) | (1 << 11);

    const float MaxRayLength = 1000;

    // Avatars follow floors up to StepHeight above their feet (stairs, ramps) and drop at most MaxDrop
    public const float StepHeight = 0.5f;
    public const float MaxDrop = 1.5f;
    // Feet within this distance of a floor count as standing on it (landing after a climb, airborne anchors)
    public const float LandingTolerance = 0.05f;

    // Highest floor below `from`, or null if there is none within maxDistance
    public static float? FindBelow(Vector3 from, float maxDistance)
    {
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, maxDistance, Layers, QueryTriggerInteraction.Ignore);

        float? best = null;
        foreach (RaycastHit hit in hits)
            if (!IsAvatar(hit.collider) && (best == null || hit.point.y > best))
                best = hit.point.y;

        return best;
    }

    // All floor heights at (x, z), top to bottom, e.g. one per storey
    public static List<float> FindAll(float x, float z)
    {
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, MaxRayLength / 2, z), Vector3.down, MaxRayLength, Layers, QueryTriggerInteraction.Ignore);

        List<float> heights = new List<float>();
        foreach (RaycastHit hit in hits)
            if (!IsAvatar(hit.collider))
                heights.Add(hit.point.y);

        heights.Sort((a, b) => b.CompareTo(a));
        return heights;
    }

    // Floor an avatar at `position` stands on: up to StepHeight above its feet or MaxDrop below
    public static float? FindStandingHeight(Vector3 position)
    {
        return FindBelow(position + Vector3.up * StepHeight, StepHeight + MaxDrop);
    }

    public static bool IsOnFloor(Vector3 position)
    {
        float? ground = FindBelow(position + Vector3.up * LandingTolerance, LandingTolerance * 2);
        return ground.HasValue;
    }

    // An airborne avatar lands on a floor up to StepHeight above its feet, as a grounded one steps up onto it:
    // Mixamo's descents go a little further down than its ascents go up (4 stair cycles: 1.92 m up, 2.04 m down)
    public static bool HasLanded(Vector3 position)
    {
        return FindBelow(position + Vector3.up * StepHeight, StepHeight + LandingTolerance).HasValue;
    }

    static bool IsAvatar(Collider collider)
    {
        return collider.GetComponentInParent<CharacterModel>() != null;
    }
}
