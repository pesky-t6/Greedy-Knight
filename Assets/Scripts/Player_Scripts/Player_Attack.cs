using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Player_Attack : MonoBehaviour
{
    private Rigidbody2D rb;
    private TrailRenderer heavyTrail;
    private Transform heavyPoint;
    public Transform attackPoint;
    [SerializeField] private LayerMask hitLayer;

    private Pickup pick;
    private Bat bat;
    private Enemy enemy;

    private Player_Inventory inv;
    private Player_Movement pm;
    private Player_Life pl;

    [SerializeField] private Player_Look lk;
    [SerializeField] private ParticleSystem ps;
    [SerializeField] private GameObject weapon;
    [SerializeField] private GameObject[] weapons;

    [SerializeField] private GameObject fShield;
    [SerializeField] private GameObject sShield;
    [SerializeField] private GameObject sparkPoint;

    [SerializeField] private GameObject Player;
    [SerializeField] private SpriteRenderer LHand;
    [SerializeField] private SpriteRenderer RHand;

    [SerializeField] private GameObject sparks;
    [SerializeField] private GameObject heavyFlash;

    [SerializeField] private Image[] heavyIcon;
    private Material[] heavyIconMat;
    private Material heavyGlow;

    [ColorUsage(true, true)]
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashTime = 0.25f;
    [SerializeField] private Material defMat;

    [SerializeField] private GameObject debris;
    private GameObject debrisObj;

    private Coroutine heavyFlasher;

    private bool falling = false;
    public bool dashAttack = false;
    public bool attacking = false;
    public bool canChain;
    public bool canFinish;
    private bool groundEmmitting;

    private string hitTag;

    //Heavy management
    private bool heavying = false;
    public int heavyCharges = 3;
    private int maxHeavyCharges = 3;
    private float heavyChargeTimer = 5f;
    public bool charging = false;

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

    private int[,] attack = {
    { 15, 15, 10, 14 }, //opener
    { 15, 20, 14, 22 }, //chain
    { 20, 8, 25, 10 },  //finisher
    { 10, 15, 9, 16 },  //dash
    { 17, 8, 20, 15 },  //jump
    { 14, 20, 13, 25 },  //fall
    { 14, 20, 13, 25 }   //heavyFall
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

        heavyIconMat = new Material[heavyIcon.Length];
        for (int i = 0; i < heavyIcon.Length; i++)
        {
            heavyIconMat[i] = heavyIcon[i].material;
        }
    }

    //Easier to flash the heavy charge
    private void CallHeavyFlash(int i)
    {
        heavyFlasher = StartCoroutine(HeavyChargeFlasher(i));
    }

    //Flash to show charge complete
    private IEnumerator HeavyChargeFlasher(int i)
    {
        heavyIconMat[i].SetColor("_FlashColor", _flashColor);

        float currentFlashAmount = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < _flashTime)
        {
            elapsedTime += Time.deltaTime;

            currentFlashAmount = Mathf.Lerp(1f, 0f, (elapsedTime / _flashTime));
            this.heavyIconMat[i].SetFloat("_FlashAmount", currentFlashAmount);

            yield return null;
        }
    }


    // Update is called once per frame
    void Update()
    {
        //adds to blocktime
        if (attacking & inv.shieldEquipped)
        {
            blockTime += Time.deltaTime;
        }

        //Throw item
        if (Input.GetKeyDown(KeyCode.Q) && !attacking && (inv.IsArmed() || inv.shieldEquipped))
        {
            pm.anim.SetTrigger("Throw");
            inv.DropItem();
        }

        //Shield block
        if (Input.GetButtonDown("Fire1") && inv.shieldEquipped && !dashAttack && canBlock && pl.currentBlockAmount > 0)
        {
            blocking = true;
            pm.moveSpeed = 2;
            weapons[3].transform.SetParent(Player.transform, false);
            attacking = true;
            LHand.enabled = false;
            RHand.enabled = false;
            fShield.SetActive(false);
            sShield.SetActive(true);
        }

        //Shield unblock
        else if ((Input.GetButtonUp("Fire1") && inv.shieldEquipped && !dashAttack && blocking) || pl.currentBlockAmount <= 0)
        {
            blocking = false;
            StartCoroutine(BlockCD());
            weapons[3].transform.SetParent(weapon.transform, false);
            attacking = false;
            LHand.enabled = true;
            RHand.enabled = true;
            fShield.SetActive(true);
            sShield.SetActive(false);
            blockTime = 0;
        }

        //only check for inputs if not attacking
        else if (!attacking && !dashAttack && inv.IsArmed())
        {
            //if rising
            if (rb.velocity.y > 0.1f)
            {
                //Light jump attack 
                if (Input.GetButtonDown("Fire1"))
                {
                    GetInfo();
                    state = (int)AttackType.jump;
                    attacking = true;
                    pm.anim.SetTrigger("Light_Jump_Attack");
                }
                //Heavy jump attack 
                else if (Input.GetButtonDown("Fire2") && heavyCharges > 0)
                {
                    pm.anim.SetTrigger("Heavy_Jump_Attack");
                    GetInfo();
                    HeavyUpdate();

                    state = (int)AttackType.jump;
                    pm.anim.speed = 0.6f;
                    attacking = true;
                    heavyTrail.emitting = true;
                }
            }

            //if falling
            else if (rb.velocity.y < -0.1f)
            {
                //Light fall attack
                if (Input.GetButtonDown("Fire1"))
                {
                    GetInfo();
                    state = (int)AttackType.fall;
                    attacking = true;
                    pm.anim.SetTrigger("Light_Fall_Attack");
                }
                //Heavy fall attack
                else if (Input.GetButtonDown("Fire2") && heavyCharges > 0)
                {
                    falling = true;
                    pm.anim.SetTrigger("Heavy_Fall_Attack");
                    GetInfo();
                    HeavyUpdate();

                    state = (int)AttackType.heavyFall;
                    pm.anim.speed = 0.6f;
                    attacking = true;
                    heavyTrail.emitting = true;
                    
                    lk.canTurn = false;
                }
            }

            //Basic attacks check
            else if (!pm.isDashing && pm.IsGrounded())
            {
                //Light attacks
                if (Input.GetButtonDown("Fire1"))
                {
                    GetInfo();
                    if (canFinish)
                    {
                        state = (int)AttackType.finisher;
                        pm.anim.SetTrigger("Finisher");
                    }
                    else if (canChain)
                    {
                        state = (int)AttackType.chain;
                        pm.anim.SetTrigger("Chain");
                    }
                    else if (!canChain)
                    {
                        state = (int)AttackType.opener;
                        pm.anim.SetTrigger("Opener");
                    }

                }

                //Heavy attacks
                else if (Input.GetButtonDown("Fire2") && heavyCharges > 0)
                {
                    GetInfo();
                    HeavyUpdate();

                    pm.anim.speed = 0.6f;
                    heavyTrail.emitting = true;
                    if (canFinish)
                    {
                        state = (int)AttackType.finisher;
                        pm.anim.SetTrigger("Finisher");
                    }
                    else if (canChain)
                    {
                        state = (int)AttackType.chain;
                        pm.anim.SetTrigger("Chain");
                    }
                    else if (!canChain)
                    {
                        state = (int)AttackType.opener;
                        pm.anim.SetTrigger("Opener");
                    }

                }
            }

            //if dashing check for dash attacks
            else if (pm.isDashing)
            {
                //Light dash attack
                if (Input.GetButtonDown("Fire1") && ((pm.dashDir > 0 && lk.isFacingRight) || (pm.dashDir < 0 && !lk.isFacingRight)))
                {
                    state = (int)AttackType.dash;
                    GetInfo();
                    dashAttack = true;
                    pm.anim.Play("Light_Dash_Attack");
                }
                //Heavy dash attack
                else if (Input.GetButtonDown("Fire2") && ((pm.dashDir > 0 && lk.isFacingRight) || (pm.dashDir < 0 && !lk.isFacingRight)) && pm.IsGrounded() && heavyCharges > 0)
                {
                    GetInfo();
                    HeavyUpdate();

                    state = (int)AttackType.dash;
                    dashAttack = true;
                    heavyTrail.emitting = true;
                    pm.anim.Play("Heavy_Dash_Attack");

                }
            }
        }

        if (groundEmmitting)
        {
            if (!lk.isFacingRight)
            {
                ps.transform.localScale = new Vector3(-1, 1, 1);
            }
            else if (lk.isFacingRight)
            {
                ps.transform.localScale = new Vector3(1, 1, 1);
            }
        }

        if (heavyCharges < maxHeavyCharges)
        {
            ShowHeavyIcon();
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
            pm.anim.SetTrigger("Smash");
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

    //Updates heavy charge
    private void ShowHeavyIcon()
    {
        heavyIcon[heavyCharges].fillAmount += 1 / (heavyChargeTimer) * Time.deltaTime;
    }

    //Uses a heavy charge
    private void HeavyUpdate()
    {
        heavying = true;
        heavyCharges--;
        heavyIcon[heavyCharges].fillAmount = 0;
        if (charging)
        {
            heavyIcon[heavyCharges].fillAmount = heavyIcon[heavyCharges + 1].fillAmount;
            heavyIcon[heavyCharges + 1].fillAmount = 0;
        }
        ChargeHeavy();
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
        pm.anim.speed = 1f;
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

    private void DustOn()
    {
        ParticleSystem.MainModule ma = ps.main;
        ma.startColor = color;
        ps.Play();
        groundEmmitting = true;
    }

    private void DustOff()
    {
        ps.Stop();
        groundEmmitting = false;
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
                        ps = child2.GetComponent<ParticleSystem>();
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
            pm.anim.speed = 0.6f;
        }
        else
        {
            pm.anim.speed = 1;
        }
    }

    private void ChargeHeavy()
    {
        if (heavyCharges < maxHeavyCharges && !charging)
        {
            StartCoroutine(StartCharging());
        }
    }

    //Starts charging heavy
    private IEnumerator StartCharging()
    {
        charging = true;
        yield return new WaitForSeconds(heavyChargeTimer);
        heavyIcon[heavyCharges].fillAmount = 1;
        CallHeavyFlash(heavyCharges);
        heavyCharges++;
        charging = false;
        if (heavyCharges < maxHeavyCharges)
        {
            StartCoroutine(StartCharging());
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
            if (hitTag.Equals("Sword") || hitTag.Equals("Axe") || hitTag.Equals("Spear") || hitTag.Equals("Shield") || hitTag.Equals("Hammer"))
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
                    bat.TakeDamage(attack[state, inv.weaponState]);
                }
                else
                {
                    bat.Knockback(lk.dir, knockback[inv.weaponState] * 1.5f, state);
                    bat.TakeDamage(attack[state, inv.weaponState] * 2);
                }
            }

            //Enemy
            else if (hitTag.Equals("Enemy"))
            {
                string weaponName = Enum.GetName(typeof(Player_Inventory.WeaponState), inv.weaponState);
                enemy = hit.transform.GetComponent<Enemy>();
                if (!heavying)
                {
                    enemy.Hit(attack[state, inv.weaponState], Color.white, transform.position, knockback[inv.weaponState], state, weaponName);
                }
                else
                {
                    enemy.Hit(attack[state, inv.weaponState] * 1.5f, Color.red, transform.position, knockback[inv.weaponState] * 1.5f, state, weaponName);
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
        pm.anim.speed = 0f;
        yield return new WaitForSeconds(0.05f);
        pm.anim.speed = 1f;
    }
    //Red flash to indicate heavy attack
    private void HeavyFlash()
    {
        if (heavying)
        {
            Instantiate(heavyFlash, heavyPoint.position, Quaternion.identity, heavyTrail.transform);
        }
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
