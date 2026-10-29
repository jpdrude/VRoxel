# Example: walking to a chair, working, leaving

Someone walks to a desk, sits down, types for a while, stands up and walks away.
Clip values are measured from the library; check them with `get_animation_motion`, and the whole plan with
`plan_sequence`.

## Sitting down moves backwards

`sit_standToSit` moves the avatar about **0.36 m backwards** while it sits, and `sit_sitToStand` 0.35 m forwards
again. So the avatar must start sitting down 0.36 m in front of the seat, **facing away from the chair** (towards
the desk it will work at, if the chair faces the desk).

If the seat centre is S and the avatar should face yaw Y once seated, the start point is
`S + 0.36 · (sin Y, 0, cos Y)`.

## The sequence

Seat at (4, 0, 2), desk on the +Z side so the seated avatar faces yaw 0:

```
set_sequence(avatar_id="green", end="hold", steps=[
    {"animation": "loco_walk",       "walk_to": [4, 0, 2.36]},   # 0.36 m in front of the seat
    {"animation": "idle_neutral",    "face": 0, "duration": 0.6}, # turn to face the desk, back to the chair
    {"animation": "sit_standToSit"},
    {"animation": "sit_typing_loop", "duration": 20},
    {"animation": "sit_sitToStand"},
    {"animation": "loco_walk",       "walk_to": [8, 0, 2.3]},
    {"animation": "idle_neutral"}])
```

Tested in VRoxel: the avatar sat down within 4 cm of the seat.

- Walking ends facing the walk direction; an on-the-spot step with `face` turns the avatar before it sits.
  Turn clips (`turn_left90` etc.) turn more than their names say (`turn_left90` ≈ 123°), so prefer `face`.
- The clips sit at a standard chair height of about 0.45 m above the floor the avatar stands on. Much higher
  seats (bar stools) or low ones (sofas) will look off; there are no clips for them.
- The seated loops are `sit_idle`, `sit_typing_loop`, `sit_writing_loop`, `sit_talking_loop`, `sit_laughing_loop`,
  `sit_clap_loop`, `sit_disbelief_loop`. Seated avatars must not play `gesture_` clips (they stand up).
- A walk as the last step keeps walking past its target (the last clip holds by looping), so end with an idle.
- Lying in bed works the same way, from the floor beside the bed: sit on its edge with `sit_standToSit` (start
  0.36 m in front of the edge, back to the bed), then `bed_sitToLie` (moves 0.57 m further back and lifts the body
  onto the mattress), `bed_lyingIdle`, `bed_lieToSit`, `sit_sitToStand`. The avatar's root stays on the floor; the
  clips lift the body to bed height, so spawn it on the floor, not on the bed. Crossfades into some of these
  clips currently shift the avatar by 0.2-0.4 m more than planned; compare `list_avatars` with the plan.
