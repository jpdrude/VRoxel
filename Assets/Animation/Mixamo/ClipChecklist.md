# Mixamo Clip Checklist

Target library of ~100 clips for LLM-driven avatars. Tick clips off as you download them.

## Download settings (every clip)

- Format: **FBX for Unity (.fbx)**
- Skin: **Without Skin**
- Frames per second: **30**
- Keyframe Reduction: **none**
- **In Place: leave unticked** (root motion is extracted / ignored in Unity)
- Save into `Assets/Animation/Mixamo/Inbox~/` and **keep Mixamo's file name** (it is the animation title)
- In Unity, open **VRoxel > Mixamo Inbox** and assign each download to its name below. The window moves it to
  `Clips/`, records the exact Mixamo title and a search link in `MixamoManifest.csv`, and ticks it here
- Character: always **X Bot**. The reference `Reference/xbot_tpose.fbx` is X Bot downloaded with skin, in T-pose, without animation
- `Clips/`, `Reference/`, `Inbox~/` and the generated `MixamoLibrary.asset` are gitignored: Mixamo data must not be
  redistributed, so every developer downloads them locally

## Reproducing someone else's set

`MixamoManifest.csv` (committed) lists every clip with its exact Mixamo title. Search each title on mixamo.com, download
it with the settings above into `Inbox~/`, and assign it in the Mixamo Inbox window; titles from the manifest are
pre-selected. The library builder logs which manifest clips are still missing.

## Naming

`category_actionName[_variant][_loop|_once].fbx`

- `_loop` / `_once` override the automatic loop setting (see `MixamoClipPostprocessor`)
- Loop: `idle`, `loco`, `gesture`, `activity` and any name containing `idle` loop; names containing `start`, `stop`, `To<Upper>`, `getUp`, `lieDown` are one-shot
- On the spot: `idle`, `gesture`, and every loop outside `loco` / `turn` / `vert` unless the name contains `walk`, `run`, `jog`, `push` or `pull`. Everything else moves the avatar (root motion)
- `turn` clips rotate the avatar; all others keep its facing direction
- `vert` clips keep their height change as root motion, so the avatar climbs; all others keep the root at floor height
- Reversed transitions are generated, not downloaded: for every `category_aToB` whose `category_bToA` isn't in
  `Clips/`, and for `lieDown` / `getUp` and `pickUp` / `putDown`, the library builder plays the clip backwards into
  `Generated/` (gitignored). A downloaded clip of the same name replaces the generated one

## How to read the list

- **Search** is a search term for mixamo.com. Mixamo's exact titles vary and change; pick the closest match and check it in the preview
- ★ = core set, download these first (28 clips)
- Chains that belong together are marked with →, e.g. sit down → sit idle → stand up. Download complete chains
- Every transition into an idle state needs one out of it. Where Mixamo only has one direction, name the clip
  `category_aToB` and the way back is generated, so avatars never get stuck in a pose

---

## idle: standing idles (6)

- [x] ★ `idle_neutral` | Search "Idle" / "Breathing Idle" | calm baseline pose, the default for everything
- [x] `idle_standRelaxed` | "Standing Idle" / "Relaxed Idle" | relaxed variation of neutral
- [x] ★ `idle_weightShift` | "Weight Shift" | subtle variation of neutral
- [x] ★ `idle_lookAround` | "Looking Around" | orientation in a space
- [x] ★ `idle_phone` | "Texting" / "Texting While Standing" | waiting person
- [x] `idle_bored` | "Bored" |

## loco: locomotion (13)

