# Example: walking through a door

Someone walks up to a door, opens it, walks through and continues. Clip values are measured from the library;
check the plan with `plan_sequence`. Tested in VRoxel without door geometry.

## The clip

`interact_openDoorAndWalkThrough` is the only room-door clip: the avatar reaches for the handle, pushes the door
open and walks through, moving about **2.2 m forward** in 4.1 s. There is no separate door-opening clip and no
door geometry moves; it is the gesture only.

Start it about 0.5 m in front of the door, facing it. It ends about 1.7 m past the door.

## The sequence

Door in a wall at z = 3, the avatar comes from z = 0 and goes on to z = 7:

```
set_sequence(avatar_id="orange", end="hold", steps=[
    {"animation": "loco_walk",                       "walk_to": [1, 0, 2.5]},
    {"animation": "idle_neutral",                    "face": 0, "duration": 0.4},
    {"animation": "interact_openDoorAndWalkThrough"},
    {"animation": "loco_walk",                       "walk_to": [1, 0, 7]},
    {"animation": "idle_neutral"}])
```

- The clip sidesteps about 0.4 m to the left while opening and comes back as it walks through; leave room.
- A walk as the last step keeps walking past its target (the last clip holds by looping), so end with an idle.

- Avatars walk through VRoxel blocks, so a wall made of blocks doesn't stop them; aim the walk at the door gap.
- Cabinets: `interact_openCabinet`, then e.g. `reach_shelfMid` or a pause, then `interact_closeCabinet`, all on
  the spot, about an arm's length from the cabinet and facing it.
- Lifts and light switches: `interact_pressButton`, standing about 0.4 m from the wall, facing it.
