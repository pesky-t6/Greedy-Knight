using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TreeEditor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Enemy : MonoBehaviour
{
    [Header("Movement:")]
    [SerializeField] private float minRunSpeed;
    [SerializeField] private float maxRunSpeed;
    [SerializeField] private float minJumpForce;
    [SerializeField] private float maxJumpForce;
    [SerializeField] private float stoppingDistance;
    [SerializeField] private float retreatDistance;
    [SerializeField] private float watchingDistance;
    [SerializeField] private float retreatThreshold;
    [SerializeField] private float retreatTime;
    [SerializeField] private float retreatCDTime;
    [Header("Attack Properties:")]
    [SerializeField] private float health;
    [SerializeField] private float alertDistance;
    [SerializeField] private float alertTime;
    [SerializeField] private float heal;
    [SerializeField] private float knockbackAmount = 1;
    [SerializeField] private float knockbackRange;
    [SerializeField] private float knockbackDealt;
    [SerializeField] private float stunTime;
    [SerializeField] private float[] attackRange;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float[] attackCD;
    [SerializeField] private float attackCDRange;
    [SerializeField] private float[] attackDamage;
    [SerializeField] private float attackDamageRange;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool[] canAttack;
    [SerializeField] private bool[] isHeavy;
    [Header("References and Checks:")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform ledgeCheck;
    [SerializeField] private Vector3 wallCheckSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private float ledgeCheckRadius = 1f;
    [SerializeField] private Transform poofPos;
    [SerializeField] private GameObject poof;
    [SerializeField] private GameObject damageNum;
    [SerializeField] private GameObject heavyFlash;
    [SerializeField] private Transform heavyPoint;
    private TrailRenderer heavyTrail;
    private Transform target;
    private Rigidbody2D rb;
    private Transform player;
    private Player_Life playerLife;
    private bool canTakeDamage = true;
    private bool hasLineOfSight = false;
    private float distance = 0;
    private bool alerted;
    public bool canMove = true;
    private Animator anim;
    private bool isJumping = false;
    private float runSpeed;
    private float jumpForce;
    private bool retreating = false;
    private bool canRetreat = true;
    public bool watching = false;
    private bool attacking = false;
    private bool canTurn = true;
    private int currentAttackIndex;

    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float flashTime = 0.25f;

    private SpriteRenderer[] sp;
    private Material[] mat;
    private float walkSpeed;

    private Coroutine DamageFlashCoroutine;
    private Player_Attack playerAttack;
    private DamageNumber damageNumHolder;
    private GameManager gameManager;

    //Scaling the size of the character
    private float x;
    private float y;

    private float freezeTime = 0.05f;

    private enum MovementState
    {
        idle, running, jumping, falling, walking, prepare
    }
    MovementState state;


    // Start is called before the first frame update
    void Start()
    {
        sp = GetComponentsInChildren<SpriteRenderer>();
        anim = GetComponent<Animator>();
        target = GameObject.FindGameObjectWithTag("Target").transform;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        playerLife = player.GetComponent<Player_Life>();
        rb = GetComponent<Rigidbody2D>();
        if (heavyPoint != null)
        {
            heavyTrail = heavyPoint.GetComponent<TrailRenderer>();
        }

        //Calculate randomized speeds
        runSpeed = Random.Range(minRunSpeed, maxRunSpeed);
        jumpForce = Random.Range(minJumpForce, maxJumpForce);
        walkSpeed = runSpeed / 2;

        //Calculate randomized distances
        stoppingDistance = Random.Range(stoppingDistance - 0.05f, stoppingDistance + 0.05f);
        retreatDistance = Random.Range(retreatDistance - 0.05f, retreatDistance);
        watchingDistance = Random.Range(watchingDistance - 1f, watchingDistance + 2.5f);

        float rando = Random.Range(-0.1f, 0.1f);
        x = transform.localScale.x + rando;
        y = transform.localScale.y + rando;
        transform.localScale = new Vector3(x, y, transform.localScale.z);
        health *= x;

        for (int i = 0; i < canAttack.Length; i++)
        {
            canAttack[i] = true;
        }

        Init();
    }

    //Get the material of each sprite renderer
    private void Init()
    {
        mat = new Material[sp.Length];

        for (int i = 0; i < sp.Length; i++)
        {
            mat[i] = sp[i].material;
        }
    }

    private void CallDamageFlash()
    {
        DamageFlashCoroutine = StartCoroutine(Flasher());
    }

    //Damage flash
    private IEnumerator Flasher()
    {
        for (int i = 0;i < mat.Length; i++)
        {
            mat[i].SetColor("_FlashColor", _flashColor);
        }

        float currentFlashAmount = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < flashTime)
        {
            elapsedTime += Time.deltaTime;

            currentFlashAmount = Mathf.Lerp(1f, 0f, (elapsedTime / flashTime));

            for (int i = 0; i < mat.Length; i++)
            {
                mat[i].SetFloat("_FlashAmount", currentFlashAmount);
            }

            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        target.position = new Vector2(player.position.x, transform.position.y);
        distance = player.position.x - rb.position.x;

        if (health <= 0)
        {
            Death();
        }

        if (attacking) { canTurn = false; canMove = false; }
        else { canTurn = true; canMove = true; }

        //Flipping the enemy to face player if they can turn
        if (canTurn)
        {
            if (distance >= 0.01f)
            {
                transform.localScale = new Vector3(x, y, 1f);
            }
            else if (distance <= 0.01f)
            {
                transform.localScale = new Vector3(-x, y, 1f);
            }
        }

        if (distance < alertDistance && !alerted)
        {
            alerted = true;
        }

        //Moving the enemy
        if (canMove)
        {
           //Watching the player
           if (watching && alerted)
            {
                if (Mathf.Abs(distance) > watchingDistance + 0.2)
                {
                    transform.position = Vector2.MoveTowards(transform.position, target.position, walkSpeed * Time.deltaTime);
                    state = MovementState.running;
                }
                //If too close walk backwards
                else if (Mathf.Abs(distance) < watchingDistance - 0.2)
                {
                    transform.position = Vector2.MoveTowards(transform.position, target.position, -walkSpeed * Time.deltaTime);
                    state = MovementState.walking;
                }
                //Stop moving if too close
                else if (Mathf.Abs(distance) < watchingDistance + 0.2 && Mathf.Abs(distance) > watchingDistance - 0.2)
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                    state = MovementState.idle;
                }
                else
                {
                    state = MovementState.idle;
                }
            }
            //Jump if looking at a wall to try and mantle
            else if (IsWalled() && IsLedged() && !isJumping)
            {
                Jump();
                isJumping = true;
            }
            //Retreat
            else if (!retreating && health < retreatThreshold && canRetreat)
            {
                Retreat();
            }
            else if (retreating)
            {
                transform.position = Vector2.MoveTowards(transform.position, target.position, -walkSpeed * Time.deltaTime);
                state = MovementState.walking;
            }
            else if (Mathf.Abs(distance) > stoppingDistance && alerted)
            {
                transform.position = Vector2.MoveTowards(transform.position, target.position, runSpeed * Time.deltaTime);
                state = MovementState.running;
            }
            //If too close walk backwards
            else if (Mathf.Abs(distance) < retreatDistance && alerted)
            {
                transform.position = Vector2.MoveTowards(transform.position, target.position, -runSpeed * Time.deltaTime);
                state = MovementState.walking;
            }
            //Stop moving if too close
            else if (Mathf.Abs(distance) < stoppingDistance && Mathf.Abs(distance) > retreatDistance && alerted)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                state = MovementState.prepare;
                if (!attacking)
                {
                    StartAttack();
                }
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, target.position, walkSpeed * Time.deltaTime);
                state = MovementState.walking;
            }

            //Mantle
            if (IsWalled() && !IsLedged())
            {
                Mantle();
            }
        }

        if (!hasLineOfSight)
        {
            StartCoroutine(AlertTimer());
        }

        UpdateAnimationState();
    }

    //Attack method for moveset
    private void StartAttack()
    {
        if (canMove)
        {
            if (canAttack[0])
            {
                attacking = true;
                canAttack[0] = false;
                canMove = false;
                anim.SetTrigger("attack1");
                currentAttackIndex = 0;
            }
            else if (canAttack.Length > 1 && canAttack[1])
            {
                attacking = true;
                canAttack[1] = false;
                canMove = false;
                anim.SetTrigger("attack2");
                currentAttackIndex = 1;
            }
            else if (canAttack.Length > 2 && canAttack[2])
            {
                attacking = true;
                canAttack[2] = false;
                canMove = false;
                anim.SetTrigger("attack3");
                currentAttackIndex = 2;
            }
            else if (canAttack.Length > 3 && canAttack[3])
            {
                attacking = true;
                canAttack[3] = false;
                canMove = false;
                anim.SetTrigger("attack4");
                currentAttackIndex = 3;
            }
        }
    }

    //Many attack function to be called in anim event
    private void Attack(int i)
    {
        StartCoroutine(AttackCD(i));
        //Gather all hit targets and store in array
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRange[i], playerLayer);
        if (hitTargets != null)
        {
            float value = Random.Range(attackDamage[i] - attackDamageRange, attackDamage[i] + attackDamageRange);
            //Go through each target until either shield or player
            foreach (Collider2D hitTarget in hitTargets)
            {
                RaycastHit2D ray = Physics2D.Raycast(attackPoint.position, player.position - transform.position, 1000f, ~enemyLayer);
                if (hitTarget.gameObject.CompareTag("Shield"))
                {
                    playerAttack = player.GetComponent<Player_Attack>();
                    if (playerAttack != null)
                    {
                        playerAttack.Blocked(ray.point, value/2);
                        playerLife.Knockback(transform.position, knockbackDealt);
                    }
                    canMove = true;
                    return;
                }
                //Check if hit the player
                else if (hitTarget.gameObject.CompareTag("Player"))
                {
                    playerAttack = player.GetComponent<Player_Attack>();
                    if (ray.collider != null && ray.collider.CompareTag("Shield") && !isHeavy[i])
                    { 
                        playerAttack.Blocked(ray.point, value/2);
                        playerLife.Knockback(transform.position, knockbackDealt);
                        canMove = true;
                        return;
                    }
                    //Deal damage
                    playerLife = player.GetComponent<Player_Life>();
                    if (playerLife != null)
                    {
                        if (isHeavy[i])
                        {
                            playerLife.Hit(value, Color.red, transform.position, knockbackDealt);
                        }
                        else
                        {
                            playerLife.Hit(value, Color.white, transform.position, knockbackDealt);
                        }
                        CallFreezeFrame();
                    }
                    canMove = true;
                    return;
                }
            }
        }
        canMove = true;
    }

    //Easier access to the method (dont need to use start coroutine)
    public void Hit(float damage, Color color, Vector3 pos, float value, int state)
    {
        StartCoroutine(HitCoroutine(damage, color, pos, value, state));
    }

    //Method for freeze framing and taking damage and knockback
    private IEnumerator HitCoroutine(float damage, Color color, Vector3 pos, float value, int state)
    {
        anim.speed = 0f;
        yield return new WaitForSeconds(freezeTime);
        anim.speed = 1f;
        TakeDamage(damage, color);
        Knockback(pos, value, state);
    }

    //Easy way to say StartCoroutine
    private void CallFreezeFrame()
    {
        StartCoroutine(FreezeFrame());
    }

    //Freeze the character when hitting an enemy
    private IEnumerator FreezeFrame()
    {
        anim.speed = 0f;
        yield return new WaitForSeconds(freezeTime);
        anim.speed = 1f;
    }

    private IEnumerator AttackCD(int i)
    {
        canAttack[i] = false;
        float value = Random.Range(attackCD[i] - attackCDRange, attackCD[i] + attackCDRange);
        yield return new WaitForSeconds(value);
        canAttack[i] = true;
    }

    //Check if ground check transform is intersecting with the ground
    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.15f, groundLayer);
    }
    //Check if touching wall
    private bool IsWalled()
    {
        return Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, groundLayer);
    }
    //Check if touching ledge
    private bool IsLedged()
    {
        return Physics2D.OverlapCircle(ledgeCheck.position, ledgeCheckRadius, groundLayer);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void FixedUpdate()
    {
        RaycastHit2D ray = Physics2D.Raycast(transform.position, player.position - transform.position, 1000f, ~enemyLayer);

        if (ray.collider != null)
        {
            if (ray.collider.CompareTag("Player") || ray.collider.CompareTag("Shield"))
            {
                hasLineOfSight = true;
            }
            else { hasLineOfSight = false; }
        }

        if (IsGrounded())
        {
            isJumping = false;
        }
    }

    //Stun when hit
    private IEnumerator Stun()
    {
        //Only stun if the attack isnt a heavy
        if (!isHeavy[currentAttackIndex])
        {
            canMove = false;
            //Prevents an enemy to be stuck attacking
            if (attacking)
            {
                StartCoroutine(AttackCD(currentAttackIndex));
            }
            attacking = false;
            //anim.SetTrigger("stun");
            yield return new WaitForSeconds(stunTime);
            state = MovementState.idle;
            canMove = true;
        }
    }

    //Calls to make enemy retreat
    public void Retreat()
    {
        StartCoroutine(RetreatTimer());
    }

    //How long the enemy retreats for
    private IEnumerator RetreatTimer()
    {
        retreating = true;
        yield return new WaitForSeconds(retreatTime);
        StartCoroutine(RetreatCD());
        retreating = false;
    }

    //Cooldown until enemy can retreat again
    private IEnumerator RetreatCD()
    {
        canRetreat = false;
        yield return new WaitForSeconds(retreatCDTime);
        canRetreat = true;
    }

    //Pretty self explanatory
    private void UpdateAnimationState()
    {
        if (rb.velocity.x > 0f)
        {
            state = MovementState.running;
        }
        else if (rb.velocity.x < 0f)
        {
            state = MovementState.running;
        }

        if (rb.velocity.y > 0.25f)
        {
            state = MovementState.jumping;
        }
        else if (rb.velocity.y < -0.25f)
        {
            state = MovementState.falling;
        }
        anim.SetInteger("state", (int)state);
    }

    private void Mantle()
    {
        rb.velocity = new Vector2(0f, 5f);
    }

    //How long until the enemy loses agro
    private IEnumerator AlertTimer()
    {
        yield return new WaitForSeconds(alertTime);
        if (!hasLineOfSight)
        { alerted = false; }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(ledgeCheck.position, ledgeCheckRadius);
        Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
        Gizmos.DrawWireSphere(groundCheck.position, 0.125f);
        Gizmos.DrawWireSphere(attackPoint.position, attackRange[0]);
    }

    //Take damage method
    public void TakeDamage(float damage, Color damageColor)
    {
        if (canTakeDamage)
        {
            health -= damage;
            CallDamageFlash();
            StartCoroutine(Stun());
            damageNumHolder = Instantiate(damageNum, rb.transform.position, Quaternion.identity).GetComponent<DamageNumber>();
            damageNumHolder.value = damage;
            damageNumHolder.color = damageColor;
            StartCoroutine(Grace());
        }
    }

    //Begins the death of the enemy
    private void Death()
    {
        RemoveObjects();
        if (!watching)
        {
            GameManager gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            gameManager.currentAttackers--;
        }
        rb.bodyType = RigidbodyType2D.Dynamic;
        anim.Play("Death");
        playerLife = GameObject.FindGameObjectWithTag("Player").GetComponent<Player_Life>();
        playerLife.Heal(heal);
        for (int i = 0; i < canAttack.Length; i++)
        {
            canAttack[i] = false;
        }
    }

    private void RemoveObjects()
    {
        foreach (Transform child in gameObject.transform)
        {
            if (!child.CompareTag("EnemyBody"))
            {
                Pickup pickup = child.GetComponent<Pickup>();
                pickup.DetachWeapon();
            }
        }
    }

    //Controls the value and execution of knockback
    public void Knockback(Vector3 pos, float value)
    {
        //Calculates unit vector and randomizes knockback
        value = Random.Range(knockbackAmount - knockbackRange, knockbackAmount + knockbackRange) * knockbackAmount;
        Vector3 dirVector = new Vector3(transform.position.x, transform.position.y, 0f) - new Vector3(pos.x, pos.y, 0f);
        Vector3 unitVector = (dirVector) / (dirVector.magnitude);
        rb.velocity = new Vector2(rb.velocity.x + unitVector.x * value, rb.velocity.y + unitVector.y * value);
    }

    public void Knockback(Vector3 pos, float value, int type)
    {
        value = Random.Range(knockbackAmount - knockbackRange, knockbackAmount + knockbackRange) * knockbackAmount;
        if (type == 2)
        {
            rb.velocity = new Vector2(0, value);
        }
        else if (type == 3)
        {
            rb.velocity = new Vector2(0, -value);
        }
        else
        {
            Vector3 dirVector = new Vector3(transform.position.x, transform.position.y, 0f) - new Vector3(pos.x, pos.y, 0f);
            Vector3 unitVector = (dirVector) / (dirVector.magnitude);
            rb.velocity = new Vector2(rb.velocity.x + unitVector.x * value, rb.velocity.y + unitVector.y * value);
        }
    }

    //Smoke when dead
    private void Poof()
    {
        Instantiate(poof, poofPos.transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    //Prevents getting hit multiple times at once
    private IEnumerator Grace()
    {
        canTakeDamage = false;
        yield return new WaitForSeconds(0.1f);
        canTakeDamage = true;
    }

    //Moves the enemey forward
    private void PushForward()
    {
        rb.velocity = new Vector2(transform.localScale.x, rb.velocity.y);
    }

    //Sets the attacking bool to false
    private void AttackDone()
    {
        attacking = false;
        if (heavyTrail != null && heavyTrail.emitting)
        {
            heavyTrail.emitting = false;
        }
    }

    //Red flash to indicate heavy attack
    private void HeavyFlash()
    {
        Instantiate(heavyFlash, heavyPoint.position, Quaternion.identity, heavyTrail.transform);
        heavyTrail.emitting = true;
    }
}
