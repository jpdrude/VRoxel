using UnityEngine;

// Analytic two-bone IK on transforms: moves a leg so its foot reaches a target, keeping the knee in its
// current bending plane and the foot's world rotation. Works on CharacterModel's rescaled bones, as it only rotates.
public static class LegIK
{
    public static void Solve(Transform upperLeg, Transform lowerLeg, Transform foot, Vector3 target)
    {
        Quaternion footRotation = foot.rotation;

        Vector3 hip = upperLeg.position;
        Vector3 knee = lowerLeg.position;
        Vector3 ankle = foot.position;

        float upperLength = Vector3.Distance(hip, knee);
        float lowerLength = Vector3.Distance(knee, ankle);
        if (upperLength < 1e-4f || lowerLength < 1e-4f) return;

        Vector3 toTarget = target - hip;
        float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upperLength - lowerLength) + 1e-3f, upperLength + lowerLength - 1e-3f);
        Vector3 direction = toTarget.normalized;

        // Bend toward where the knee points now, so the knee keeps its direction
        Vector3 bend = (knee - hip) - Vector3.Dot(knee - hip, direction) * direction;
        if (bend.sqrMagnitude < 1e-8f)
            bend = Vector3.ProjectOnPlane(upperLeg.parent != null ? upperLeg.parent.forward : Vector3.forward, direction);
        bend.Normalize();

        // Law of cosines: angle at the hip between the hip-target line and the thigh
        float cosHip = (upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2 * upperLength * distance);
        float hipAngle = Mathf.Acos(Mathf.Clamp(cosHip, -1, 1));
        Vector3 newKnee = hip + upperLength * (Mathf.Cos(hipAngle) * direction + Mathf.Sin(hipAngle) * bend);

        upperLeg.rotation = Quaternion.FromToRotation(knee - hip, newKnee - hip) * upperLeg.rotation;
        lowerLeg.rotation = Quaternion.FromToRotation(foot.position - lowerLeg.position, hip + direction * distance - lowerLeg.position) * lowerLeg.rotation;

        foot.rotation = footRotation;
    }
}
