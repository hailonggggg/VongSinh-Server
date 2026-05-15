using System;

[Serializable]
public class BattleBanPickInfo
{
    public int LeftSidePlayerId;
    public bool HasBanPhase = false;
    public int MaxUnitsPerPlayer = 0;
    public int MapIndexSelected = 0;
    public int[] AllowCharacterSelectables;
    public BattlePlayerInfo[] Players;
}
