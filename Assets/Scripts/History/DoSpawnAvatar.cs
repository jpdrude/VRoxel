using UnityEngine;

// Spawning and removing an avatar both toggle it, so the same object comes back on undo / redo
public class DoSpawnAvatar : DoAction
{
    GameObject avatar;
    bool spawned;

    public DoSpawnAvatar(DoType type, GameObject _avatar) : base(type)
    {
        avatar = _avatar;
        spawned = type == DoType.SpawnAvatar;
    }

    public override bool Undo()
    {
        if (avatar == null) return false;

        avatar.SetActive(!spawned);

        return true;
    }

    public override bool Redo()
    {
        if (avatar == null) return false;

        avatar.SetActive(spawned);

        return true;
    }
}
