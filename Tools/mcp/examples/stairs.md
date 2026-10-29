# Example: climbing stairs to another storey

Someone walks to a staircase, climbs it and walks on at the top. Clip values are measured from the library;
check the plan with `plan_sequence`. Tested in VRoxel without a staircase: the avatar climbed to 2.91 m as planned.

## Find the storeys

`get_ground_height(x, z)` lists floor heights top to bottom. Probe the bottom and the top of the stairs to get
both storeys, e.g. 0.0 at the bottom and 3.0 at the landing.

## How far one cycle climbs

`vert_stairsUp_loop` rises about **0.48 m** and moves 0.52 m forward per cycle (1.37 s), so its stairs have a
fixed slope: about 0.92 m up per metre forward. `vert_stairsDown_loop` drops 0.51 m per 0.48 m forward.

Vertical clips make the avatar airborne: it follows the clip's height, not the floor, until its feet reach a real
floor again. A `walk_to` step with a vertical clip ends on arrival at the target's x / z, and the height it reaches
is the clip's slope times the horizontal distance; the target's y is ignored. So pick the target where the clip has
climbed the storey height: for 3.0 m that is 3.0 / 0.92 ≈ 3.25 m past the first step. Shallower real stairs end
further away; the avatar then arrives too early and too high, or walks on in the air until it finds a floor.
Check `end` and `grounded` in the plan against `get_ground_height` at the landing.

## The sequence

Stairs start at (0, 0, 5) and go up towards +Z, landing on height 3.0:

```
set_sequence(avatar_id="purple", end="hold", steps=[
    {"animation": "loco_walk",          "walk_to": [0, 0, 5]},
    {"animation": "idle_neutral",       "face": 0, "duration": 0.5},
    {"animation": "vert_stairsUp_loop", "walk_to": [0, 3, 8.25]},
    {"animation": "loco_walk",          "walk_to": [0, 3, 11]},
    {"animation": "idle_neutral"}])
```

- A walk as the last step keeps walking past its target (the last clip holds by looping), so end with an idle.
- Face up the stairs before climbing, so the first cycle starts on the first step.
- Use `vert_stairsUpRun_loop` for running; ladders are `vert_ladderStart` → `vert_ladderClimb_loop` (with `loops`)
  → `vert_ladderTopOut`.
