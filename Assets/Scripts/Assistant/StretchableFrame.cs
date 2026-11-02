using System;
using System.Collections.Generic;
using UnityEngine;

// A frame sliced into 9 slots (4 corners, 4 edges, an optional centre), each a list of pieces, that fits content of any
// size while its corners keep their shape. Everything is read from the pieces' bounding boxes (Read Sizes From Bounds):
// which way the frame runs (from the left to the right slots, from the bottom to the top ones, so it may be modelled
// in any plane, seen from either side), each column's width and each row's height. SetContentSize then lays the frame
// out anew around content of that size: the corners keep their size, the edges stretch along the content, the centre
// both ways, and the whole frame sits with its bottom centre on this object's origin.
// The pieces are direct children of this object; they may be rotated in steps of 90 degrees or mirrored by negative
// scale. AssistantTerminalMirror sets the content size to the mirror's every frame.
public class StretchableFrame : MonoBehaviour
{
    [Header("Pieces")]
    [SerializeField] Transform[] topLeft = new Transform[0];
    [SerializeField] Transform[] top = new Transform[0];
    [SerializeField] Transform[] topRight = new Transform[0];
    [SerializeField] Transform[] left = new Transform[0];
    [Tooltip("Optional: e.g. a backing plate behind the content")]
    [SerializeField] Transform[] center = new Transform[0];
    [SerializeField] Transform[] right = new Transform[0];
    [SerializeField] Transform[] bottomLeft = new Transform[0];
    [SerializeField] Transform[] bottom = new Transform[0];
    [SerializeField] Transform[] bottomRight = new Transform[0];

    // As read from the bounding boxes, in this object's space
    [Serializable]
    struct Piece
    {
        public Transform transform;
        public int slot;
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
    }

    [SerializeField, HideInInspector] Piece[] pieces;
    // Each slot's pieces together, by slot (row * 3 + column; rows bottom to top, columns left to right)
    [SerializeField, HideInInspector] Bounds[] slotBounds;
    [SerializeField, HideInInspector] bool[] slotUsed;
    // The axes width and height run along (0 x, 1 y, 2 z) and their direction (+1, -1)
    [SerializeField, HideInInspector] int widthAxis;
    [SerializeField, HideInInspector] int heightAxis = 1;
    [SerializeField, HideInInspector] float widthSign = 1;
    [SerializeField, HideInInspector] float heightSign = 1;
    // Column widths and row heights of the corners and edges, and the gap between the corners, as modelled
    [SerializeField, HideInInspector] Vector3 columnWidths;
    [SerializeField, HideInInspector] Vector3 rowHeights;
    [SerializeField, HideInInspector] Vector2 modelledGap;
    // How far the centre piece reaches in front of and behind the origin plane (0 without a centre piece)
    [SerializeField, HideInInspector] float centreFront;
    [SerializeField, HideInInspector] float centreBack;

    Vector2 appliedSize = new Vector2(float.NaN, float.NaN);

    public bool HasSizes { get { return pieces != null && pieces.Length > 0; } }
    public int PieceCount { get { return HasSizes ? pieces.Length : 0; } }
    // The space between the corners as modelled: the content size the frame was built for
    public Vector2 ModelledGap { get { return modelledGap; } }
    // The same in meters (world units), at the frame's current scale
    public Vector2 ModelledGapWorld
    {
        get
        {
            Vector3 scale = transform.lossyScale;
            return new Vector2(modelledGap.x * Mathf.Abs(scale[widthAxis]), modelledGap.y * Mathf.Abs(scale[heightAxis]));
        }
    }

    public string Orientation
    {
        get
        {
            const string axes = "XYZ";
            Vector3 front = Front;
            int frontAxis = Dominant(front);
            return "width along " + (widthSign > 0 ? "+" : "-") + axes[widthAxis] + ", height along " + (heightSign > 0 ? "+" : "-") +
                axes[heightAxis] + ", seen from " + (front[frontAxis] > 0 ? "+" : "-") + axes[frontAxis];
        }
    }

