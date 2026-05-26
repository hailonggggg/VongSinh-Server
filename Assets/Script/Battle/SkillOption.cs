using UnityEngine;


public class SkillOption
{
    public SkillEffectType EffectType;
    public int Value;
    public int TurnApply;

    public void Execute(Unit owner)
    {
        TurnApply--;
        if (EffectType == SkillEffectType.Bleed)
        {
            owner.TakeDamage(Value);
        }
    }

    public void FromJson()
    {

    }
}
