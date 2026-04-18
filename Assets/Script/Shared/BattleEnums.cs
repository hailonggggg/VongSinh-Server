using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script.Shared
{
    public enum BattlePhase
    {
        Deployment,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat,
        WaitingForPlayers,
        Draw
    }

    public enum CommandType
    {
        PlaceUnit,
        RemoveUnit,
        SwapUnits,
        SetReady,

        // Combat commands
        Move,
        Attack,
        UseSkill,
        EndTurn,
        Surrender
    }
}
