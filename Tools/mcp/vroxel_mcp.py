"""MCP server that lets an LLM spawn and animate avatars in the running VRoxel app.

Thin layer: every tool forwards to the McpBridge component in Unity (see vroxel_bridge.py).
Run with: python Tools/mcp/vroxel_mcp.py   (stdio transport, started by the MCP client)
"""

from typing import Annotated, Literal, Optional

from mcp.server.mcpserver import MCPServer
from mcp.server.mcpserver.exceptions import ToolError
from pydantic import BaseModel, Field

import vroxel_bridge


def call(command, **args):
    # Only ToolError messages reach the model; other exceptions arrive as a bare "Error executing tool"
    try:
        return vroxel_bridge.call(command, **args)
    except vroxel_bridge.BridgeError as e:
        raise ToolError(str(e)) from e

mcp = MCPServer(
    "vroxel",
    instructions=(
        "Controls avatars in VRoxel, a VR tool for architectural design. Units are meters, Y is up, "
        "yaw is in degrees clockwise around Y seen from above (0 faces +Z, 90 faces +X). VRoxel must be in Play mode.\n"
        "Clips: call list_animations first; names follow category_actionName. cycle_motion is how far one clip "
        "cycle moves the avatar in its own frame (right / forward in meters, turn in degrees). "
        "get_animation_motion gives the trajectory over time. gesture clips are full-body and standing: playing one "
        "on a sitting avatar stands it up, so seated avatars use the sit_ loops (talking, laughing, clapping) instead.\n"
        "Floors: grounded avatars stand on floors and imported geometry found by raycast and walk through VRoxel "
        "blocks (they sculpt paths). Use get_ground_height to find storeys before placing avatars. Clips marked "
        "vertical (stairs, ladders, steps) move the avatar up or down and make it airborne: it then keeps its height, "
        "e.g. walking on at the top of stairs that don't exist, until its feet reach a real floor again.\n"
        "Sequences: set_sequence scripts multi-step behaviour from an anchor pose. Check it with plan_sequence first. "
        "By default the last step holds; only if the user asks, use end='loop' (repeat) or end='return' (jump back to "
        "the start and wait). For loops, aim for a small loop_gap. restart_sequences replays the scene.\n"
        "Colours: every avatar gets a random colour of its own at spawn (list_avatars shows it). Refer to avatars by "
        "colour when talking to the user ('the teal avatar'), and pass the colour as avatar_id when the user names one. "
        "set_avatar_color changes it; no two avatars share a colour.\n"
        "Saving: save_animation stores the avatars with their colours, sizes and sequences under a name; "
        "load_animation brings a saved one back (replacing the current avatars unless replace=false) and replays it "
        "from the start. Check list_saved_animations before saving so you don't overwrite the user's work.\n"
        "undo / redo only affect changes made through these tools, never the user's own edits."
    ),
)


class SequenceStep(BaseModel):
    animation: str = Field(description="Clip name from list_animations")
    duration: Optional[float] = Field(None, ge=0, description="Seconds before the next step; default plays the clip once (or `loops` times)")
    loops: Optional[int] = Field(None, ge=1, description="Repetitions of a looping clip when no duration is given")
    walk_to: Optional[list[float]] = Field(
        None, min_length=3, max_length=3,
        description="[x, y, z] target; the avatar steers there and the step ends on arrival. Needs a clip with speed > 0. "
                    "y is ignored: height comes from the floor, or from the clip for vertical clips",
    )
    face: Optional[float] = Field(None, description="Turn to this yaw (degrees) during the step")
    fade: Optional[float] = Field(None, ge=0, description="Crossfade into this step in seconds")
    speed: Optional[float] = Field(None, ge=0, le=10, description="Playback speed multiplier")


