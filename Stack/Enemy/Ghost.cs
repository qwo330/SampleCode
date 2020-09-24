using System.Collections;
using UnityEngine;

public class Ghost : BaseEnemy, IAttackable
{
    Coroutine attackRoutine;

    public override void SetEnemy(int level)
    {
        maxHP = 1 + (int)(level * 0.5f);
        hp = maxHP;
        moveSpeed = 3f;
        attackInterval = 3f;

        attackRoutine = StartCoroutine(CO_Attack());
    }

    public IEnumerator CO_Attack()
    {
        while (InGameManager.Instance.CheckPlaying())
        {
            yield return new WaitForSeconds(attackInterval);

            GameObject bullet = ObjectPool.Get.GetObject(Defines.key_EnemyBullet);
            bullet.transform.position = new Vector3(transform.position.x, transform.position.y - 1f, 0f);
        }
    }

    protected override void Dead()
    {
        base.Dead();
        StopCoroutine(attackRoutine);
    }
}