    Vector3 Right { get { return Axis(widthAxis) * widthSign; } }
    Vector3 Up { get { return Axis(heightAxis) * heightSign; } }
    // Towards the viewer: a UI canvas, right along +X and up along +Y, is seen from -Z
    Vector3 Front { get { return -Vector3.Cross(Right, Up); } }

    // Turns the frame from its modelled orientation into the content's: right along X, up along Y, seen from -Z
    public Quaternion ContentRotation { get { return Quaternion.Inverse(Quaternion.LookRotation(-Front, Up)); } }

    // The centre piece's front and back surface in meters, measured from the origin plane towards the viewer and away
    // from them; AssistantTerminalMirror sets the mirror in front of it
    public float CentreFrontWorld { get { return centreFront * Mathf.Abs(transform.lossyScale[DepthAxis]); } }
    public float CentreBackWorld { get { return centreBack * Mathf.Abs(transform.lossyScale[DepthAxis]); } }

    int DepthAxis { get { return 3 - widthAxis - heightAxis; } }

    // The height of the bottom row in meters: the content's bottom edge sits this far above the origin
    public float BottomBorderWorld { get { return rowHeights.x * Mathf.Abs(transform.lossyScale[heightAxis]); } }

    static Vector3 Axis(int index)
    {
        Vector3 axis = Vector3.zero;
        axis[index] = 1;
        return axis;
    }

    Transform[][] Slots()
    {
        return new[] { bottomLeft, bottom, bottomRight, left, center, right, topLeft, top, topRight };
    }

    // Measures the pieces as they are now; call it with the frame as modelled (Reset To Read Layout first if it was
    // laid out since)
    public void ReadSizesFromBounds()
    {
        Transform[][] slots = Slots();
        List<Piece> read = new List<Piece>();
        Bounds[] bounds = new Bounds[9];
        bool[] used = new bool[9];

        for (int slot = 0; slot < 9; ++slot)
        {
            if (slots[slot] == null)
                continue;

            foreach (Transform piece in slots[slot])
            {
                if (piece == null)
                    continue;
                if (piece.parent != transform)
                {
                    Debug.LogWarning("StretchableFrame on " + name + ": " + piece.name + " is not a direct child, skipped");
                    continue;
                }

                Bounds pieceBounds;
                if (!LocalBounds(piece, out pieceBounds))
                {
                    Debug.LogWarning("StretchableFrame on " + name + ": " + piece.name + " has no mesh to measure, skipped");
                    continue;
                }

                if (used[slot])
                    bounds[slot].Encapsulate(pieceBounds);
                else
                    bounds[slot] = pieceBounds;
                used[slot] = true;

                read.Add(new Piece { transform = piece, slot = slot, position = piece.localPosition, scale = piece.localScale, rotation = piece.localRotation });
            }
        }

        foreach (int corner in new[] { 0, 2, 6, 8 })
        {
            if (!used[corner])
            {
                Debug.LogWarning("StretchableFrame on " + name + ": all four corners need a piece");
                return;
            }
        }

        // Which way the frame runs: from the left column to the right one, from the bottom row to the top one
        Vector3 rightward = Centre(bounds, used, 2, true) - Centre(bounds, used, 0, true);
        Vector3 upward = Centre(bounds, used, 2, false) - Centre(bounds, used, 0, false);
        int w = Dominant(rightward);
        int h = Dominant(upward);
        if (w == h)
        {
            Debug.LogWarning("StretchableFrame on " + name + ": the left/right and bottom/top slots don't lie along different axes");
            return;
        }

        widthAxis = w;
        heightAxis = h;
        widthSign = Mathf.Sign(rightward[w]);
        heightSign = Mathf.Sign(upward[h]);
        pieces = read.ToArray();
        slotBounds = bounds;
        slotUsed = used;

        // The widest piece group in each column, the highest in each row
        columnWidths = Vector3.zero;
        rowHeights = Vector3.zero;
        for (int slot = 0; slot < 9; ++slot)
        {
            if (!used[slot])
                continue;
            int column = slot % 3;
            int row = slot / 3;
            if (column != 1)
                columnWidths[column] = Mathf.Max(columnWidths[column], bounds[slot].size[w]);
            if (row != 1)
                rowHeights[row] = Mathf.Max(rowHeights[row], bounds[slot].size[h]);
        }

        // Between the corners' inner edges
        float leftEdge = Mathf.Max(Interval(bounds[0], w, widthSign).y, Interval(bounds[6], w, widthSign).y);
        float rightEdge = Mathf.Min(Interval(bounds[2], w, widthSign).x, Interval(bounds[8], w, widthSign).x);
        float bottomEdge = Mathf.Max(Interval(bounds[0], h, heightSign).y, Interval(bounds[2], h, heightSign).y);
        float topEdge = Mathf.Min(Interval(bounds[6], h, heightSign).x, Interval(bounds[8], h, heightSign).x);
        modelledGap = new Vector2(Mathf.Max(0, rightEdge - leftEdge), Mathf.Max(0, topEdge - bottomEdge));

        // The centre piece's surfaces along the depth axis, towards the viewer
        centreFront = centreBack = 0;
        if (used[4])
        {
            int d = DepthAxis;
            Vector2 depth = Interval(bounds[4], d, Mathf.Sign(Front[d]));
            centreFront = Mathf.Max(0, depth.y);
            centreBack = Mathf.Max(0, -depth.x);
        }

        appliedSize = new Vector2(float.NaN, float.NaN);
    }

