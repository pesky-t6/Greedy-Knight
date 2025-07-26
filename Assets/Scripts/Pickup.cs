using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    private float speed = 15f;
    private Rigidbody2D rb;
    private Player_Inventory inv;
    private Player_Look lk;
    public GameObject itemButton;
    //Whether it is a thrown object or not
    public bool thrown = false;
    private bool throwable = false;

    private int[] throwDamage = { 10, 10 }; 
    private int[] knockback = { 3, 0 };

    private int knockbackIndex;

    private bool canDamage = true;

    private enum Throwables { sword, axe, spear, shield, hammer}

    [SerializeField] private LayerMask lm;
    [SerializeField] private BoxCollider2D bx;
    [SerializeField] private GameObject debris;
    [SerializeField] private Transform debrisPoint;
    private GameObject debrisObj;
    private UnityEngine.Color color;

    // Start is called before the first frame update
    void Start()
    {
        tag = gameObject.tag;
        rb = GetComponent<Rigidbody2D>();
        inv = GameObject.FindGameObjectWithTag("Player").GetComponent<Player_Inventory>();
        lk = GameObject.FindGameObjectWithTag("Player_Head").GetComponent<Player_Look>();
        if (!lk.isFacingRight)
        {
            Vector3 localScale = transform.localScale;
            localScale.x = -1f;
            transform.localScale = localScale;
        }
        if(gameObject.CompareTag("Sword"))
        {
            knockbackIndex = 0;
        }
        else if (gameObject.CompareTag("Shield"))
        {
            knockbackIndex = 1;
        }

        if (!thrown)
        {
            rb.AddTorque(UnityEngine.Random.Range(-5, 5));
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground"))
        {
            color = collision.GetComponent<SpriteRenderer>().color;
        }
        else if(collision.CompareTag("Enemy"))
        {
            color = UnityEngine.Color.red;
        }
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(transform.position, 0.15f, lm);
    }

    //Tests for damage to enemies when thrown
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (throwable && rb.velocity.magnitude > 0.7f && canDamage)
        {
            if (collision.collider.CompareTag("Bat"))
            {
                Bat bat = collision.collider.GetComponent<Bat>();
                bat.Knockback((int)(bat.gameObject.transform.position - transform.position).magnitude, knockback[knockbackIndex]);
                bat.TakeDamage(throwDamage[knockbackIndex]);
                StartCoroutine(DamageCD());
            }
            if (collision.collider.CompareTag("Enemy"))
            {

                Enemy enemy = collision.collider.GetComponent<Enemy>();
                enemy.Knockback(transform.position, knockback[knockbackIndex]);
                enemy.TakeDamage(throwDamage[knockbackIndex], UnityEngine.Color.white, null);

                //Sticking to enemy if touching enemy layer
                if (bx != null && bx.IsTouchingLayers(1 << 8))
                {
                    throwable = false;
                    rb.velocity = Vector3.zero;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    gameObject.layer = 8;
                    transform.SetParent(enemy.gameObject.transform, true);
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    transform.position += transform.up * 0.1f;
                    debrisObj = Instantiate(debris, debrisPoint.transform.position, Quaternion.identity);
                    ParticleSystem.MainModule ma = debrisObj.GetComponent<ParticleSystem>().main;
                    ma.startColor = color;
                    StartCoroutine(FallOfEnemy(10f));
                }

                StartCoroutine(DamageCD());
            }
        }
    }

    //Causes the weapon to eventually fall of the stuck enemy
    private IEnumerator FallOfEnemy(float time)
    {
        yield return new WaitForSeconds(time);
        transform.parent = null;
        rb.constraints = RigidbodyConstraints2D.None;
        rb.bodyType = RigidbodyType2D.Dynamic;
        gameObject.layer = 7;
    }

    //Causes the weapon to fall off when the enemy its stuck to dies
    public void DetachWeapon()
    {
        transform.parent = null;
        rb.constraints = RigidbodyConstraints2D.None;
        rb.bodyType = RigidbodyType2D.Dynamic;
        gameObject.layer = 7;
    }

    private void Update()
    {
        if (rb.velocity.magnitude < 1f)
        {
            Physics2D.IgnoreLayerCollision(7, 8, true);
        }
        else
        {
            Physics2D.IgnoreLayerCollision(7, 8, false);
        }
        if (bx != null &&  bx.IsTouchingLayers(lm) && rb.velocity.magnitude > 1f)
        {
            throwable = false;
            rb.velocity = Vector3.zero;
            transform.position += transform.up * 0.1f;
            rb.bodyType = RigidbodyType2D.Static;
            gameObject.layer = 9;
            StartCoroutine(FallOfEnemy(30f));
            debrisObj = Instantiate(debris, debrisPoint.transform.position, Quaternion.identity);
            ParticleSystem.MainModule ma = debrisObj.GetComponent<ParticleSystem>().main;
            ma.startColor = color;
        }

        if (thrown)
        {
            throwable = true;
            thrown = false;
            rb.velocity = transform.up * speed;
            if (!gameObject.CompareTag("Spear"))
            {
                if (lk.isFacingRight)
                {
                    rb.AddTorque(-35f);
                }
                else
                {
                    rb.AddTorque(35f);
                }
            }
            else
            {
                if (lk.isFacingRight)
                {
                    rb.AddTorque(-3f);
                }
                else
                {
                    rb.AddTorque(3f);
                }
            }
        }
    }

    private IEnumerator DamageCD()
    {
        canDamage = false;
        yield return new WaitForSeconds(0.5f);
        canDamage = true;
    }

    public void Knockback(int dirx, float value, int type)
    {
        if (type == 4)
        {
            rb.velocity = new Vector2(0, value);
        }
        else if (type == 5)
        {
            rb.velocity = new Vector2(0, -value);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x + dirx * value, rb.velocity.y);
        }
    }
}