Steps = Annotated[list[SequenceStep], Field(min_length=1, description="Clips to play in order")]
End = Annotated[
    Literal["hold", "loop", "return"],
    Field(description="After the last step: 'hold' keeps its pose (default), 'loop' starts over from the anchor, "
                      "'return' jumps back to the anchor and waits. Use loop / return only when asked"),
]
Start = Annotated[
    Literal["anchor", "current"],
    Field(description="'anchor' replays from the avatar's start pose (spawn pose by default); 'current' starts where it stands now"),
]
# Mirrors AvatarColors.Palette in Unity
Color = Literal["red", "blue", "mint", "canary", "pink", "orange", "green", "purple", "teal", "sand"]
AvatarId = Annotated[str, Field(description="Avatar id (avatar_1) or its colour (teal)")]

StartPosition = Annotated[Optional[list[float]], Field(min_length=3, max_length=3, description="New anchor [x, y, z]")]
StartYaw = Annotated[Optional[float], Field(description="New anchor yaw in degrees")]


def _sequence_args(steps, end, start, start_position, start_yaw):
    return dict(
        steps=[step.model_dump(exclude_none=True) for step in steps],
        end=end,
        start=start,
        start_position=start_position,
        start_yaw=start_yaw,
    )


@mcp.tool()
def list_animations(
    category: Annotated[Optional[str], Field(description="Only this category, e.g. idle, loco, sit, gesture")] = None,
) -> list:
    """List the clips avatars can play: loop flag, length in seconds, travel speed in m/s, whether it climbs
    (vertical) and cycle_motion."""
    return call("list_animations", category=category)


@mcp.tool()
def get_animation_motion(
    animation: Annotated[str, Field(description="Clip name from list_animations")],
    interval: Annotated[float, Field(ge=0.05, description="Seconds between samples")] = 0.25,
) -> dict:
    """Root motion trajectory of one clip cycle: right / forward / up offset in meters and turn in degrees over time,
    relative to the avatar's pose at clip start. Use it for precise placement, e.g. how far sitting down moves back."""
    return call("get_animation_motion", animation=animation, interval=interval)


@mcp.tool()
def get_ground_height(x: float, z: float) -> dict:
    """Floor heights at (x, z), top to bottom. Several values mean several storeys or overhangs."""
    return call("get_ground_height", x=x, z=z)


@mcp.tool()
def get_assembly_bounds() -> dict:
    """Bounding box of the user's block assembly in world space: min / max / center / size in meters, block count
    and grid spacing. Use it to place avatars in or around the design."""
    return call("get_assembly_bounds")


@mcp.tool()
def spawn_avatar(
    position: Annotated[list[float], Field(description="[x, y, z] in meters; snaps to a floor within 0.5 m above or 1.5 m below y", min_length=3, max_length=3)],
    yaw: Annotated[float, Field(description="Facing direction in degrees around Y")] = 0,
    on_floor: Annotated[bool, Field(description="False places the avatar exactly at y, airborne, e.g. on a floor that doesn't exist")] = True,
    height_cm: Annotated[Optional[float], Field(description="Body height in cm (50-250); default is the model's 181 cm", ge=50, le=250)] = None,
    color: Annotated[Optional[Color], Field(description="Colour no other avatar has; default is a random free one")] = None,
    animation: Annotated[Optional[str], Field(description="Clip to start immediately")] = None,
    name: Annotated[Optional[str], Field(description="Display name in the scene")] = None,
) -> dict:
    """Spawn an avatar and return its id and colour; the other tools address it by either. Its spawn pose is its anchor."""
    return call(
        "spawn_avatar",
        position=position,
        yaw=yaw,
        on_floor=on_floor,
        height_cm=height_cm,
        color=color,
        animation=animation,
        name=name,
    )


@mcp.tool()
def play_animation(
    avatar_id: AvatarId,
    animation: Annotated[str, Field(description="Clip name from list_animations")],
    fade: Annotated[Optional[float], Field(description="Crossfade in seconds", ge=0)] = None,
    speed: Annotated[Optional[float], Field(description="Playback speed multiplier", ge=0, le=10)] = None,
) -> dict:
    """Crossfade an avatar into a single clip from where it stands now, replacing its sequence and anchor.
    One-shot clips hold their last frame when they end."""
    return call("play_animation", avatar_id=avatar_id, animation=animation, fade=fade, speed=speed)


