using System.Diagnostics;
using ExitGames.Client.Photon.StructWrapping;
using UnityEngine;

public class PassiveCondition
{
    public PassiveConditionType Type;
    public int EnemyGetHitRequired;
    public ThreatLevel ThreatLevel;

    public virtual bool Evaluated(Unit owner, PassiveContext context)
    {
        switch (Type)
        {
            case PassiveConditionType.OwnerIsActor:
                if (context.TryGetEvent<ActionPerformedEvent>(out var actionEvent))
                    return actionEvent.Actor == owner;
                if (context.TryGetEvent<SkillUsedEvent>(out var skillEvent))
                    return skillEvent.Owner == owner;
                if (context.TryGetEvent<TurnStartEvent>(out var turnStartEvent))
                    return turnStartEvent.Unit == owner;
                if (context.TryGetEvent<AttackHitEvent>(out var hitEvent))
                    return hitEvent.Attacker == owner;
                break;

            case PassiveConditionType.UseYuanSkill:
                return context.TryGetEvent<SkillUsedEvent>(out var e) && e.Skill.IsType<YuanSkill>();

            case PassiveConditionType.HitEnemyCountAtLeast:
                return context.TryGetEvent<AttackHitEvent>(out hitEvent)
                    && hitEvent.EnemyGetHitCount >= EnemyGetHitRequired;
        }
        return false;
    }
}
