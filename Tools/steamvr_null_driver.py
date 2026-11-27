"""Switch SteamVR between the real headset and its null driver (a virtual headset for testing without hardware).

    python Tools/steamvr_null_driver.py on       use the virtual headset
    python Tools/steamvr_null_driver.py off      back to the real headset
    python Tools/steamvr_null_driver.py status   show which one is set

SteamVR has to be closed: it writes its settings back when it exits. The null driver gives a still head at the
floor and no controllers; move the player with teleport_player. Only the keys below are changed, the rest of the
settings stay as they are.
"""
import json
import os
import subprocess
import sys
import winreg

NULL_KEYS = {"requireHmd": False, "forcedDriver": "null", "activateMultipleDrivers": True}
STEAMVR_PROCESSES = ("vrserver.exe", "vrmonitor.exe", "vrcompositor.exe", "vrdashboard.exe")


def steam_path():
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam") as key:
            return os.path.normpath(winreg.QueryValueEx(key, "SteamPath")[0])
    except OSError:
        return r"C:\Program Files (x86)\Steam"


def paths():
    steam = steam_path()
    settings = os.path.join(steam, "config", "steamvr.vrsettings")
    driver = os.path.join(steam, "steamapps", "common", "SteamVR", "drivers", "null", "resources", "settings",
                          "default.vrsettings")
    return settings, driver


def steamvr_running():
    out = subprocess.run(["tasklist"], capture_output=True, text=True).stdout.lower()
    return [p for p in STEAMVR_PROCESSES if p in out]


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def save(path, data, indent):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=indent)


def status(settings, driver):
    steamvr = load(settings).get("steamvr", {})
    forced = steamvr.get("forcedDriver") == "null"
    enabled = load(driver).get("driver_null", {}).get("enable", False)
    if forced and enabled:
        return "on (virtual headset)"
    if not forced and not enabled:
        return "off (real headset)"
    return "partly set (forcedDriver null: %s, null driver enabled: %s); run on or off" % (forced, enabled)


def main():
    command = sys.argv[1] if len(sys.argv) > 1 else "status"
    settings, driver = paths()
    if command == "status":
        print(status(settings, driver))
        return 0
    if command not in ("on", "off"):
        print(__doc__)
        return 2
    running = steamvr_running()
    if running:
        print("Close SteamVR first (running: %s); it would overwrite the settings when it exits." % ", ".join(running))
        return 1

    on = command == "on"
    data = load(driver)
    data.setdefault("driver_null", {})["enable"] = on
    save(driver, data, 4)

    data = load(settings)
    steamvr = data.setdefault("steamvr", {})
    for key, value in NULL_KEYS.items():
        if on:
            steamvr[key] = value
        else:
            steamvr.pop(key, None)
    save(settings, data, 3)

    print("SteamVR null driver", status(settings, driver))
    return 0


if __name__ == "__main__":
    sys.exit(main())