- [x] ★ `loco_walk` | "Walking" | main walk cycle
- [x] ★ `loco_walkSlow` | "Slow Walk" / "Old Man Walk" | elderly / leisurely pace
- [x] `loco_walkFast` | "Fast Walk" / "Brisk Walk" | commuting
- [x] `loco_jogging` | "Jogging" |
- [x] ★ `loco_run` | "Running" |
- [x] `loco_catwalk` | "Catwalk Walk" |
- [x] `loco_startWalking` | "Start Walking" | idle → walk
- [x] ★ `loco_stopWalking` | "Stop Walking" / "Walking To Stop" | walk → idle
- [x] `loco_walkBackwards` | "Walking Backwards" |
- [x] `loco_strafeLeft` | "Left Strafe Walking" | side-stepping in narrow spaces
- [x] `loco_strafeRight` | "Right Strafe Walking" |
- [x] `loco_walkPhone` | "Texting While Walking" | distracted pedestrian
- [x] `loco_crouchWalk` | "Crouched Walking" | under low ceilings

## turn: turning on the spot and walking in circles (7)

- [x] ★ `turn_left90` | "Left Turn" / "Standing Turn Left 90" |
- [x] ★ `turn_right90` | "Right Turn" / "Standing Turn Right 90" |
- [x] ★ `turn_180` | "Turn 180" / "Standing Turn 180" |
- [x] `turn_walkInCircleLeft_loop` | "Walking In Circle" / "Walk In A Circle" | one cycle turns roughly 30°; turns while walking, set the angle with `loops`
- [x] `turn_walkInCircleRight_loop` | "Walking In Circle" (mirrored) / "Walk In A Circle" | as above, turning right
- [x] `turn_jogInCircleLeft_loop` | "Jog In Circle" with **Mirror** ticked | turning left
- [x] `turn_jogInCircleRight_loop` | "Jog In Circle" | turning right

## vert: stairs, ladders, steps, jumps (9)

- [x] ★ `vert_stairsUp_loop` | "Ascending Stairs" / "Walking Up Stairs" | loop it along a staircase
- [x] ★ `vert_stairsDown_loop` | "Descending Stairs" / "Walking Down Stairs" | Mixamo pins the feet to a flat floor; the library builder bakes a StairDescentFix that lowers the feet at runtime
- [x] `vert_stairsUpRun_loop` | "Running Up Stairs" |
- [x] `vert_ladderStart` | "Start Climbing Ladder" / "Climbing Ladder Start" | standing to the first rungs → `vert_ladderClimb_loop`
- [x] `vert_ladderClimb_loop` | "Climbing Ladder" |
- [x] `vert_ladderTopOut` | "Climbing To Top" / "Climb Ladder End" | leave ladder at the top
- [x] `vert_stepDown` | "Step Down" / "Jump Down (low)" |
- [x] `vert_jumpOver` | "Jump Over" / "Running Jump" | gap or obstacle
- [x] `vert_jumpStationary` | "Jump" / "Jumping Up" / "Standing Jump" | jump on the spot, lands where it took off

## sit: chairs and benches (9)

- [x] ★ `sit_standToSit` | "Stand To Sit" | → `sit_idle`
- [x] ★ `sit_idle` | "Sitting Idle" | loops by name ("idle")
- [x] ★ `sit_sitToStand` | "Sit To Stand" / "Standing Up (from chair)" | `sit_idle` →
- [x] ★ `sit_talking_loop` | "Sitting Talking" | meetings, cafés
- [x] `sit_laughing_loop` | "Sitting Laughing" |
- [x] ★ `sit_typing_loop` | "Typing" / "Sitting Typing" | desk work
- [x] `sit_writing_loop` | "Writing" / "Sitting Writing" |
- [x] `sit_clap_loop` | "Sitting Clap" | audience
- [x] `sit_disbelief_loop` | "Sitting Disapproval" / "Sitting Rubbing Head" | variation

## floor: kneeling, floor sitting (4)

- [x] `floor_standToKneel` | "Kneeling Down" / "Stand To Kneel" | → `floor_kneelIdle`; `floor_kneelToStand` is generated
- [x] `floor_kneelIdle` | "Kneeling Idle" |
- [x] `floor_sitGroundIdle` | "Sitting On Ground Idle" |
- [x] `floor_sitGroundToStand` | "Standing Up" / "Sitting To Standing" | `floor_sitGroundIdle` →; `floor_standToSitGround` is generated