    // The centre of a column (or a row) of the slots that have pieces
    static Vector3 Centre(Bounds[] bounds, bool[] used, int index, bool column)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < 3; ++i)
        {
            int slot = column ? i * 3 + index : index * 3 + i;
            if (!used[slot])
                continue;
            sum += bounds[slot].center;
            ++count;
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    static int Dominant(Vector3 v)
    {
        Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        return a.x >= a.y && a.x >= a.z ? 0 : a.y >= a.z ? 1 : 2;
    }

    // A box's extent along an axis, measured in the frame's direction (rightward or upward): (low, high)
    static Vector2 Interval(Bounds bounds, int axis, float sign)
    {
        return sign > 0 ? new Vector2(bounds.min[axis], bounds.max[axis]) : new Vector2(-bounds.max[axis], -bounds.min[axis]);
    }

    // The content's size in meters (world units)
    public void SetContentSize(float worldWidth, float worldHeight)
    {
        Vector3 scale = transform.lossyScale;
        SetLocalContentSize(new Vector2(worldWidth / Mathf.Abs(scale[widthAxis]), worldHeight / Mathf.Abs(scale[heightAxis])));
    }

    // Lays the frame out around content of this size (in this object's units), its bottom centre on the origin
    public void SetLocalContentSize(Vector2 size)
    {
        if (!HasSizes || (size - appliedSize).sqrMagnitude < 1e-12f)
            return;
        appliedSize = size;
        size = Vector2.Max(size, Vector2.zero);

        // The cells along the width (left, middle, right) centred on the origin, along the height from the origin up
        float totalWidth = columnWidths.x + size.x + columnWidths.z;
        Vector3 columnStarts = new Vector3(-totalWidth / 2, -size.x / 2, size.x / 2);
        Vector3 columnSizes = new Vector3(columnWidths.x, size.x, columnWidths.z);
        Vector3 rowStarts = new Vector3(0, rowHeights.x, rowHeights.x + size.y);
        Vector3 rowSizes = new Vector3(rowHeights.x, size.y, rowHeights.z);

        foreach (Piece piece in pieces)
        {
            if (piece.transform == null)
                continue;

            Vector3 position = piece.position;
            Vector3 scale = piece.scale;
            int column = piece.slot % 3;
            int row = piece.slot / 3;
            Place(piece, widthAxis, widthSign, slotBounds[piece.slot], column, columnStarts[column], columnSizes[column], ref position, ref scale);
            Place(piece, heightAxis, heightSign, slotBounds[piece.slot], row, rowStarts[row], rowSizes[row], ref position, ref scale);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
        }
    }

    // Along one of the frame's axes: the piece's slot is moved into its cell, against the frame's outer edge for the
    // first and last cell, and stretched to fill the middle one; the piece keeps its place within its slot
    static void Place(Piece piece, int axis, float sign, Bounds slot, int cell, float cellStart, float cellSize, ref Vector3 position, ref Vector3 scale)
    {
        Vector2 slotSpan = Interval(slot, axis, sign);
        float slotSize = slotSpan.y - slotSpan.x;

        float stretch = 1;
        float start;
        if (cell == 1)
        {
            stretch = slotSize > 1e-6f ? cellSize / slotSize : 1;
            start = cellStart;
        }
        else if (cell == 0)
            start = cellStart;
        else
            start = cellStart + cellSize - slotSize;

        // The pivot, measured in the frame's direction, mapped like the rest of the slot
        float pivot = piece.position[axis] * sign;
        position[axis] = (start + (pivot - slotSpan.x) * stretch) * sign;

        int localAxis = AlignedAxis(piece.rotation, axis);
        scale[localAxis] = piece.scale[localAxis] * stretch;
    }

    public void ResetToReadLayout()
    {
        if (!HasSizes)
            return;

        foreach (Piece piece in pieces)
        {
            if (piece.transform == null)
                continue;
            piece.transform.localPosition = piece.position;
            piece.transform.localScale = piece.scale;
        }
        appliedSize = new Vector2(float.NaN, float.NaN);
    }

    // The measured pieces, e.g. to record them for undo before laying them out in the editor
    public Transform[] PieceTransforms()
    {
        if (!HasSizes)
            return new Transform[0];

        List<Transform> transforms = new List<Transform>();
        foreach (Piece piece in pieces)
            if (piece.transform != null)
                transforms.Add(piece.transform);
        return transforms.ToArray();
    }

    // Which of a piece's own axes lies along the frame's axis
    static int AlignedAxis(Quaternion rotation, int frameAxis)
    {
        Vector3 direction = Axis(frameAxis);
        int best = 0;
        float bestDot = -1;
        for (int i = 0; i < 3; ++i)
        {
            float dot = Mathf.Abs(Vector3.Dot(rotation * Axis(i), direction));
            if (dot > bestDot)
            {
                bestDot = dot;
                best = i;
            }
        }
        return best;
    }

    // A piece's meshes (on it and its children) as a box in this object's space
    bool LocalBounds(Transform piece, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        Matrix4x4 toFrame = transform.worldToLocalMatrix;
        foreach (MeshFilter filter in piece.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;

            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 toLocal = toFrame * filter.transform.localToWorldMatrix;
            for (int c = 0; c < 8; ++c)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                Vector3 point = toLocal.MultiplyPoint3x4(corner);
                if (!any)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    any = true;
                }
                else
                    bounds.Encapsulate(point);
            }
        }
        return any;
    }

    // Each slot's box as read (yellow), and the directions read from the origin (red right, green up, blue towards the
    // viewer)
    void OnDrawGizmosSelected()
    {
        if (!HasSizes || slotUsed == null)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        for (int slot = 0; slot < 9; ++slot)
            if (slotUsed[slot])
                Gizmos.DrawWireCube(slotBounds[slot].center, slotBounds[slot].size);

        float length = Mathf.Max(modelledGap.x, modelledGap.y, 0.01f) * 0.25f;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, Right * length);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Vector3.zero, Up * length);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, Front * length);
    }
}
