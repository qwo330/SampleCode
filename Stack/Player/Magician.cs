using UnityEngine;

public class Magician : Player
{
    protected override void SetPlayer()
    {
        MaxHP = 100f;
        Hp = MaxHP;
    }

    public override void Skill_1()
    {
        if (InGameManager.Instance.UseManaStone(1))
        {
            Debug.Log("Magician Skill 1");
        }
    }

    public override void Skill_2()
    {
        if (InGameManager.Instance.UseManaStone(2))
        {
            Debug.Log("Magician Skill 2");
        }
    }

    public override void Skill_3()
    {
        if (InGameManager.Instance.UseManaStone(3))
        {
            Debug.Log("Magician Skill 3");
        }
    }
}
