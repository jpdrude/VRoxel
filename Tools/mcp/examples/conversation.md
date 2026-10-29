# Example: a conversation group

Two or three people standing together, taking turns to talk. Tested in VRoxel.

## Placing them

- Stand people 1.0-1.5 m apart, facing the group's centre.
- Yaw that faces from (x1, z1) towards (x2, z2): `yaw = atan2(x2 - x1, z2 - z1)` in degrees
  (0 faces +Z, 90 faces +X). Two people facing each other differ by 180°.
- Check the floor first with `get_ground_height`; spawn snaps to it anyway.

```
spawn_avatar(position=[2, 0, 0],   yaw=0,   name="Talker",   animation="gesture_talking")
spawn_avatar(position=[2, 0, 1.2], yaw=180, name="Listener", animation="gesture_listening", height_cm=165)
```

Each spawn gets its own colour; tell the user which is which ("the blue one talks first").

## Taking turns

Give everyone the same total length, with the talking parts offset, and `end="loop"`:

```
set_sequence(avatar_id="blue", end="loop", steps=[
    {"animation": "gesture_talking",    "duration": 6},
    {"animation": "gesture_explaining", "duration": 2.5},
    {"animation": "gesture_listening",  "duration": 5}])

set_sequence(avatar_id="red", end="loop", steps=[
    {"animation": "gesture_listening",   "duration": 6},
    {"animation": "gesture_nodYes_once"},
    {"animation": "gesture_talking",     "duration": 5}])

restart_sequences()   # starts both in sync
```

- One-shot gestures (`_once`, e.g. nodding, handshake) play once when no duration is given; read their length
  from `list_animations` to keep the turns aligned (here 6 + 2.6 + 5 ≈ 6 + 2.5 + 5).
- Gestures are full-body standing clips. Seated groups use the `sit_` loops (`sit_talking_loop`,
  `sit_laughing_loop`) instead.
- Nobody moves, so `loop_gap` is 0 and the loop is seamless.
