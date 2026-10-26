# Mixamo Clip Checklist

Target library of ~100 clips for LLM-driven avatars. Tick clips off as you download them.

## Download settings (every clip)

- Format: **FBX for Unity (.fbx)**
- Skin: **Without Skin**
- Frames per second: **30**
- Keyframe Reduction: **none**
- **In Place: leave unticked** (root motion is extracted / ignored in Unity)
- Save to `Assets/Animation/Mixamo/Clips/` as the file name given below (`.fbx`)

## Naming

`category_actionName[_variant][_loop|_once].fbx`

- `_loop` / `_once` override the automatic loop setting (see `MixamoClipPostprocessor`)
- Loop: `idle`, `loco`, `gesture`, `activity` and any name containing `idle` loop; names containing `start`, `stop`, `To<Upper>`, `getUp`, `lieDown` are one-shot
- On the spot: `idle`, `gesture`, and every loop outside `loco` / `turn` / `vert` unless the name contains `walk`, `run`, `jog`, `push` or `pull`. Everything else moves the avatar (root motion)
- `turn` clips rotate the avatar; all others keep its facing direction

## How to read the list

- **Search** is a search term for mixamo.com. Mixamo's exact titles vary and change; pick the closest match and check it in the preview
- ★ = core set, download these first (34 clips)
- Chains that belong together are marked with →, e.g. sit down → sit idle → stand up. Download complete chains

---

## idle: standing idles (8)

- [ ] ★ `idle_neutral` | Search "Idle" / "Breathing Idle" | calm baseline pose, the default for everything
- [ ] ★ `idle_weightShift` | "Weight Shift" | subtle variation of neutral
- [ ] ★ `idle_lookAround` | "Looking Around" | orientation in a space
- [ ] `idle_armsCrossed` | "Crossed Arms" / "Standing Arms Crossed" |
- [ ] `idle_handsInPockets` | "Hands In Pockets" / "Casual Idle" |
- [ ] ★ `idle_phone` | "Texting" / "Texting While Standing" | waiting person
- [ ] `idle_bored` | "Bored" |
- [ ] `idle_thinking` | "Thinking" | looking at something, considering

## loco: locomotion (13)

- [ ] ★ `loco_walk` | "Walking" | main walk cycle
- [ ] ★ `loco_walkSlow` | "Slow Walk" / "Old Man Walk" | elderly / leisurely pace
- [ ] `loco_walkFast` | "Fast Walk" / "Brisk Walk" | commuting
- [x] `loco_jogging` | "Jogging" |
- [ ] ★ `loco_run` | "Running" |
- [x] `loco_catwalk` | "Catwalk Walk" |
- [x] `loco_startWalking` | "Start Walking" | idle → walk
- [ ] ★ `loco_stopWalking` | "Stop Walking" / "Walking To Stop" | walk → idle
- [ ] `loco_walkBackwards` | "Walking Backwards" |
- [ ] `loco_strafeLeft` | "Left Strafe Walking" | side-stepping in narrow spaces
- [ ] `loco_strafeRight` | "Right Strafe Walking" |
- [ ] `loco_walkPhone` | "Texting While Walking" | distracted pedestrian
- [ ] `loco_crouchWalk` | "Crouched Walking" | under low ceilings

## turn: turning on the spot (8)

- [ ] ★ `turn_left90` | "Left Turn" / "Standing Turn Left 90" |
- [ ] ★ `turn_right90` | "Right Turn" / "Standing Turn Right 90" |
- [ ] ★ `turn_180` | "Turn 180" / "Standing Turn 180" |
- [ ] `turn_walkLeft90` | "Walking Turn Left" / "Left Turn 90 (walking)" | turning without stopping
- [ ] `turn_walkRight90` | "Walking Turn Right" |
- [ ] `turn_walk180` | "Walking Turn 180" |
- [x] `turn_jogInCircleRight_loop` | "Jog In Circle" |
- [ ] `turn_lookBehind` | "Look Over Shoulder" / "Looking Behind" | upper body only