## bed: lying in bed (2)

Lying happens on a bed, not the floor. The avatar sits on the bed edge first (`sit_standToSit` at mattress height), lies down from there and sits up again with the generated `bed_lieToSit` (Mixamo has no getting out of bed). Place it on the mattress surface; pick clips whose body stays level with the hips (no reach down to a floor).

- [x] ★ `bed_sitToLie` | "Laying Down" / "Lying Down" / "Sitting To Laying" | from sitting on the bed edge → `bed_lyingIdle` → `bed_lieToSit` (generated) → `sit_sitToStand`
- [x] ★ `bed_lyingIdle` | "Sleeping Idle" / "Laying Idle" / "Lying Idle" | loops by name

## reach: picking up, reaching, pushing (5)

- [x] ★ `reach_pickUpFloor` | "Picking Up" / "Pick Up Object" | `reach_putDownFloor` is generated
- [x] `reach_shelfMid` | "Reaching" / "Reaching Out" | shelf at breast height; Mixamo has no high or low shelf reach and no pick up from a table
- [x] `reach_push_loop` | "Pushing" / "Push Object" | walking behind a cart or trolley
- [x] `reach_pull_loop` | "Pulling" / "Pull Object" |
- [x] `reach_throw` | "Throw" / "Toss" |

## interact: using the building (6)

- [x] ★ `interact_openDoorAndWalkThrough` | "Opening Door" | the only room door clip: opens it and walks through
- [x] ★ `interact_pressButton` | "Button Pushing" / "Pressing Button" | elevator, light switch
- [x] ★ `interact_leanWall_loop` | "Leaning On Wall" / "Wall Lean" |
- [x] `interact_leanRailing_loop` | "Leaning On Railing" / "Leaning Forward" | balcony, bridge
- [x] `interact_openCabinet` | "Opening Cabinet" / "Open Cabinet" | cabinet door → `interact_closeCabinet`
- [x] `interact_closeCabinet` | "Closing Cabinet" / "Close Cabinet" |

## gesture: standing social gestures (12)

Pick the **standing** version of every gesture: the clips are full-body, so a gesture always plays standing on the spot.
Sitting avatars use the `sit_` loops instead. Upper-body layering over walking / sitting via a mask may come later.

- [x] `gesture_waving` | "Waving" |
- [x] ★ `gesture_point` | "Pointing" / "Pointing Forward" | directing attention
- [x] ★ `gesture_talking` | "Talking" | conversation loop
- [x] ★ `gesture_listening` | "Listening" / "Idle Listening" / "Agreeing" | the other side of the conversation
- [x] ★ `gesture_handshake_once` | "Shaking Hands" |
- [x] `gesture_nodYes_once` | "Head Nod Yes" |
- [x] `gesture_shakeNo_once` | "Shaking Head No" |
- [x] `gesture_explaining` | "Explaining" / "Talking With Hands" | presentation
- [x] `gesture_clapping` | "Clapping" |
- [x] `gesture_cheering` | "Cheering" |
- [x] `gesture_arguing` | "Arguing" |
- [x] `gesture_shrug_once` | "Shrugging" |

## activity: longer task motions (6)

- [x] `activity_stretching_loop` | "Stretching" / "Arm Stretching" |
- [x] `activity_exercise_loop` | "Jumping Jacks" / "Push Up" | gym / outdoor
- [x] `activity_dancing_loop` | "Dancing" | event space
- [x] `activity_bartending_loop` | "Bartending" | behind a bar or counter
- [x] `activity_guitar_loop` | "Guitar Playing" | music, event space
- [x] `activity_speedbag_loop` | "Speedbag" | gym, boxing training

---

Total: 79
