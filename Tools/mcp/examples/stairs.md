# Example: climbing stairs or a ladder to another storey

Someone walks to a staircase, climbs one storey and walks on at the top. The stairs and ladders are scaled so that
whole cycles climb a 3.2 m storey. Clip values are measured from the library; check the plan with `plan_sequence`.

## Find the storeys

`get_ground_height(x, z)` lists floor heights top to bottom. Probe the bottom and the top of the stairs to get
both storeys, e.g. 0.0 at the bottom and 3.2 at the landing.

## How far one cycle climbs

`vert_stairsUp_loop` rises **0.457 m** and moves 0.52 m forward per cycle (1.37 s): **7 cycles climb 3.2 m** over
3.65 m. `vert_stairsDown_loop` drops the same 0.457 m per 0.48 m forward, so 7 cycles go down one storey too;
`vert_stairsUpRun_loop` runs up 0.457 m per cycle. A ladder climbs one storey with `vert_ladderStart`,
`vert_ladderClimb_loop` with `loops: 2` and `vert_ladderTopOut`, which ends 1.3 m in from the edge; down is
`vert_ladderTopIn` (walking backwards to the edge, so turn your back to it first), `vert_ladderClimbDown_loop` with
`loops: 2` and `vert_ladderEnd`. Stairs and ladders keep these sizes for every body.

Vertical clips make the avatar airborne: it follows the clip's height, not the floor, until its feet are on or just
below a real floor again. For other storey heights, use `walk_to` with the stair clip instead of `loops`: it ends on
arrival at the target's x / z, and the height it reaches is 0.88 m per metre forward (the target's y is ignored).
Check `end` and `grounded` in the plan against `get_ground_height` at the landing.

## The sequence

Stairs start at (0, 0, 5) and go up towards +Z, landing on height 3.2:

```
set_sequence(avatar_id="purple", end="hold", steps=[
    {"animation": "loco_walk",          "walk_to": [0, 0, 5]},
    {"animation": "idle_neutral",       "face": 0, "duration": 0.5},
    {"animation": "vert_stairsUp_loop", "loops": 7},
    {"animation": "loco_walk",          "walk_to": [0, 3.2, 11]},
    {"animation": "idle_neutral"}])
```

- A walk as the last step keeps walking past its target (the last clip holds by looping), so end with an idle.
- Face up the stairs before climbing, so the first cycle starts on the first step.
