"""MCP server that lets an LLM spawn and animate avatars in the running VRoxel app.

Thin layer: every tool forwards to the McpBridge component in Unity (see vroxel_bridge.py).
Run with: python Tools/mcp/vroxel_mcp.py   (stdio transport, started by the MCP client)
"""

from typing import Annotated, Literal, Optional

from mcp.server.mcpserver import MCPServer
from pydantic import BaseModel, Field

from vroxel_bridge import call

mcp = MCPServer(
    "vroxel",
    instructions=(
        "Controls avatars in VRoxel, a VR tool for architectural design. Units are meters, Y is up, "
        "yaw is in degrees clockwise around Y seen from above (0 faces +Z, 90 faces +X). VRoxel must be in Play mode.\n"
        "Clips: call list_animations first; names follow category_actionName. cycle_motion is how far one clip "
        "cycle moves the avatar in its own frame (right / forward in meters, turn in degrees). "
        "get_animation_motion gives the trajectory over time.\n"
        "Floors: grounded avatars stand on floors and imported geometry found by raycast and walk through VRoxel "
        "blocks (they sculpt paths). Use get_ground_height to find storeys before placing avatars. Clips marked "
        "vertical (stairs, ladders, steps) move the avatar up or down and make it airborne: it then keeps its height, "
        "e.g. walking on at the top of stairs that don't exist, until its feet reach a real floor again.\n"
        "Sequences: set_sequence scripts multi-step behaviour from an anchor pose. Check it with plan_sequence first. "
        "By default the last step holds; only if the user asks, use end='loop' (repeat) or end='return' (jump back to "
        "the start and wait). For loops, aim for a small loop_gap. restart_sequences replays the scene.\n"
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
    color_set: Annotated[Optional[int], Field(description="Index of a VRoxel colour set", ge=0)] = None,
    animation: Annotated[Optional[str], Field(description="Clip to start immediately")] = None,
    name: Annotated[Optional[str], Field(description="Display name in the scene")] = None,
) -> dict:
    """Spawn an avatar and return its id, which the other tools use to address it. Its spawn pose is its anchor."""
    return call(
        "spawn_avatar",
        position=position,
        yaw=yaw,
        on_floor=on_floor,
        height_cm=height_cm,
        color_set=color_set,
        animation=animation,
        name=name,
    )


@mcp.tool()
def play_animation(
    avatar_id: Annotated[str, Field(description="Id returned by spawn_avatar, e.g. avatar_1")],
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
    avatar_id: Annotated[Optional[str], Field(description="Plan from this avatar's anchor or current pose")] = None,
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
    avatar_id: Annotated[str, Field(description="Id returned by spawn_avatar")],
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
    avatar_id: Annotated[Optional[str], Field(description="Only this avatar; default restarts all, in sync")] = None,
) -> list:
    """Replay sequences from their anchors, e.g. to review a scene from the beginning."""
    return call("restart_sequences", avatar_id=avatar_id)


@mcp.tool()
def list_avatars() -> list:
    """List the avatars spawned through this server, with position, facing, current clip and sequence."""
    return call("list_avatars")


@mcp.tool()
def remove_avatar(avatar_id: Annotated[str, Field(description="Id returned by spawn_avatar")]) -> dict:
    """Delete an avatar from the scene."""
    return call("remove_avatar", avatar_id=avatar_id)


@mcp.tool()
def undo() -> dict:
    """Undo the last spawn, removal or sequence change made through these tools."""
    return call("undo")


@mcp.tool()
def redo() -> dict:
    """Redo the last undone change."""
    return call("redo")


if __name__ == "__main__":
    mcp.run()
