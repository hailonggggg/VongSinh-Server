using UnityEngine;


public class SkillOption
{
    public StatusEffect StatusEffect;
    public int Value;
    public int TurnApply;

    public void Execute(Unit owner)
    {
        TurnApply--;
        if (StatusEffect.EffectType == SkillEffectType.Bleed)
        {
            owner.TakeDamage(Value);
        }
    }
}
