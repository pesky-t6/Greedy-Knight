using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.UI;

public class Player_Attack : MonoBehaviour
{
    private Rigidbody2D rb;
    public Transform attackPoint;
    [SerializeField] private LayerMask hitLayer;

    private Pickup pick;
    private Bat bat;
    private Enemy enemy;

    //Script refs
    private Player_Inventory inv;
    private Player_Movement pm;
    private Player_Life pl;
    private Heavy_Charges hc;
    public InputActionReference attackInput;
    private Animator anim;

    [SerializeField] private Player_Look lk;
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject[] weapons;

    [SerializeField] private GameObject fShield;
    [SerializeField] private GameObject sShield;
    [SerializeField] private GameObject sparkPoint;

    [SerializeField] private GameObject Player;
    [SerializeField] private SpriteRenderer LHand;
    [SerializeField] private SpriteRenderer RHand;

    [SerializeField] private GameObject sparks;
    [SerializeField] private GameObject debris;

    [SerializeField] public TrailRenderer heavyTrail;
    [SerializeField] public Transform heavyPoint;

    private GameObject debrisObj;

    private bool falling = false;
    public bool dashAttack = false;
    public bool attacking = false;
    public bool canChain;
    public bool canFinish;
    public bool swordThrown;
    private string hitTag;

    //Heavy management
    public bool heavying = false;

    //Block management
    private float blockTime = 0;
    private bool canBlock = true;
    public bool blocking = false;

    //Time management
    private float defaultTime = 1;
    private float defaultFixedTime = 0.02f;
    private float slowedTime = 0.25f;
    private float slowedFixedTime = 0.005f;

    private Color color;

    private string lightButton = "Fire1";
    private string heavyButton = null;
    private string shieldButton = "Fire2";

    private float timeHolding = 0f;
    private bool isHoldingAttack = false;
    private float heavyThreshold = 1f;

    public List<AttackSO> comboList;
    float lastTimeClicked;
    float lastTimeComboEnd;
    int comboIndex;

    //Attack ranges
    private float[] attackRange =
    {
        0.25f,  //opener
        0.5f,   //chain
        0.4f,  //finisher
        1f,   //dash    
        0.75f,  //jump
        1.2f,   //fall
        1.3f      //heavyFall
    };

    //Attack numbers
    private int[] attack = {
        15, //opener
        15, //chain
        20,  //finisher
        10,  //dash
        17,  //jump
        14,  //fall
        14   //heavyFall
    };

    private int[] knockback = { 3, 1, 2, 4 };

    private enum AttackType { opener, chain, finisher, dash, jump, fall, heavyFall };
    private int state;

    private void Start()
    {
        inv = GetComponent<Player_Inventory>();
        pm = GetComponent<Player_Movement>();
        pl = GetComponent<Player_Life>();
        rb = GetComponent<Rigidbody2D>();
        hc = GetComponent<Heavy_Charges>();
        anim = pm.anim;
    }

    void AttackHandler()
    {
        if (Input.GetButtonDown(lightButton) && !attacking)
        {
            isHoldingAttack = true;
            anim.ResetTrigger("LightAttack");
            anim.ResetTrigger("HeavyAttack");
            ComboAttack();
        }
        else if (Input.GetButtonUp(lightButton) && isHoldingAttack)
        {
            isHoldingAttack = false;
            if (timeHolding <= heavyThreshold) anim.SetTrigger("LightAttack");
            else if (timeHolding >= heavyThreshold) anim.SetTrigger("HeavyAttack");
        }

        if (isHoldingAttack)
        {
            timeHolding += Time.deltaTime;
            if (timeHolding >= heavyThreshold)
            {
                isHoldingAttack = false;
                anim.SetTrigger("HeavyAttack");
            }
        }
        else timeHolding = 0;
        Debug.Log(isHoldingAttack);
    }

    void ComboAttack()
    {
        if (Time.time - lastTimeComboEnd > 0.2f && comboIndex < comboList.Count)
        {
            CancelInvoke("EndCombo");

            if (Time.time - lastTimeClicked >= 0.4f)
            {
                anim.runtimeAnimatorController = comboList[comboIndex].animatorOV;
                anim.Play("Charging Attack", 0, 0);

                comboIndex++;
                lastTimeClicked = Time.time;
                if (comboIndex > comboList.Count) { comboIndex = 0; }
            }
        }
    }