## vert: stairs, ladders, steps (8)

- [ ] ★ `vert_stairsUp_loop` | "Ascending Stairs" / "Walking Up Stairs" | loop it along a staircase
- [ ] ★ `vert_stairsDown_loop` | "Descending Stairs" / "Walking Down Stairs" |
- [ ] `vert_stairsUpRun_loop` | "Running Up Stairs" |
- [ ] `vert_ladderClimb_loop` | "Climbing Ladder" |
- [ ] `vert_ladderTopOut` | "Climbing To Top" / "Climb Ladder End" | leave ladder at the top
- [ ] `vert_stepUp` | "Step Up" / "Climbing (low obstacle)" | single high step / platform
- [ ] `vert_stepDown` | "Step Down" / "Jump Down (low)" |
- [ ] `vert_jumpOver` | "Jump Over" / "Running Jump" | gap or obstacle

## sit: chairs and benches (14)

- [ ] ★ `sit_standToSit` | "Stand To Sit" | → `sit_idle`
- [ ] ★ `sit_idle` | "Sitting Idle" | loops by name ("idle")
- [ ] ★ `sit_sitToStand` | "Sit To Stand" / "Standing Up (from chair)" | `sit_idle` →
- [ ] `sit_standToSit_low` | "Sitting Down (low)" / "Stand To Sit" on low seat | low bench / sofa, if a distinct clip exists
- [ ] ★ `sit_talking_loop` | "Sitting Talking" | meetings, cafés
- [ ] `sit_laughing_loop` | "Sitting Laughing" |
- [ ] ★ `sit_typing_loop` | "Typing" / "Sitting Typing" | desk work
- [ ] `sit_writing_loop` | "Writing" / "Sitting Writing" |
- [ ] `sit_drinking_loop` | "Drinking" / "Sitting Drinking" |
- [ ] `sit_eating_loop` | "Eating" / "Sitting Eating" |
- [ ] `sit_phone_loop` | "Sitting Texting" / "Sitting Phone" |
- [ ] `sit_leanBack_loop` | "Sitting Relaxed" / "Sitting Leaning Back" |
- [ ] `sit_clap_loop` | "Sitting Clap" | audience
- [ ] `sit_disbelief_loop` | "Sitting Disapproval" / "Sitting Rubbing Head" | variation

## floor: crouching, kneeling, floor sitting, lying (10)

- [ ] ★ `floor_standToCrouch` | "Standing To Crouched" | → `floor_crouchIdle`
- [ ] ★ `floor_crouchIdle` | "Crouching Idle" / "Crouch Idle" | loops by name
- [ ] ★ `floor_crouchToStand` | "Crouch To Stand" |
- [ ] `floor_standToKneel` | "Kneeling Down" / "Stand To Kneel" |
- [ ] `floor_kneelIdle` | "Kneeling Idle" |
- [ ] `floor_standToSitGround` | "Sitting Down (ground)" / "Stand To Sit On Floor" |
- [ ] `floor_sitGroundIdle` | "Sitting On Ground Idle" |
- [ ] `floor_lieDown` | "Laying Down" / "Lying Down" | → `floor_lyingIdle`
- [ ] `floor_lyingIdle` | "Laying Idle" / "Sleeping Idle" | loops by name
- [ ] `floor_getUp` | "Getting Up" / "Stand Up" |

## reach: picking up, placing, carrying (12)

