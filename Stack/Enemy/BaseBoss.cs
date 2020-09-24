using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseBoss : MonoBehaviour
{
    protected int hp, maxHP;

    public abstract void SetEnemy(int level);
    protected abstract IEnumerator CO_Attack();

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {

    }

    void Dead()
    {
        InGameManager.Instance.GameClearEvent.Invoke();
    }
}
