using Fusion;

public class BattlePlayer
{
    public PlayerRef PlayerRef { get; }
    public string Name { get; }
    public bool IsSceneLoaded { get; private set; }

    public BattlePlayer(PlayerRef playerRef, string name)
    {
        PlayerRef = playerRef;
        Name = name;
    }

    public bool MarkSceneLoaded()
    {
        if (IsSceneLoaded)
        {
            return false;
        }

        IsSceneLoaded = true;
        return true;
    }
}