- [ ] ★ `reach_pickUpFloor` | "Picking Up" / "Pick Up Object" |
- [ ] ★ `reach_putDownFloor` | "Put Down" / "Placing Object" |
- [ ] ★ `reach_pickUpTable` | "Pick Up (waist height)" / "Grabbing" | counter / table height
- [ ] `reach_shelfHigh` | "Reaching Up" / "Grab High" | top shelf, overhead cabinet
- [ ] `reach_shelfLow` | "Reaching Low" / "Pick Up Low" | bottom shelf
- [ ] ★ `reach_liftBox` | "Lifting" / "Box Lift" | → `reach_carryBoxWalk`
- [ ] ★ `reach_carryBoxWalk_loop` | "Walking With Box" / "Carry Walk" |
- [ ] `reach_carryBoxIdle_loop` | "Box Idle" / "Holding Box" |
- [ ] `reach_dropBox` | "Box Put Down" / "Putting Down Box" | end of carry chain
- [ ] `reach_push_loop` | "Pushing" / "Push Object" | cart, heavy door
- [ ] `reach_pull_loop` | "Pulling" / "Pull Object" |
- [ ] `reach_throw` | "Throw" / "Toss" |

## interact: using the building (10)

- [x] `interact_openDoorAndWalkThrough` | "Opening Door" |
- [ ] ★ `interact_openDoor` | "Open Door" / "Opening A Door" | without walking through
- [ ] ★ `interact_pressButton` | "Button Pushing" / "Pressing Button" | elevator, light switch
- [ ] ★ `interact_leanWall_loop` | "Leaning On Wall" / "Wall Lean" |
- [ ] `interact_leanRailing_loop` | "Leaning On Railing" / "Leaning Forward" | balcony, bridge
- [ ] `interact_lookOutWindow_loop` | "Looking Forward Idle" / "Looking Out" |
- [ ] `interact_openDrawer` | "Opening Drawer" / "Open Cabinet" |
- [ ] `interact_typingStanding_loop` | "Standing Typing" / "Using Computer Standing" | counter, kiosk
- [ ] `interact_keyLock` | "Unlocking Door" / "Using Key" |
- [ ] `interact_sitAtCounter` | "Sitting At Bar" / "Bar Stool Sit" | high seat

## gesture: upper-body social gestures (14)

These can later be layered over walking / sitting via an upper-body mask.

- [x] `gesture_waving` | "Waving" |
- [ ] ★ `gesture_point` | "Pointing" / "Pointing Forward" | directing attention
- [ ] ★ `gesture_talking` | "Talking" | conversation loop
- [ ] ★ `gesture_listening` | "Listening" / "Idle Listening" / "Agreeing" | the other side of the conversation
- [ ] ★ `gesture_handshake_once` | "Shaking Hands" |
- [ ] `gesture_nodYes_once` | "Head Nod Yes" |
- [ ] `gesture_shakeNo_once` | "Shaking Head No" |
- [ ] `gesture_explaining` | "Explaining" / "Talking With Hands" | presentation
- [ ] `gesture_clapping` | "Clapping" |
- [ ] `gesture_cheering` | "Cheering" |
- [ ] `gesture_arguing` | "Arguing" |
- [ ] `gesture_beckon_once` | "Come Here" / "Waving Over" | "follow me"
- [ ] `gesture_checkWatch_once` | "Looking At Watch" / "Checking Watch" |
- [ ] `gesture_shrug_once` | "Shrugging" |

## activity: longer task motions (10)

- [ ] ★ `activity_cleaning_loop` | "Mopping" / "Sweeping" / "Cleaning" | maintenance scenarios
- [ ] `activity_wipingTable_loop` | "Wiping" / "Cleaning Table" |
- [ ] `activity_cooking_loop` | "Cooking" / "Stirring" | kitchen
- [ ] `activity_counterWork_loop` | "Working At Counter" / "Standing Work" |
- [ ] `activity_hammering_loop` | "Hammering" | workshop / construction
- [ ] `activity_stretching_loop` | "Stretching" / "Arm Stretching" |
- [ ] `activity_exercise_loop` | "Jumping Jacks" / "Push Up" | gym / outdoor
- [ ] `activity_dancing_loop` | "Dancing" | event space
- [ ] `activity_playing_loop` | "Playing" / "Jumping" | play areas, children-like motion
- [ ] `activity_painting_loop` | "Painting" / "Drawing On Wall" |

---

Total: 107 (6 already downloaded)
