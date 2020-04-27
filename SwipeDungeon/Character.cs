using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Status))]
public class Character : MonoBehaviour
{
    public float shakeAmount, shakeTime, shakeLerp;

    [HideInInspector]
    public Status status;

    [SerializeField]
    UIStatus uiStatus;

    [SerializeField]
    Weapon weapon;

    [SerializeField]
    GameObject shieldEffect;

    int weaponCount = 0;
    Weapon defaultWeapon;
    WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    private Vector3 destination;
    private Vector3 lookDirection = Vector3.forward;
    private Animator animator;
    private CharacterHandleWeapon characterHandleWeapon;
    private List<AttackRecord> attackedList;
    
    private void Awake()
    {
        status = GetComponent<Status>();
        animator = GetComponentInChildren<Animator>();
        characterHandleWeapon = GetComponent<CharacterHandleWeapon>();
        defaultWeapon = weapon;
    }

    void Start()
    {
        Init();
    }

    void Init()
    {
        status.changeAddDefenceEvent += AddShield;
        status.changeSubDefenceEvent += SubShield;

        status.changeSubHPEvent += ShowDamage;

        status.changeAddHPEvent += uiStatus.SetHP;
        status.changeSubHPEvent += uiStatus.SetHP;

        status.changeAddAttackPowerEvent += uiStatus.SetPower;
        status.changeSubAttackPowerEvent += uiStatus.SetPower;

        ChangeWeapon(defaultWeapon);

        uiStatus.SetHP(status.HP, status.maxHP, 0);
        uiStatus.SetPower(status.AttackPower, 0);
    }

    void UpdateCharacterState(Status.StateType state)
    {
        status.ChangeState(state);
        switch (state)
        {
            case Status.StateType.Idle:
                animator.SetBool("Idle", true);
                break;
            case Status.StateType.Move:
                animator.SetTrigger("Jump");
                animator.SetBool("Idle", false);
                break;
            case Status.StateType.Attack:
                animator.SetTrigger("Attack");
                animator.SetBool("Idle", false);
                break;
            case Status.StateType.Death:
                animator.SetBool("Alive", false);
                break;
        }
    }

    public void OnDamage(float damage)
    {
        UpdateCharacterState(Status.StateType.Attack);

        status.SubHP(damage);

        if (status.stateType == Status.StateType.Death)
        {
            Die();
        }
    }

    void Die()
    {
        UpdateCharacterState(Status.StateType.Death);
        GameManager.Instance.EndGame(false);
    }

    #region Move
    public void MoveFowardTo(Vector3 direction)
    {
        if (status.GetCurrentState() == Status.StateType.Move)
            return;

        GameManager.Instance.AddSwipeCount();
        if (weapon.WeaponType == Weapon.WeaponTypes.Bow)
        {
            RaycastHit hit;
            //화살 발사 방향으로 몬스터 확인
            Ray bowRay = new Ray(transform.position, -direction);
            if (Physics.Raycast(bowRay, out hit, 6f))
            {
                if (hit.collider.gameObject.CompareTag(Defines.Tag_Monster))
                {
                    lookDirection = -direction;
                    ShowEffect("AttackEffect/" + weapon.AttackEffect, transform.position, ConverLookDirectionToRotation());
                    SoundManager.Instance.PlaySFX(weapon.UseSfx);
                    ProjectileAttack attack = Instantiate(Resources.Load<GameObject>("Effect/Bow_Projectile")).GetComponent<ProjectileAttack>();
                    attack.transform.position = transform.position + lookDirection;
                    attack.Shot(lookDirection, status.AttackPower);
                    transform.Find("MainChar").rotation = Quaternion.LookRotation(lookDirection);

                    --weaponCount;

                    if (weaponCount <= 0)
                        ChangeWeapon(defaultWeapon);
                    else
                        GameManager.Instance.gameUI.ShowWeaponCount(weaponCount);

                    return;
                }
            }
        }

        lookDirection = direction;
        Vector3 destination = SearchPath(transform.position, direction);
        transform.Find("MainChar").rotation = Quaternion.LookRotation(direction);
        MoveTo(destination);
    }

