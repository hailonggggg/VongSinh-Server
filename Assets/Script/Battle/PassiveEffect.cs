using System;
using System.Collections.Generic;
using UnityEngine;

public class PassiveEffect
{
    public PassiveEffectType Type;
    public int Amount;
    public int Range;
    public bool IncludeOwner;

    public void Execute(Unit owner, PassiveContext context)
    {
        switch (Type)
        {
            case PassiveEffectType.GainOwnerSP:
                owner.PlusSkillPoint(Amount);
                break;

            case PassiveEffectType.GainTeamAP:
                context.BattleContext.ActionPointSystem.PlusPoint(Amount);
                break;

            case PassiveEffectType.ExtendYuanPressure:
                context.BattleContext.YuanPressureSystem.AdjustValue(Amount);
                ServerNetwork.Instance.SendToClient(owner.Owner.Client,
                    Service.YuanPressureUpdate(context.BattleContext.YuanPressureSystem.Current));
                break;

            case PassiveEffectType.HealAlliesAroundOwner:
                if (IncludeOwner) owner.PlusHp(Amount);
                List<Vector3Int> validHealCells = context.BattleContext.Map.GetSquareTiles(owner.CurrentGridPosition, Range);
                foreach (var ally in context.BattleContext.Allies)
                {
                    if (!validHealCells.Contains(ally.CurrentGridPosition)) continue;
                    ally.PlusHp(Amount);
                }
                break;
        }
    }
}
