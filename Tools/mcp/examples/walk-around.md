# Example: walking a loop around something

One person walking clockwise in a circle of 3 m radius around a point, forever. Tested in VRoxel.

## Waypoints

`walk_to` steers in straight lines, so a circle is a polygon of waypoints. 12 points (every 30°) look round;
the avatar turns about 30° at each.

Clockwise seen from above, around centre (cx, cz) with radius r, the point at angle a (0° = +Z side, going
towards +X) is `(cx + r·sin a, cz + r·cos a)`. For centre (2, 0.6) and r = 3:

```
a=0:   (2, 3.6)       a=30:  (3.5, 3.198)    a=60:  (4.598, 2.1)   a=90:  (5, 0.6)
a=120: (4.598, -0.9)  a=150: (3.5, -1.998)   a=180: (2, -2.4)      a=210: (0.5, -1.998)
a=240: (-0.598, -0.9) a=270: (-1, 0.6)       a=300: (-0.598, 2.1)  a=330: (0.5, 3.198)
```

Counter-clockwise: walk the same points in reverse order.

## The sequence

Start at the a=0 point and walk to the others, ending back at the start:

```
plan_sequence(avatar_id="teal", end="loop", start_position=[2, 0, 3.6], start_yaw=75, steps=[
    {"animation": "loco_walk", "walk_to": [3.5, 0, 3.198]},
    {"animation": "loco_walk", "walk_to": [4.598, 0, 2.1]},
    ...
    {"animation": "loco_walk", "walk_to": [2, 0, 3.6]}])
```

- `start_yaw` is the heading of the **last** segment (from a=330 to a=0, here 75°), not the circle's tangent.
  Otherwise the avatar snaps by the difference every lap; `plan_sequence` reports it as `loop_gap_yaw`.
  Aim for `loop_gap` ≈ 0 and `loop_gap_yaw` ≈ 0, then call `set_sequence` with the same arguments.
- A lap with `loco_walk` (≈1.56 m/s) takes about 12 s here. `loco_walkSlow`, `loco_jogging` etc. change the pace;
  `speed` on a step plays the clip faster or slower.
- Keep the radius clear of other avatars: they don't avoid each other.
