using UnityEditor;
using UnityEngine;

// Buttons for StretchableFrame: read the pieces' sizes from their bounding boxes, and lay the frame out around content
// of any size in the editor (each step can be undone)
[CustomEditor(typeof(StretchableFrame))]
public class StretchableFrameEditor : Editor
{
    Vector2 contentSize = new Vector2(float.NaN, float.NaN);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        StretchableFrame frame = (StretchableFrame)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sizes", EditorStyles.boldLabel);

        if (frame.HasSizes)
        {
            Vector2 gap = frame.ModelledGap;
            EditorGUILayout.HelpBox(frame.PieceCount + " pieces read: " + frame.Orientation + ". Content between the corners " +
                gap.x.ToString("0.###") + " x " + gap.y.ToString("0.###") + " as modelled.", MessageType.None);
        }
        else
            EditorGUILayout.HelpBox("Not read yet: fill the slots, then read the sizes with the frame as modelled", MessageType.Info);

        if (GUILayout.Button("Read Sizes From Bounds"))
        {
            Undo.RecordObject(frame, "Read frame sizes");
            frame.ReadSizesFromBounds();
            EditorUtility.SetDirty(frame);
            contentSize = frame.ModelledGap;
        }

        using (new EditorGUI.DisabledScope(!frame.HasSizes))
        {
            if (float.IsNaN(contentSize.x))
                contentSize = frame.ModelledGap;

            EditorGUILayout.Space();
            contentSize = EditorGUILayout.Vector2Field("Content Size", contentSize);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Lay Out"))
                    Arrange(frame, f => { f.ResetToReadLayout(); f.SetLocalContentSize(contentSize); }, "Lay out frame");
                if (GUILayout.Button("Reset To Read Layout"))
                    Arrange(frame, f => f.ResetToReadLayout(), "Reset frame layout");
            }
        }
    }

    static void Arrange(StretchableFrame frame, System.Action<StretchableFrame> arrange, string undoName)
    {
        Transform[] pieces = frame.PieceTransforms();
        Undo.RecordObjects(pieces, undoName);
        arrange(frame);
        foreach (Transform piece in pieces)
            EditorUtility.SetDirty(piece);
    }
}
