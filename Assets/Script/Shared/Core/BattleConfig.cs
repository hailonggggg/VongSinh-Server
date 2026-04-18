using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class BattleConfig
{
    public int MaxPlayers = 2;
    public int MaxUnitsPerPlayer = 6;
    public int MinUnitsPerPlayer = 1;
    public float TurnTimeLimit = 30f;
    public float DeploymentTime = 30f;
    public int[] AllowCharacterSelectables = new int[] { 1, 2};

}


