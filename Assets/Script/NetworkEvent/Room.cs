using System;
using System.Collections.Generic;

[Serializable]
public class Room
{
    public int RoomId;
    public string Name;
    public List<BattlePlayerInfo> Players;
    public int MaxPlayers;
    public int MapIndexSelected = 0;

}
