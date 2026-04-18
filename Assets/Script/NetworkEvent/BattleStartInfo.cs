using System;
using System.Collections.Generic;

[Serializable]
public class BattleBanPickInfo
{
    public bool IsLocalPlayerOnLeftSide = true;
    public int[] AllowCharacterSelectables;
}
