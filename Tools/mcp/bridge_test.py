"""Smoke test for McpBridge without the MCP SDK: python Tools/mcp/bridge_test.py

Needs VRoxel in Play mode with an McpBridge component. Spawns two avatars, plays clips, then lists them.
Pass --keep to leave the avatars in the scene.
"""

import json
import sys

from vroxel_bridge import BridgeError, call


def show(title, value):
    print(f"--- {title}")
    print(json.dumps(value, indent=2))


def main():
    try:
        show("ping", call("ping"))

        animations = call("list_animations")
        show("list_animations", animations)
        if not animations:
            print("The library is empty. Run VRoxel > Rebuild Mixamo Animation Library in Unity.")
            return 1

        names = [a["name"] for a in animations]
        wave = "gesture_waving" if "gesture_waving" in names else names[0]
        idle = next((n for n in names if n.startswith("idle_")), names[0])

        # Baked root motion: a moving clip should report a non-zero cycle displacement
        moving = next((a["name"] for a in animations if a["speed"] > 0.05 and a["loop"]), None)
        if moving:
            motion = call("get_animation_motion", animation=moving, interval=0.5)
            show(f"get_animation_motion {moving}", motion)
            if not any(abs(s["forward"]) + abs(s["right"]) > 0.05 for s in motion["samples"]):
                print("!!! baked motion is empty; check the Unity console for AnimationLibraryBuilder warnings")

        show("get_ground_height (0, 2)", call("get_ground_height", x=0, z=2))

        first = call("spawn_avatar", position=[0, 0, 2], yaw=180, animation=wave)
        second = call("spawn_avatar", position=[0, 0, 4], height_cm=165, animation=idle)
        show("spawned", [first, second])

        show("play_animation", call("play_animation", avatar_id=second["id"], animation=wave, fade=0.5))

        # Walk to a point, wave twice, walk back, idle; loops from the spawn pose
        if moving:
            steps = [
                {"animation": moving, "walk_to": [3, 0, 2]},
                {"animation": wave, "face": 270, "loops": 2},
                {"animation": moving, "walk_to": [0, 0, 2]},
                {"animation": idle, "face": 180, "duration": 2},
            ]
            show("plan_sequence", call("plan_sequence", avatar_id=first["id"], steps=steps, end="loop"))
            show("set_sequence", call("set_sequence", avatar_id=first["id"], steps=steps, end="loop"))
        else:
            print("--- no looping clip with root motion, skipping sequences")

        show("list_avatars", call("list_avatars"))

        # Undo the sequence and the second avatar's clip, then redo one of them
        show("undo", call("undo"))
        show("undo", call("undo"))
        show("redo", call("redo"))

        try:
            call("play_animation", avatar_id=first["id"], animation="does_not_exist")
        except BridgeError as e:
            print(f"--- expected error\n{e}")

        if "--keep" not in sys.argv:
            for avatar in (first, second):
                call("remove_avatar", avatar_id=avatar["id"])
            print("--- removed both avatars")

    except BridgeError as e:
        print(f"Bridge error: {e}")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