    void ExitAttack()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f && anim.GetCurrentAnimatorStateInfo(0).IsTag("AttackEnd"))
        {
            Invoke("EndCombo", 0);
            Debug.Log("Combo ended");
        }
    }

    void EndCombo()
    {
        comboIndex = 0;
        lastTimeComboEnd = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        AttackHandler();

        ExitAttack();

        //adds to blocktime
        if (attacking & inv.shieldEquipped) blockTime += Time.deltaTime;

        //Throw item
        if (Input.GetKeyDown(KeyCode.Q) && !attacking && (inv.IsArmed() || inv.shieldEquipped))
        {
            if (inv.IsArmed()) swordThrown = true;
            anim.SetTrigger("Throw");
            inv.DropItem();
        }

        //Shield block
        if (Input.GetButtonDown(shieldButton) && !dashAttack && canBlock && pl.currentBlockAmount > 0)
        {
            blocking = true;
            pm.moveSpeed = 2;
            weapons[1].transform.SetParent(Player.transform, false);
            attacking = true;
            LHand.enabled = false;
            RHand.enabled = false;
            fShield.SetActive(false);
            sShield.SetActive(true);
        }

        //Shield unblock
        else if ((Input.GetButtonUp(shieldButton) && !dashAttack && blocking) || pl.currentBlockAmount <= 0)
        {
            blocking = false;
            StartCoroutine(BlockCD());
            weapons[1].transform.SetParent(weapon.transform, false);
            attacking = false;
            LHand.enabled = true;
            RHand.enabled = true;
            fShield.SetActive(true);
            sShield.SetActive(false);
            blockTime = 0;
        }

        //only check for inputs if not attacking
        else if (!attacking && !dashAttack && !swordThrown)
        {
            //if rising
            if (rb.velocity.y > 0.1f)
            {
                //Light jump attack 
                if (Input.GetButtonDown(lightButton))
                {
                    GetInfo();
                    state = (int)AttackType.jump;
                    attacking = true;
                    anim.SetTrigger("Light_Jump_Attack");
                }
                //Heavy jump attack 
                else if (Input.GetButtonDown(heavyButton) && hc.heavyCharges > 0)
                {
                    anim.SetTrigger("Heavy_Jump_Attack");
                    GetInfo();
                    hc.HeavyUpdate();

                    state = (int)AttackType.jump;
                    anim.speed = 0.6f;
                    attacking = true;
                    heavyTrail.emitting = true;
                }
            }

            //if falling
            else if (rb.velocity.y < -0.1f)
            {
                //Light fall attack
                if (Input.GetButtonDown(lightButton))
                {
                    GetInfo();
                    state = (int)AttackType.fall;
                    attacking = true;
                    anim.SetTrigger("Light_Fall_Attack");
                }
                //Heavy fall attack
                else if (Input.GetButtonDown(heavyButton) && hc.heavyCharges > 0)
                {
                    falling = true;
                    anim.SetTrigger("Heavy_Fall_Attack");
                    GetInfo();
                    hc.HeavyUpdate();

                    state = (int)AttackType.heavyFall;
                    anim.speed = 0.6f;
                    attacking = true;
                    heavyTrail.emitting = true;
                    
                    lk.canTurn = false;
                }
            }

            //if dashing check for dash attacks
            else if (pm.isDashing)
            {
                //Light dash attack
                if (Input.GetButtonDown(lightButton) && ((pm.dashDir > 0 && lk.isFacingRight) || (pm.dashDir < 0 && !lk.isFacingRight)))
                {
                    state = (int)AttackType.dash;
                    GetInfo();
                    dashAttack = true;
                    anim.Play("Light_Dash_Attack");
                }
                //Heavy dash attack
                else if (Input.GetButtonDown(heavyButton) && ((pm.dashDir > 0 && lk.isFacingRight) || (pm.dashDir < 0 && !lk.isFacingRight)) && pm.IsGrounded() && hc.heavyCharges > 0)
                {
                    GetInfo();
                    hc.HeavyUpdate();

                    state = (int)AttackType.dash;
                    dashAttack = true;
                    heavyTrail.emitting = true;
                    anim.Play("Heavy_Dash_Attack");

                }
            }
        }
    }

    public void Blocked(Vector3 contactPoint, float blockDamage)
    {
        //Sparks effect when blocked attack
        Instantiate(sparks, contactPoint, Quaternion.identity);
        //Take block damage
        pl.TakeBlockDamage(blockDamage);
        //Parry
        if (blockTime != 0 && blockTime <= 0.2f)
        {
            pl.canTakeDamage = false;
            //Turn down time
            pl.CallCamZoom();
            Time.timeScale = slowedTime;
            Time.fixedDeltaTime = slowedFixedTime;
            StartCoroutine(ResumeTime());
        }
    }

    private void FixedUpdate()
    {
        //Ground smash when touching ground and heavy falling
        if (pm.IsGrounded() && falling)
        {
            anim.SetTrigger("Smash");
            heavying = false;
            lk.canTurn = true;
            falling = false;
        }
    }

    //Hit detection and response
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dashAttack && heavying && other != null && other.GetComponent<SpriteRenderer>() != null)
        {
            color = other.GetComponent<SpriteRenderer>().color;
        }
    }

    //Finishes dash attack
    public void DashAttackDone()
    {
        dashAttack = false;
        attacking = false;
        heavying = false;
        if (heavyTrail != null && heavyTrail.emitting)
        {
            heavyTrail.emitting = false;
        }
    }

    public void AttackStart()
    {
        attacking = true;
        StopCoroutine(Combo());
        canChain = false;
        pm.moveSpeed = 1;
        pm.canMove = false;
    }

    public void AttackDone()
    {
        anim.speed = 1f;
        if (heavyTrail != null && heavyTrail.emitting)
        {
            heavyTrail.emitting = false;
        }
        attacking = false;
        heavying = false;
    }
    private IEnumerator Combo()
    {
        canChain = true;
        yield return new WaitForSeconds(0.75f);
        canChain = false;
    }

    private IEnumerator Finish()
    {
        canFinish = true;
        yield return new WaitForSeconds(1f);
        canFinish = false;
    }

    private void PushForward()
    {
        rb.velocity = new Vector2(transform.localScale.x, rb.velocity.y);
    }

    private void PushUp()
    {
        rb.velocity = new Vector2(rb.velocity.x, 10f);
    }

    private void CanMove()
    {
        pm.canMove = true;
    }

    private void GetInfo()
    {
        //For every weapon
        foreach (GameObject child in weapons)
        {
            //if active
            if (child.activeSelf)
            {
                //for everything in that weapon
                foreach (Transform child2 in child.transform)
                {
                    if (child2.CompareTag("HeavyTrail"))
                    {
                        heavyPoint = child2;
                        heavyTrail = child2.GetComponent<TrailRenderer>();
                        //Delete heavyflash that got disabled
                        foreach (Transform child3 in child2.transform)
                        {
                            if (child3 != null)
                            {
                                Destroy(child3.gameObject);
                            }
                        }
                    }
                    if (child2.CompareTag("FX"))
                    {

                    }
                }
                break;
            }
        }
    }

    //resumes time
    private IEnumerator ResumeTime()
    {
        yield return new WaitForSeconds(4f * slowedTime);

        float currentTimeSpeed = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < 4f)
        {
            elapsedTime += Time.deltaTime;

            currentTimeSpeed = Mathf.Lerp(slowedTime, defaultTime, (elapsedTime));
            Time.timeScale = currentTimeSpeed;
            Time.fixedDeltaTime = currentTimeSpeed / 50f;

            yield return null;
        }

        Time.timeScale = defaultTime;
        Time.fixedDeltaTime = defaultFixedTime;
        pl.canTakeDamage = true;
        pm.moveSpeed = 1;
        if (heavying)
        {
            anim.speed = 0.6f;
        }
        else
        {
            anim.speed = 1;
        }
    }

    //Attack function
    private void Attack()
    {
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(attackPoint.position, attackRange[state], hitLayer);
        foreach (Collider2D hit in hitTargets)
        {
            hitTag = hit.tag;

            //Pickup
            if (hitTag.Equals("Sword") || hitTag.Equals("Shield"))
            {
                pick = hit.GetComponent<Pickup>();
                if (!heavying)
                {
                    pick.Knockback(lk.dir, knockback[inv.weaponState], state);
                }
                else
                {
                    pick.Knockback(lk.dir, knockback[inv.weaponState] * 1.5f, state);
                }
            }

            //Bat
            else if (hitTag.Equals("Bat"))
            {
                bat = hit.transform.GetComponent<Bat>();
                if (!heavying)
                {
                    bat.Knockback(lk.dir, knockback[inv.weaponState], state);
                    bat.TakeDamage(attack[state]);
                }
                else
                {
                    bat.Knockback(lk.dir, knockback[inv.weaponState] * 1.5f, state);
                    bat.TakeDamage(attack[state] * 2);
                }
            }

            //Enemy
            else if (hitTag.Equals("Enemy"))
            {
                string weaponName = Enum.GetName(typeof(Player_Inventory.WeaponState), inv.weaponState);
                enemy = hit.transform.GetComponent<Enemy>();
                if (!heavying)
                {
                    enemy.Hit(attack[state], Color.white, transform.position, knockback[inv.weaponState], state, weaponName);
                }
                else
                {
                    enemy.Hit(attack[state] * 1.5f, Color.red, transform.position, knockback[inv.weaponState] * 1.5f, state, weaponName);
                }
                CallFreezeFrame();
            }
        }
    }

    private void CallFreezeFrame()
    {
        StartCoroutine(FreezeFrame());
    }

    //Freeze the character when hitting an enemy
    private IEnumerator FreezeFrame()
    {
        anim.speed = 0f;
        yield return new WaitForSeconds(0.05f);
        anim.speed = 1f;
    }

    private IEnumerator BlockCD()
    {
        canBlock = false;
        yield return new WaitForSeconds(1f);
        canBlock = true;
    }

    private void SmashDebris()
    {
        debrisObj = Instantiate(debris, heavyTrail.transform.position, Quaternion.identity);
        ParticleSystem.MainModule ma = debrisObj.GetComponent<ParticleSystem>().main;
        ma.startColor = pm.color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(attackPoint.position, attackRange[state]);
    }
}
