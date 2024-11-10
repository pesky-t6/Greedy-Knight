using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Rendering;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.GraphicsBuffer;

public class Bat : MonoBehaviour
{
    private float health = 1;
    private float speed = 20f;
    public bool canAttack;
    private bool dead = false;
    private bool hasLineOfSight = false;
    private bool hitPlayer = false;

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject poof;

    private Animator anim;
    private Player_Life playerLife;
    private Player_Attack playerAttack;
    private Transform playerPos;
    private GameObject player;
    private BoxCollider2D box;
    private Rigidbody2D rb;
    public BatAI batAI;
    private Vector2 direction;
    private SpriteRenderer sp;

    [SerializeField] private float damage;
    [SerializeField] private float heal;

    private void Start()
    {
        sp = GetComponent<SpriteRenderer>();   
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        playerPos = player.GetComponent<Transform>();
        batAI = GetComponent<BatAI>();
        box.enabled = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (health <= 0 && !dead)
        {
            dead = true;
            Death();
        }
        if (Vector2.Distance(transform.position, playerPos.position) < 2.5f && canAttack && hasLineOfSight && batAI.target == batAI.player)
        {
            canAttack = false;
            anim.SetTrigger("Attack");
            direction = (playerPos.position - transform.position).normalized;
        }
    }

    private void FixedUpdate()
    {
        RaycastHit2D ray = Physics2D.Raycast(transform.position, playerPos.position - transform.position, 1000f, ~enemyLayer);

        if (ray.collider != null)
        {
            if (ray.collider.CompareTag("Player") || ray.collider.CompareTag("Shield"))
            {
                hasLineOfSight = true;
            }
            else { hasLineOfSight = false; }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerLife = player.GetComponent<Player_Life>();
            if (playerLife != null && !hitPlayer)
            {
                hitPlayer = true;
                playerLife.TakeDamage(damage, Color.white);
                StartCoroutine(Cooldown());
            }
        }
        else if (collision.gameObject.CompareTag("Shield"))
        {
            RaycastHit2D ray = Physics2D.Raycast(transform.position, player.transform.position - transform.position, 1000f, ~enemyLayer);
            playerAttack = player.GetComponent<Player_Attack>();
            if (playerAttack != null)
            {
                hitPlayer = true;
                playerAttack.Blocked(ray.point, damage);
                StartCoroutine(Cooldown());
            }
        }
        else
        {
            return;
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
    }

    private IEnumerator Cooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(0.5f);
        box.enabled = false;
        yield return new WaitForSeconds(9.5f);
        hitPlayer = false;
        canAttack = true;
    }

    private void Attack()
    {
        rb.velocity = new Vector2(direction.x * speed, direction.y * speed);
        StartCoroutine(Cooldown());
    }

    private void CanHit()
    {
        box.enabled = true;
    }

    private void Death()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        playerLife = GameObject.FindGameObjectWithTag("Player").GetComponent<Player_Life>();
        playerLife.Heal(heal);
        Instantiate(poof, transform.position, Quaternion.identity);
        canAttack = false;
        Destroy(gameObject);
    }
    public void Knockback(int dirx, float value)
    {
        rb.velocity = new Vector2(rb.velocity.x + dirx * value, rb.velocity.y);
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
