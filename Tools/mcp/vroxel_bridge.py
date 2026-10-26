"""Client for the McpBridge TCP server inside the running VRoxel app (standard library only)."""

import itertools
import json
import os
import socket

HOST = "127.0.0.1"
PORT = int(os.environ.get("VROXEL_BRIDGE_PORT", "8765"))
TIMEOUT = 35  # a little longer than McpBridge's main-thread timeout

_ids = itertools.count(1)


class BridgeError(Exception):
    """VRoxel rejected the command or could not be reached."""


def call(command, **args):
    """Send one command to VRoxel and return its result. Arguments that are None are left out."""
    request = {
        "id": next(_ids),
        "command": command,
        "args": {key: value for key, value in args.items() if value is not None},
    }

    # One connection per call, so restarting Play mode never leaves a stale socket behind
    try:
        with socket.create_connection((HOST, PORT), timeout=TIMEOUT) as connection:
            connection.sendall((json.dumps(request) + "\n").encode("utf-8"))
            with connection.makefile("r", encoding="utf-8") as reader:
                line = reader.readline()
    except ConnectionRefusedError:
        raise BridgeError(
            f"VRoxel is not reachable on {HOST}:{PORT}. Start Play mode in a scene with an McpBridge component."
        ) from None
    except socket.timeout:
        raise BridgeError("VRoxel did not answer in time. Is Play mode paused?") from None

    if not line:
        raise BridgeError("VRoxel closed the connection without answering")

    response = json.loads(line)
    if not response.get("ok"):
        raise BridgeError(response.get("error", "Unknown error"))

    return response.get("result")
