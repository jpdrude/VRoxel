using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Valve.VR.InteractionSystem;

// MCP commands for the VR player and the program: where the player and their controllers are, moving the player, and
// quitting. Yaw is in degrees clockwise around Y seen from above (0 faces +Z), as for the avatars.
public static class PlayerCommands
{
    public static void Register(McpBridge bridge)
    {
        bridge.Register("get_player_pose", GetPlayerPose);
        bridge.Register("teleport_player", TeleportPlayer);
        bridge.Register("quit_program", QuitProgram);
    }

    // The SteamVR player rig, when VRoxel runs in VR mode (also its fallback without a headset)
    static Player VrPlayer()
    {
        Player player = Player.instance;
        if (player == null || !player.isActiveAndEnabled || player.hmdTransform == null)
            throw new McpException("VRoxel is not in VR mode: the player pose and teleporting are only available with the VR player");
        return player;
    }

    static object GetPlayerPose(JObject args)
    {
        Player player = VrPlayer();
        Transform head = player.hmdTransform;
        Transform origin = player.trackingOriginTransform;

        return new
        {
            mode = Valve.VR.SteamVR.active ? "vr" : "vr_fallback",
            // Where the player stands, under the head, and which way they look
            feet = AvatarCommands.ToArray(player.feetPositionGuess),
            heading = Yaw(head.forward),
            head = new { position = AvatarCommands.ToArray(head.position), yaw = Yaw(head.forward), pitch = AvatarCommands.Round(Pitch(head.forward)) },
            eye_height = AvatarCommands.Round(player.eyeHeight),
            controllers = new { left = Controller(player.leftHand), right = Controller(player.rightHand) },
            // A scaled player sees the world smaller or larger
            world_scale = AvatarCommands.Round(origin.lossyScale.x),
            tracking_origin = AvatarCommands.ToArray(origin.position)
        };
    }

    static object Controller(Hand hand)
    {
        if (hand == null || !hand.isActive || !hand.gameObject.activeInHierarchy)
            return null;

        Transform t = hand.transform;
        return new
        {
            position = AvatarCommands.ToArray(t.position),
            yaw = Yaw(t.forward),
            pitch = AvatarCommands.Round(Pitch(t.forward)),
            tracked = hand.isPoseValid
        };
    }

    // Moves the rig so the player's feet stand at position (y is the floor), facing yaw if given. The rig turns around
    // the head, so the player isn't swung sideways; the elevation platform is lowered, as by a teleport.
    static object TeleportPlayer(JObject args)
    {
        Player player = VrPlayer();
        Vector3 target = AvatarCommands.ReadVector3(args["position"], new Vector3(float.NaN, 0, 0));
        if (float.IsNaN(target.x))
            throw new McpException("teleport_player needs a position [x, y, z]");

        // An inactive menu hasn't started: it has no elevation platform yet, nor listens to teleports
        MenuSwitch menu = Object.FindObjectOfType<MenuSwitch>();
        if (menu != null && menu.isActiveAndEnabled)
            menu.SetPlayerHeight(0);

        Transform origin = player.trackingOriginTransform;
        JToken yawToken = args["yaw"];
        if (yawToken != null && yawToken.Type != JTokenType.Null)
        {
            float turn = Mathf.DeltaAngle(Yaw(player.hmdTransform.forward), (float)yawToken);
            origin.RotateAround(player.hmdTransform.position, Vector3.up, turn);
        }

        origin.position += target - player.feetPositionGuess;

        // As after a teleport: MenuSwitch takes the new position as the elevation platform's base
        Teleport.Player.Send(null);

        return new
        {
            feet = AvatarCommands.ToArray(player.feetPositionGuess),
            heading = Yaw(player.hmdTransform.forward)
        };
    }

    // Ends Play mode in the editor, or VRoxel itself in a build, once this answer is on its way. The bridge, speech to
    // text and Whisper's server stop on the way out; the assistant terminal stays open.
    static object QuitProgram(JObject args)
    {
        McpBridge.Bridge.StartCoroutine(QuitSoon());
        return new { quitting = Application.isEditor ? "play_mode" : "application" };
    }

    static IEnumerator QuitSoon()
    {
        yield return new WaitForSecondsRealtime(0.5f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static float Yaw(Vector3 forward)
    {
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        return AvatarCommands.Round(yaw < 0 ? yaw + 360 : yaw);
    }

    // Up is positive
    static float Pitch(Vector3 forward)
    {
        return Mathf.Asin(Mathf.Clamp(forward.normalized.y, -1, 1)) * Mathf.Rad2Deg;
    }
}