    /// <summary>
    /// direct 방향으로 도착지점까지의 충돌 오브젝트 파악 
    /// </summary>
    /// <param name="playerPos">플레이어 위치</param>
    /// <param name="direct">스와이프 방향</param>
    public Vector3 SearchPath(Vector3 playerPos, Vector3 direction)
    {
        Vector3 endPos = playerPos + direction;
        RaycastHit hit;
        MonsterController monster = null;
        // hit가 발생하지 않을때까지 raycast
        while (true)
        {
            Ray ray = new Ray(endPos + Vector3.up * 5f, Vector3.down * 10f);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.CompareTag(Defines.Tag_Monster))
                {
                    monster = hit.collider.GetComponent<MonsterController>();
                    endPos -= direction;
                    break;
                }
                if (hit.collider.gameObject.CompareTag(Defines.Tag_Wall))
                {
                    endPos -= direction;
                    break;
                }
                endPos += direction;
            }
            else
            {
                endPos -= direction;
                break;
            }
        }

        if (monster)
        {
            // moveto가 종료된 후에 플레이
            attackedList.Add(new AttackRecord(monster, Vector3.Distance(playerPos, endPos)));
        }

        return endPos;
    }

    public void MoveTo(Vector3 position)
    {
        destination = position;
        UpdateCharacterState(Status.StateType.Move);
        SoundManager.Instance.PlaySFX("Move");
        StartCoroutine(Movement());
    }

    IEnumerator Movement()
    {
        ShowEffect("JumpEffect", transform.position, Vector3.zero);
        float lerpTime = 0f;

        while ((destination - transform.position).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.Lerp(transform.position, destination, lerpTime);
            lerpTime += status.movementSpeed * Time.deltaTime;
            yield return waitForFixedUpdate;
        }

        transform.position = destination;

        if (attackedList.Count > 0)
        {
            for (int i = 0; i < attackedList.Count; ++i)
            {
                if (weapon.WeaponType == Weapon.WeaponTypes.Bow)
                    Attack(attackedList[i].monster, 0);
                else
                    Attack(attackedList[i].monster, attackedList[i].distance);
            }
            attackedList.Clear();
        }
        UpdateCharacterState(Status.StateType.Idle);
    }
    #endregion

    #region Attack
    public void Attack(MonsterController monster, float distance)
    {
        // 이펙트, 사운드 추가
        // 모션 추가
        Vector3 viewToRotation = ConverLookDirectionToRotation();

        ShowEffect("AttackEffect/" + weapon.AttackEffect, transform.position, viewToRotation);
        SoundManager.Instance.PlaySFX(weapon.UseSfx);
        float playerPower = status.AttackPower + DistancePower(distance);

        --weaponCount;

        FindObjectOfType<MapViewCamera>().Shake(shakeAmount * playerPower, shakeTime, shakeLerp);

        if (weaponCount <= 0)
            ChangeWeapon(defaultWeapon);
        else
            GameManager.Instance.gameUI.ShowWeaponCount(weaponCount);


        OnDamage(monster.status.AttackPower);
        monster.OnDamage(playerPower);
    }

    // 거리 2마다 1 증가, 최대 3 증가, 활은 적용하지 않음
    float DistancePower(float distance)
    {
        if (weapon.WeaponType == Weapon.WeaponTypes.Bow)
            return 0;

        float additivePower = Mathf.Floor(distance * 0.5f);
        return Mathf.Min(additivePower, 3);
    }
    #endregion

    public void ChangeWeapon(Weapon weapon)
    {
        this.weapon = weapon;
        weaponCount = weapon.AttackCount;
        status.AttackPower = weapon.Power;
        uiStatus.SetPower(status.AttackPower, 0);
        GameManager.Instance.gameUI.ChangeWeapon(weapon);
        characterHandleWeapon.ChangeWeapon(weapon);
    }

    public Vector3 ConverLookDirectionToRotation()
    {
        if (lookDirection == Vector3.forward)
        {
            return Vector3.up;
        }
        else if (lookDirection == Vector3.right)
        {
            return Vector3.right * 90f + Vector3.up * 90f;
        }
        else if (lookDirection == Vector3.back)
        {
            return Vector3.right * 90f + Vector3.up * 180f;
        }
        else if (lookDirection == Vector3.left)
        {
            return Vector3.right * 90f + Vector3.up * 270f;
        }

        return Vector3.zero;
    }

    public void ShowEffect(string effectName, Vector3 position, Vector3 rotation)
    {
        string path = "Effect/" + effectName;
        GameObject prefab = Resources.Load(path) as GameObject;
        GameObject go = Instantiate(prefab);

        if (position != Vector3.zero)
            go.transform.position = position;
        else
        {
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
        }

        if (rotation != Vector3.zero)
            go.transform.localEulerAngles = rotation;

        Effect effect = go.GetComponent<Effect>();
        effect.ShowEffect();
    }

    public void ShowTextEffect(float damage)
    {
        var position = uiStatus.GetHPPosition();
        string path = "Effect/TextEffect";
        GameObject prefab = Resources.Load(path) as GameObject;
        GameObject go = Instantiate(prefab);
        go.transform.position = position;

        TextEffect effect = go.GetComponent<TextEffect>();
        effect.ShowTextEffect(damage);
    }

    public void ShowDamage(float hp, float max, float amount)
    {
        ShowTextEffect(amount);
    }
   
    public void AddShield(float defence, float amount)
    {
        shieldEffect.SetActive(true);
    }

    public void SubShield(float defence, float amount)
    {
        shieldEffect.SetActive(false);
    }
}
