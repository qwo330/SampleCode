using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Player : MonoBehaviour
{
    [SerializeField]
    float MoveSpeed = 3f;

    [SerializeField]
    float JumpSpeed = 8f;

    [SerializeField]
    float JumpHeight = 3f;

    [SerializeField]
    float AttackInterval = 0.15f;

    [SerializeField]
    int MaxBulletCount = 50;

    Rigidbody rb;
    Animator anim;
    
    bool isJump = false;
    bool isFirstTouch = false;
    float height = 0f;
    float lastTouchTime;
    float jumpSpeed;


    public float MaxHP { get; protected set; }
    public float Hp { get; protected set; }

    protected Vector3 bulletOffset = new Vector3(0f, 1f, 0f);

    protected abstract void SetPlayer();
    public abstract void Skill_1();
    public abstract void Skill_2();
    public abstract void Skill_3();

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        SetPlayer();
        StartCoroutine(CO_Attack());
    }
    
    protected IEnumerator CO_Attack()
    {
        while (true)
        {
            if (InGameManager.Instance.CurrentState == GameState.Play)
            {
                Bullet bullet = ObjectPool.Get.GetObject(Defines.key_Bullet).GetComponent<Bullet>();
                bullet.Init(transform.position + bulletOffset);
            }
            yield return new WaitForSeconds(AttackInterval);
        }
    }

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                CheckJump();
            }
            else if (Input.GetMouseButton(0))
            {
                Move();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                anim.SetBool(Defines.key_RunL, false);
                anim.SetBool(Defines.key_RunR, false);
            }
        }
    }

    void FixedUpdate()
    {
        if (isJump)
        {
            Jump();
        }
    }

    void CheckJump()
    {
        if ((Time.time - lastTouchTime) < 0.25f && !isJump)
        {
            isJump = true;
            anim.SetBool(Defines.key_Jump, true);
            height = transform.position.y + JumpHeight;
        }
        lastTouchTime = Time.time;
    }

    void Jump()
    {
        if (transform.position.y < height)
            transform.Translate(0, JumpSpeed * Time.deltaTime, 0);
        else
            JumpSpeed = 0;
    }

    void Move()
    {
        Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float moveValue = touchPos.x - transform.position.x;

        if (Mathf.Abs(moveValue) > Vector3.kEpsilon + 0.1f)
        {
            Vector3 dir = Vector3.right;

            if (moveValue > 0)
            {
                dir = Vector3.right;
                anim.SetBool(Defines.key_RunR, true);
                anim.SetBool(Defines.key_RunL, false);
            }
            else
            {
                dir = Vector3.left;
                anim.SetBool(Defines.key_RunL, true);
                anim.SetBool(Defines.key_RunR, false);
            }

            Vector3 pos = transform.position;
            Ray ray = new Ray(pos, dir);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 0.5f))
            {
                if (hit.collider.CompareTag(Defines.key_Ground))
                    return;
            }

            float x = pos.x + (dir.x * MoveSpeed * Time.deltaTime);
            x = Mathf.Clamp(x, 0f, 8f);
            transform.position = new Vector3(x, pos.y, pos.z);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(Defines.key_Ground) || rb.velocity.y == 0)
        {
            isJump = false;
            jumpSpeed = JumpSpeed;
            anim.SetBool(Defines.key_Jump, false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(Defines.key_Enemy) || other.gameObject.CompareTag(Defines.key_EnemyBullet))
        {
            Damage(10);
        }
    }

    void Damage(float power)
    {
        Hp = Mathf.Clamp(Hp - power, 0, MaxHP);
        InGameManager.Instance.playerHitEvent.Invoke(Hp/MaxHP);
        if (Hp <= 0)
            InGameManager.Instance.GameOver();
    }
}