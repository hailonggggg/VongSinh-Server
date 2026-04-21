using System;

[Serializable]
public class BattleBanPickInfo
{
    public bool IsLocalPlayerOnLeftSide = true;
    public bool HasBanPhase = false;
    public int MaxUnitsPerPlayer = 0;
    public int MapIndexSelected = 0;
    public int[] AllowCharacterSelectables;
    public BattlePlayerInfo[] Players;
}