@mcp.tool()
def plan_sequence(
    steps: Steps,
    avatar_id: Annotated[Optional[str], Field(description="Plan from this avatar's anchor or current pose (id or colour)")] = None,
    end: End = "hold",
    start: Start = "anchor",
    start_position: StartPosition = None,
    start_yaw: StartYaw = None,
) -> dict:
    """Dry run of set_sequence: start / end time, position, yaw and grounded state of every step, plus loop_gap
    (distance from the last step's end back to the anchor). Changes nothing. walk_to steps are approximated as
    straight walks."""
    return call("plan_sequence", avatar_id=avatar_id, **_sequence_args(steps, end, start, start_position, start_yaw))


@mcp.tool()
def set_sequence(
    avatar_id: AvatarId,
    steps: Steps,
    end: End = "hold",
    start: Start = "anchor",
    start_position: StartPosition = None,
    start_yaw: StartYaw = None,
) -> dict:
    """Give an avatar a scripted sequence, e.g. walk to a door, open it, then idle, and return its plan."""
    return call("set_sequence", avatar_id=avatar_id, **_sequence_args(steps, end, start, start_position, start_yaw))


@mcp.tool()
def restart_sequences(
    avatar_id: Annotated[Optional[str], Field(description="Only this avatar (id or colour); default restarts all, in sync")] = None,
) -> list:
    """Replay sequences from their anchors, e.g. to review a scene from the beginning."""
    return call("restart_sequences", avatar_id=avatar_id)


@mcp.tool()
def list_avatars() -> list:
    """List the avatars spawned through this server, with colour, position, facing, current clip and sequence."""
    return call("list_avatars")


@mcp.tool()
def remove_avatar(avatar_id: AvatarId) -> dict:
    """Delete an avatar from the scene."""
    return call("remove_avatar", avatar_id=avatar_id)


@mcp.tool()
def set_avatar_color(
    avatar_id: AvatarId,
    color: Annotated[Color, Field(description="New colour; must not be worn by another avatar")],
) -> dict:
    """Change an avatar's colour, e.g. when the user wants to tell avatars apart differently."""
    return call("set_avatar_color", avatar_id=avatar_id, color=color)


AnimationName = Annotated[str, Field(description="1-64 letters, digits, spaces, '-' or '_'", min_length=1, max_length=64)]


@mcp.tool()
def save_animation(
    name: AnimationName,
    description: Annotated[Optional[str], Field(description="What happens in it, shown by list_saved_animations")] = None,
    avatars: Annotated[Optional[list[str]], Field(description="Avatar ids or colours to save; default all")] = None,
    overwrite: Annotated[bool, Field(description="Replace a saved animation with the same name")] = False,
) -> dict:
    """Save the avatars (colour, body size, anchor) and their sequences as a named animation in VRoxel's
    AppData folder. Clips are stored by name."""
    return call("save_animation", name=name, description=description, avatars=avatars, overwrite=overwrite)


@mcp.tool()
def list_saved_animations() -> list:
    """Saved animations with description, save time and their avatars (name, colour, number of steps)."""
    return call("list_saved_animations")


@mcp.tool()
def load_animation(
    name: AnimationName,
    replace: Annotated[bool, Field(description="Remove the current avatars first (default); false adds the saved ones")] = True,
) -> dict:
    """Spawn a saved animation's avatars and replay their sequences from the start, in sync. One undo reverts the
    whole load. Warnings list clips missing from the library and colours that had to change."""
    return call("load_animation", name=name, replace=replace)


@mcp.tool()
def undo() -> dict:
    """Undo the last spawn, removal, sequence or colour change, or animation load, made through these tools."""
    return call("undo")


@mcp.tool()
def redo() -> dict:
    """Redo the last undone change."""
    return call("redo")


if __name__ == "__main__":
    mcp.run()
