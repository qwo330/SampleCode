using UnityEngine;

public enum MagicianSkill
{
    Teleport,
    FireBall,
    Explosion,
}

public class Magician : Player
{
    protected override void SetPlayer()
    {
        MaxHP = 100f;
        Hp = MaxHP;
    }

    public override void Skill_1()
    {
        if (InGameManager.Instance.UseManaCube(1))
        {
            Debug.Log("Magician Skill 1");
            skills[0].PlaySkill();
        }
    }

    public override void Skill_2()
    {
        if (InGameManager.Instance.UseManaCube(2))
        {
            Debug.Log("Magician Skill 2");
            GameObject skill = ObjectPool.Get.GetObject(MagicianSkill.FireBall.ToString());
            skills[1].PlaySkill(skill);
        }
    }

    public override void Skill_3()
    {
        if (InGameManager.Instance.UseManaCube(3))
        {
            Debug.Log("Magician Skill 3");
            GameObject skill = ObjectPool.Get.GetObject(MagicianSkill.Explosion.ToString());
            skills[2].PlaySkill(skill);
        }
    }
}
