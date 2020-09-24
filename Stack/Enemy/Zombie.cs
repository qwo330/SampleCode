using System.Collections;
using UnityEngine;

public class Zombie : BaseEnemy
{
    public override void SetEnemy(int level)
    {
        maxHP = 3 + level;
        hp = maxHP;
        moveSpeed = 3f;
    }
}