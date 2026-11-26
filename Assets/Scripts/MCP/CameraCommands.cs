using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

// MCP commands for the desktop (CAD) camera: CADController with its CameraRecorder, in the FPSController prefab.
// Views are a position and a point looked at (the controller's orbit pivot). Scripted moves (keyframe paths and orbits)
// play live, one after the other, and can write themselves into the recorder's path frame by frame at its frame rate,
// so they play back (P) and export as PNG frames like a path recorded with the mouse.
// Yaw is in degrees clockwise around Y seen from above (0 faces +Z), pitch is positive looking up.
public static class CameraCommands
{
    public static void Register(McpBridge bridge)
    {
        bridge.Register("get_desktop_camera", GetCamera);
        bridge.Register("set_desktop_camera", SetCamera);
        bridge.Register("move_desktop_camera", MoveCamera);
        bridge.Register("stop_desktop_camera", StopCamera);
        bridge.Register("camera_recording", CameraRecording);
        bridge.Register("export_camera_recording", ExportRecording);
    }

    // Next to the saved scenes and animations
    public static string Folder
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRoxel", "Recordings"); }
    }

    static readonly Regex validName = new Regex(@"^[\w\- ]{1,64}$");

    // CADController's orbit limits; a steeper view would snap back at the next mouse orbit
    const float MaxPitch = 80;
    const float MaxShotDuration = 600;

    class Rig
    {
        public CameraRecorder recorder;
        public CADController controller;
        public Camera camera;

        public bool Valid { get { return recorder != null && recorder.isActiveAndEnabled && controller != null && camera != null; } }
    }

    static Rig rig;

    static Rig FindRig()
    {
        if (rig != null && rig.Valid)
            return rig;

        rig = null;
        CameraRecorder recorder = UnityEngine.Object.FindObjectOfType<CameraRecorder>();
        if (recorder == null)
            return null;

        Rig found = new Rig { recorder = recorder, controller = recorder.GetComponent<CADController>(), camera = recorder.GetComponent<Camera>() };
        if (!found.Valid)
            return null;
        return rig = found;
    }

    static Rig DesktopCamera()
    {
        Rig found = FindRig();
        if (found == null)
            throw new McpException("VRoxel is not in desktop mode: the CAD camera is only there once the desktop controller is chosen in the start menu (not VR), and not in an orthogonal view");
        return found;
    }

    // ---------------------------------------------------------------- views

    struct View
    {
        public Vector3 position;
        public Vector3 lookAt;

        public Vector3 Direction { get { return lookAt - position; } }
        public float Distance { get { return Direction.magnitude; } }
    }

    static View CurrentView(Rig r)
    {
        Transform t = r.camera.transform;
        float distance = Mathf.Max(Vector3.Distance(r.controller.Pivot, t.position), 0.1f);
        return new View { position = t.position, lookAt = t.position + t.forward * distance };
    }

    static float Yaw(Vector3 direction)
    {
        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        return yaw < 0 ? yaw + 360 : yaw;
    }

    static float Pitch(Vector3 direction)
    {
        return Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1, 1)) * Mathf.Rad2Deg;
    }

    static Vector3 Direction(float yaw, float pitch)
    {
        float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(p) * Mathf.Sin(y), Mathf.Sin(p), Mathf.Cos(p) * Mathf.Cos(y));
    }

    // The camera has no roll; Unity's pitch is positive looking down
    static Quaternion Rotation(View view)
    {
        Vector3 direction = view.Direction;
        if (direction.sqrMagnitude < 1e-8f)
            direction = Vector3.forward;
        return Quaternion.Euler(-Mathf.Clamp(Pitch(direction), -MaxPitch, MaxPitch), Yaw(direction), 0);
    }

    static void Apply(Rig r, View view)
    {
        r.controller.SetView(view.position, Rotation(view), view.Distance);
    }

    // A view described by any of position, look_at, yaw, pitch and distance, relative to the previous one:
    // with a position, the camera looks at look_at, else along yaw / pitch, else keeps looking at the previous point;
    // without, it orbits: it looks at look_at (default the previous point) along yaw / pitch from distance away.
    class ViewSpec
    {
        public Vector3? position, lookAt;
        public float? yaw, pitch, distance;

        public bool Empty { get { return position == null && lookAt == null && yaw == null && pitch == null && distance == null; } }

        public static ViewSpec Read(JObject args)
        {
            ViewSpec spec = new ViewSpec
            {
                position = OptVector3(args["position"]),
                lookAt = OptVector3(args["look_at"]),
                yaw = OptFloat(args["yaw"]),
                pitch = OptFloat(args["pitch"]),
                distance = OptFloat(args["distance"])
            };
            if (spec.distance.HasValue && spec.distance.Value <= 0)
                throw new McpException("distance must be positive");
            if (spec.pitch.HasValue && Mathf.Abs(spec.pitch.Value) > MaxPitch)
                throw new McpException("pitch must be within +/-" + MaxPitch + " degrees, as the camera's orbit");
            if (spec.position.HasValue && spec.lookAt.HasValue && Vector3.Distance(spec.position.Value, spec.lookAt.Value) < 0.01f)
                throw new McpException("look_at must not be the camera position");
            return spec;
        }

        public View Resolve(View previous)
        {
            Vector3 dir = previous.Direction;
            float yawNow = Yaw(dir), pitchNow = Mathf.Clamp(Pitch(dir), -MaxPitch, MaxPitch), distanceNow = previous.Distance;

            if (position.HasValue)
            {
                Vector3 p = position.Value;
                if (lookAt.HasValue)
                    return new View { position = p, lookAt = lookAt.Value };
                if (yaw.HasValue || pitch.HasValue || distance.HasValue)
                    return new View { position = p, lookAt = p + Direction(yaw ?? yawNow, pitch ?? pitchNow) * (distance ?? distanceNow) };
                return new View { position = p, lookAt = previous.lookAt };
            }

            Vector3 pivot = lookAt ?? previous.lookAt;
            float d = distance ?? (lookAt.HasValue ? Vector3.Distance(lookAt.Value, previous.position) : distanceNow);
            return new View { position = pivot - Direction(yaw ?? yawNow, pitch ?? pitchNow) * Mathf.Max(d, 0.1f), lookAt = pivot };
        }
    }

    static object ViewResult(View view)
    {
        Vector3 dir = view.Direction;
        return new
        {
            position = AvatarCommands.ToArray(view.position),
            look_at = AvatarCommands.ToArray(view.lookAt),
            yaw = AvatarCommands.Round(Yaw(dir)),
            pitch = AvatarCommands.Round(Pitch(dir)),
            distance = AvatarCommands.Round(dir.magnitude)
        };
    }

    // ---------------------------------------------------------------- scripted moves

    abstract class Shot
    {
        public float duration;
        public string record;   // off, new, append
        public abstract View Evaluate(float time);
    }

    // Through keyframes on a spline timed by their durations; smooth starts and ends at rest, linear moves evenly.
    // Position, yaw, pitch and pivot distance are splined, so the camera turns smoothly, also on the spot; between two
    // keyframes looking at the same point it keeps that point in view.
    class PathShot : Shot
    {
        public View[] keys;
        public float[] times;
        public bool smooth;

        float[][] values;   // x, y, z, yaw (unwrapped), pitch, distance

        public void Prepare()
        {
            values = new float[keys.Length][];
            float previousYaw = 0;
            for (int i = 0; i < keys.Length; ++i)
            {
                Vector3 dir = keys[i].Direction;
                float yaw = Yaw(dir);
                if (i > 0)
                    yaw = previousYaw + Mathf.DeltaAngle(previousYaw, yaw);
                previousYaw = yaw;
                values[i] = new[] { keys[i].position.x, keys[i].position.y, keys[i].position.z, yaw, Mathf.Clamp(Pitch(dir), -MaxPitch, MaxPitch), dir.magnitude };
            }
        }

        public override View Evaluate(float time)
        {
            int last = keys.Length - 1;
            if (time <= 0)
                return keys[0];
            if (time >= times[last])
                return keys[last];

            int i = 0;
            while (i < last - 1 && time >= times[i + 1])
                ++i;

            float dt = times[i + 1] - times[i];
            float u = (time - times[i]) / dt;

            float[] v = new float[6];
            for (int c = 0; c < 6; ++c)
                v[c] = Hermite(values[i][c], Tangent(i, c) * dt, values[i + 1][c], Tangent(i + 1, c) * dt, u);

            Vector3 position = new Vector3(v[0], v[1], v[2]);
            if (Vector3.Distance(keys[i].lookAt, keys[i + 1].lookAt) < 0.01f)
                return new View { position = position, lookAt = keys[i].lookAt };
            return new View { position = position, lookAt = position + Direction(v[3], v[4]) * Mathf.Max(v[5], 0.1f) };
        }

        float Tangent(int k, int c)
        {
            int last = keys.Length - 1;
            if (k == 0)
                return smooth ? 0 : (values[1][c] - values[0][c]) / (times[1] - times[0]);
            if (k == last)
                return smooth ? 0 : (values[last][c] - values[last - 1][c]) / (times[last] - times[last - 1]);
            return (values[k + 1][c] - values[k - 1][c]) / (times[k + 1] - times[k - 1]);
        }

        static float Hermite(float p0, float m0, float p1, float m1, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return (2 * u3 - 3 * u2 + 1) * p0 + (u3 - 2 * u2 + u) * m0 + (-2 * u3 + 3 * u2) * p1 + (u3 - u2) * m1;
        }
    }

    // Around a center it looks at, at a radius and height, turning degrees (clockwise seen from above when positive)
    class OrbitShot : Shot
    {
        public Vector3 center;
        public float radius, height, startAngle, degrees;
        public bool smooth;

        public override View Evaluate(float time)
        {
            float t = Mathf.Clamp01(duration > 0 ? time / duration : 1);
            if (smooth)
                t = t * t * (3 - 2 * t);
            float angle = (startAngle + degrees * t) * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
            return new View { position = position, lookAt = center };
        }
    }

    static Shot current;
    static float elapsed;
    static readonly Queue<Shot> queue = new Queue<Shot>();
    static int lastTick = -10;

    // The runner ticks every frame; if its host went away (scene reload), a new one starts
    static bool RunnerAlive { get { return Time.frameCount - lastTick <= 2; } }

    static float Remaining
    {
        get { return current == null ? 0 : Mathf.Max(0, current.duration - elapsed) + queue.Sum(shot => shot.duration); }
    }

    // Where the last scripted move ends, or the camera now
    static View EndView(Rig r)
    {
        Shot last = queue.Count > 0 ? queue.Last() : current;
        return last != null ? last.Evaluate(last.duration) : CurrentView(r);
    }

    static void StartShot(Rig r, Shot shot)
    {
        current = shot;
        elapsed = 0;

        if (r.recorder.Play)
            StopPlayback(r);
        if (shot.record != "off")
        {
            r.recorder.StopRecording();
            if (shot.record == "new")
                r.recorder.ClearRecording();
            RefreshRecordMenu();
        }

        Apply(r, shot.Evaluate(0));

        if (!RunnerAlive)
        {
            lastTick = Time.frameCount;
            McpBridge.Bridge.StartCoroutine(Run());
        }
    }

    static IEnumerator Run()
    {
        while (current != null)
        {
            lastTick = Time.frameCount;
            yield return null;
            lastTick = Time.frameCount;

            if (current == null)
                break;

            Rig r = FindRig();
            if (r == null)
            {
                // Left desktop mode: the moves can't go on
                current = null;
                queue.Clear();
                break;
            }
            if (r.recorder.Play)
            {
                // The user started playback: it drives the camera now
                StopShots(r);
                break;
            }

            elapsed += Time.deltaTime;
            if (elapsed < current.duration)
            {
                Apply(r, current.Evaluate(elapsed));
                continue;
            }

            Apply(r, current.Evaluate(current.duration));
            RecordShot(r, current, current.duration);
            current = null;
            if (queue.Count > 0)
                StartShot(r, queue.Dequeue());
        }
        lastTick = -10;
    }

    // Writes the shot, up to time, into the recorder's path: one pose per frame at the recorder's frame rate
    static void RecordShot(Rig r, Shot shot, float upTo)
    {
        if (shot.record == "off")
            return;

        float step = 1f / r.recorder.FrameRate;
        int count = Mathf.FloorToInt(upTo / step + 1e-4f);
        // A shot appended to a path starts where the path ends: its first pose is already there
        int first = r.recorder.SampleCount > 0 ? 1 : 0;

        List<Vector3> positions = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();
        for (int k = first; k <= count; ++k)
        {
            View view = shot.Evaluate(Mathf.Min(k * step, upTo));
            positions.Add(view.position);
            rotations.Add(Rotation(view));
        }
        r.recorder.AppendSamples(positions, rotations);
    }

    // Ends the running move where it is (recording what played of it) and drops the queued ones
    static bool StopShots(Rig r)
    {
        bool stopped = current != null || queue.Count > 0;
        if (current != null)
            RecordShot(r, current, Mathf.Min(elapsed, current.duration));
        current = null;
        queue.Clear();
        return stopped;
    }

    // ---------------------------------------------------------------- commands

    static object GetCamera(JObject args)
    {
        Rig r = DesktopCamera();
        return State(r);
    }

    static object State(Rig r)
    {
        View view = CurrentView(r);
        return new
        {
            view = ViewResult(view),
            field_of_view = AvatarCommands.Round(r.camera.fieldOfView),
            mouse_control = r.controller.enabled,
            move = current == null ? null : new
            {
                elapsed = AvatarCommands.Round(elapsed),
                remaining = AvatarCommands.Round(Remaining),
                queued = queue.Count
            },
            recording = RecordingState(r.recorder)
        };
    }

    static object RecordingState(CameraRecorder recorder)
    {
        return new
        {
            live_recording = recorder.Record,
            playing = recorder.Play,
            samples = recorder.SampleCount,
            seconds = AvatarCommands.Round(recorder.SampleCount / (float)recorder.FrameRate),
            frame_rate = recorder.FrameRate,
            playable = recorder.SampleCount >= CameraRecorder.MinPlaybackSamples,
            sampling_width = recorder.SamplingWidth,
            playback = recorder.Play ? new { frame = recorder.PlaybackPosition, frames = recorder.PlaybackFrames } : null,
            export = recorder.Exporting ? new { path = exportPath, frames_written = recorder.ExportedFrames, frames = recorder.PlaybackFrames } : null
        };
    }

    static void RefuseWhileExporting(Rig r)
    {
        if (r.recorder.Exporting)
            throw new McpException("The camera recording is being exported (" + r.recorder.ExportedFrames + " of " + r.recorder.PlaybackFrames + " frames): wait until get_desktop_camera shows no export");
    }

    static object SetCamera(JObject args)
    {
        Rig r = DesktopCamera();
        RefuseWhileExporting(r);

        ViewSpec spec = ViewSpec.Read(args);
        float? fov = OptFloat(args["field_of_view"]);
        if (fov.HasValue && (fov.Value < 5 || fov.Value > 120))
            throw new McpException("field_of_view must be between 5 and 120 degrees");

        bool stoppedMove = StopShots(r);
        bool stoppedPlayback = r.recorder.Play;
        if (stoppedPlayback)
            StopPlayback(r);

        if (!spec.Empty)
            Apply(r, spec.Resolve(CurrentView(r)));
        if (fov.HasValue)
            r.camera.fieldOfView = fov.Value;

        return new
        {
            view = ViewResult(CurrentView(r)),
            field_of_view = AvatarCommands.Round(r.camera.fieldOfView),
            stopped_move = stoppedMove,
            stopped_playback = stoppedPlayback
        };
    }

    static object MoveCamera(JObject args)
    {
        Rig r = DesktopCamera();
        RefuseWhileExporting(r);

        bool queued = args["queue"] != null && args["queue"].Type == JTokenType.Boolean && (bool)args["queue"];
        string easing = (string)args["easing"] ?? "smooth";
        if (easing != "smooth" && easing != "linear")
            throw new McpException("easing is 'smooth' or 'linear'");
        string record = (string)args["record"] ?? "off";
        if (record != "off" && record != "new" && record != "append")
            throw new McpException("record is 'off', 'new' or 'append'");
        if (record != "off" && !r.recorder.CapturesPath)
            throw new McpException("The camera recorder is set to capture a rotation or a character animation (Inspector), not a camera path");

        JArray keyframes = args["keyframes"] as JArray;
        JObject orbit = args["orbit"] as JObject;
        if ((keyframes == null || keyframes.Count == 0) == (orbit == null))
            throw new McpException("Give either keyframes or an orbit");

        if (!queued)
            StopShots(r);
        View start = EndView(r);
        float startsIn = queued ? Remaining : 0;

        Shot shot = keyframes != null ? PathFrom(keyframes, start, easing == "smooth") : OrbitFrom(orbit, start, easing == "smooth");
        shot.record = record;

        if (current == null)
            StartShot(r, shot);
        else
            queue.Enqueue(shot);

        object detail;
        PathShot path = shot as PathShot;
        if (path != null)
            detail = new { keyframes = path.keys.Select((key, i) => new { time = AvatarCommands.Round(path.times[i]), view = ViewResult(key) }).ToArray() };
        else
            detail = new { start = ViewResult(shot.Evaluate(0)), end = ViewResult(shot.Evaluate(shot.duration)), cut = Vector3.Distance(start.position, shot.Evaluate(0).position) > 0.05f };

        string note = null;
        if (record != "off" && r.recorder.SamplingWidth > 15)
            note = "Playback and export smooth the path over " + r.recorder.SamplingWidth + " samples (" +
                   AvatarCommands.Round(r.recorder.SamplingWidth / (float)r.recorder.FrameRate) +
                   " s), which rounds off scripted moves; camera_recording(sampling_width=1) plays them exactly";

        return new
        {
            duration = AvatarCommands.Round(shot.duration),
            starts_in = AvatarCommands.Round(startsIn),
            shot = detail,
            record,
            note
        };
    }

    static PathShot PathFrom(JArray keyframes, View start, bool smooth)
    {
        List<View> keys = new List<View> { start };
        List<float> times = new List<float> { 0 };
        View previous = start;
        float time = 0;

        foreach (JToken token in keyframes)
        {
            JObject key = token as JObject;
            if (key == null)
                throw new McpException("Each keyframe is an object with a duration and some of position, look_at, yaw, pitch and distance");

            float? duration = OptFloat(key["duration"]);
            if (!duration.HasValue || duration.Value < 0.1f)
                throw new McpException("Each keyframe needs a duration of at least 0.1 s (seconds to get there from the previous one)");

            ViewSpec spec = ViewSpec.Read(key);
            if (spec.Empty)
                throw new McpException("A keyframe needs at least one of position, look_at, yaw, pitch and distance");

            previous = spec.Resolve(previous);
            time += duration.Value;
            keys.Add(previous);
            times.Add(time);
        }

        if (time > MaxShotDuration)
            throw new McpException("A move lasts at most " + MaxShotDuration + " s; queue several");

        PathShot shot = new PathShot { keys = keys.ToArray(), times = times.ToArray(), smooth = smooth, duration = time };
        shot.Prepare();
        return shot;
    }

    static OrbitShot OrbitFrom(JObject orbit, View start, bool smooth)
    {
        Vector3 center = OptVector3(orbit["center"]) ?? start.lookAt;
        Vector3 offset = start.position - center;
        float horizontal = new Vector2(offset.x, offset.z).magnitude;

        float? radius = OptFloat(orbit["radius"]);
        if (radius.HasValue && radius.Value <= 0)
            throw new McpException("radius must be positive");
        if (!radius.HasValue && horizontal < 0.5f)
            throw new McpException("The camera is right above or below the center: give the orbit a radius");

        float? degrees = OptFloat(orbit["degrees"]);
        float? duration = OptFloat(orbit["duration"]);
        if (!degrees.HasValue || Mathf.Abs(degrees.Value) < 0.1f)
            throw new McpException("The orbit needs degrees to turn (negative turns anticlockwise seen from above)");
        if (!duration.HasValue || duration.Value < 0.1f || duration.Value > MaxShotDuration)
            throw new McpException("The orbit needs a duration between 0.1 and " + MaxShotDuration + " s");

        return new OrbitShot
        {
            center = center,
            radius = radius ?? horizontal,
            height = OptFloat(orbit["height"]) ?? offset.y,
            startAngle = OptFloat(orbit["start_angle"]) ?? Yaw(offset),
            degrees = degrees.Value,
            duration = duration.Value,
            smooth = smooth
        };
    }

    static object StopCamera(JObject args)
    {
        Rig r = DesktopCamera();
        bool stopped = StopShots(r);
        return new { stopped, view = ViewResult(CurrentView(r)), recording = RecordingState(r.recorder) };
    }

    static object CameraRecording(JObject args)
    {
        Rig r = DesktopCamera();
        CameraRecorder recorder = r.recorder;
        string action = (string)args["action"];
        float? width = OptFloat(args["sampling_width"]);

        if ((action != null && action != "status") || width.HasValue)
            RefuseWhileExporting(r);

        if (width.HasValue)
        {
            if (width.Value < 1 || width.Value > 1000)
                throw new McpException("sampling_width is between 1 (no smoothing) and 1000 samples");
            recorder.SetNewSamplingWidth(Mathf.RoundToInt(width.Value));
        }

        switch (action)
        {
            case null:
            case "status":
                break;
            case "play":
                if (!recorder.CapturesPath)
                    throw new McpException("The camera recorder is set to capture a rotation or a character animation (Inspector), not a camera path");
                if (recorder.SampleCount < CameraRecorder.MinPlaybackSamples)
                    throw new McpException("The recorded path has " + recorder.SampleCount + " poses; playback needs at least " +
                                           CameraRecorder.MinPlaybackSamples + " (" + AvatarCommands.Round(CameraRecorder.MinPlaybackSamples / (float)recorder.FrameRate) + " s)");
                StopShots(r);
                if (!recorder.Play)
                    recorder.PlayRecording(true);
                break;
            case "stop":
                if (recorder.Play)
                    StopPlayback(r);
                recorder.StopRecording();
                break;
            case "record":
                // As the R key: the user's mouse moves are recorded, appended to the path unless the camera jumped
                StopShots(r);
                if (recorder.Play)
                    StopPlayback(r);
                recorder.RecordCamera();
                break;
            case "clear":
                StopShots(r);
                if (recorder.Play)
                    StopPlayback(r);
                recorder.ClearRecording();
                break;
            default:
                throw new McpException("action is one of status, play, stop, record, clear");
        }

        RefreshRecordMenu();
        return RecordingState(recorder);
    }

    static string exportPath;

    static object ExportRecording(JObject args)
    {
        Rig r = DesktopCamera();
        RefuseWhileExporting(r);

        string kind = (string)args["kind"] ?? "video";
        if (kind != "video" && kind != "screenshot")
            throw new McpException("kind is 'video' or 'screenshot'");
        bool overwrite = args["overwrite"] != null && args["overwrite"].Type == JTokenType.Boolean && (bool)args["overwrite"];

        RecordMenu menu = UnityEngine.Object.FindObjectOfType<RecordMenu>(true);
        if (menu == null)
            throw new McpException("The scene has no record menu to export with");
        if (EscapeMenu.GamePaused)
            throw new McpException("The Esc menu is open: ask the user to close it first");
        if (!menu.enabled)
            throw new McpException("A file dialog of the record menu is open: ask the user to close it first");

        string name = (string)args["name"];
        string path = (string)args["path"];
        string extension = kind == "video" ? ".png" : ".jpg";
        if (!string.IsNullOrEmpty(path))
        {
            path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                path += extension;
            name = Path.GetFileNameWithoutExtension(path);
        }
        else
        {
            if (string.IsNullOrEmpty(name))
                name = (kind == "video" ? "camera_" : "view_") + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            if (!validName.IsMatch(name))
                throw new McpException("name may have 1-64 letters, digits, spaces, '-' and '_'");
            // Video frames get a folder of their own: the export first deletes the files starting with its name there
            path = kind == "video" ? Path.Combine(Folder, name, name + extension) : Path.Combine(Folder, name + extension);
        }

        string directory = Path.GetDirectoryName(path);
        Directory.CreateDirectory(directory);

        if (kind == "screenshot")
        {
            if (File.Exists(path) && !overwrite)
                throw new McpException(path + " exists; set overwrite to replace it");

            StopShots(r);
            menu.CaptureScreenshotTo(path);
            return new { kind, path, size = new[] { 3840, 2160 }, note = "Written within a few frames, of the current view" };
        }

        if (!r.recorder.CapturesPath)
            throw new McpException("The camera recorder is set to capture a rotation or a character animation (Inspector), not a camera path");
        if (r.recorder.SampleCount < CameraRecorder.MinPlaybackSamples)
            throw new McpException("The recorded path has " + r.recorder.SampleCount + " poses; exporting needs at least " + CameraRecorder.MinPlaybackSamples);

        string[] existing = Directory.GetFiles(directory, name + "_*.png");
        if (existing.Length > 0 && !overwrite)
            throw new McpException(existing.Length + " frames of an earlier export named " + name + " are in " + directory + "; set overwrite to replace them");

        StopShots(r);
        if (r.recorder.Play)
            StopPlayback(r);

        exportPath = path;
        menu.ExportVideoTo(path);

        // In the same frame the export starts, so frame 0 is the sequences' start: exports advance animation by
        // exactly one frame (1 / frame_rate) per image, so the avatars stay in step with the camera path
        bool restart = args["restart_sequences"] != null && args["restart_sequences"].Type == JTokenType.Boolean && (bool)args["restart_sequences"];
        int restarted = 0;
        if (restart)
            foreach (KeyValuePair<string, GameObject> avatar in AvatarRegistry.All())
            {
                AvatarCommands.GetSequencer(avatar.Value).Restart();
                ++restarted;
            }

        return new
        {
            kind,
            folder = directory,
            pattern = name + "_00000.png",
            frames = r.recorder.PlaybackFrames,
            frame_rate = r.recorder.FrameRate,
            restarted_avatars = restarted,
            note = "Exports in the background, one PNG per recorded pose, with VRoxel's interface hidden; get_desktop_camera shows the progress. " +
                   "Join the frames into a video at frame_rate, e.g. ffmpeg -framerate " + r.recorder.FrameRate + " -i \"" + name + "_%05d.png\" -pix_fmt yuv420p " + name + ".mp4"
        };
    }

    static void StopPlayback(Rig r)
    {
        r.recorder.StopPlayback();
        RefreshRecordMenu();
    }

    static void RefreshRecordMenu()
    {
        RecordMenu menu = UnityEngine.Object.FindObjectOfType<RecordMenu>(true);
        if (menu != null)
            menu.RefreshButtons();
    }

    // ---------------------------------------------------------------- arguments

    static float? OptFloat(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;
        if (token.Type != JTokenType.Float && token.Type != JTokenType.Integer)
            throw new McpException("Expected a number, got " + token.ToString(Newtonsoft.Json.Formatting.None));
        return (float)token;
    }

    static Vector3? OptVector3(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;
        return AvatarCommands.ReadVector3(token, Vector3.zero);
    }
}
