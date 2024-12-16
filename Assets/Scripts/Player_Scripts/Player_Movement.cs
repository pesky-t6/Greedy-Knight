using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

public class Player_Movement : MonoBehaviour
{
    private Rigidbody2D rb;
    public Animator anim;
    private Player_Life life;

    private enum MovementState { idle, running, jumping, falling, backrun, dash, backdash }

    //floats
    private float dirX = 0f;
    private float dashSpeed = 10f;
    private float dashCD = 1.5f;
    private float dashTime = 0.25f;
    private float targetSpeed;
    private float speedDif;
    private float accelRate;
    private float movement;
    private float lastGroundedTime;
    private float lastJumpTime;
    public float dashDir;
    private float jumpCoyoteTime = 0.15f;
    private float jumpBufferTime = 0.1f;
    private float gravityScale = 1f;
    private float fallGravityMultiplier = 1.5f;
    private float frictionAmount = 0.2f;
    private float amount;
    private float idleTime = 0;

    //booleans
    private bool dashReady = true;
    public bool isDashing = false;
    public bool canMove = true;
    private bool isJumping;
    private bool canMantle = true;

    //serialized variables
    [SerializeField] private Player_Look look;
    [SerializeField] private Player_Attack attack;
    [SerializeField] public float moveSpeed = 4f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform ledgeCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;
    [SerializeField] private ParticleSystem pr;
    [SerializeField] private float ledgeCheckRadius = 1f;
    [SerializeField] private Vector3 wallCheckSize = new Vector3 (1f,1f,1f);
    [SerializeField] private float acceleration = 3f;
    [SerializeField] private float decceleration = 3f;
    [SerializeField] private float velPower = 0.6f;

    [SerializeField] private CircleCollider2D circleCollider;
    [SerializeField] private Transform leftLeg;
    [SerializeField] private Transform rightLeg;
    [SerializeField] private ParticleSystem leftDust;
    [SerializeField] private ParticleSystem rightDust;

    private EmissionModule leftDustEmmision;
    private EmissionModule rightDustEmmision;
    public UnityEngine.Color color;

    private MovementState state;

    // Start is called before the first frame update
    void Start()
    {
        life = GetComponent<Player_Life>();
        rb = GetComponent<Rigidbody2D>();
        dirX = Input.GetAxisRaw("Horizontal");
        anim = GetComponent<Animator>();
        pr.Stop();

        leftDustEmmision = leftDust.emission;
        rightDustEmmision = rightDust.emission;
    }

    // Update is called once per frame
    void Update()
    {
        //Get value of horizontal movement
        dirX = Input.GetAxisRaw("Horizontal");
        if (canMove)
        {
            //Jump if grounded
            if (Input.GetButtonDown("Jump") && IsGrounded())
            {
                Jump();
            }

            //Hold jump for higher jump
            if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            }

            //Dash
            if (Input.GetKeyDown(KeyCode.LeftShift) && dashReady && !attack.attacking)
            {
                if (rb.bodyType == RigidbodyType2D.Static)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
                canMove = false;
                isDashing = true;
                dashReady = false;
                dashDir = dirX;
                tr.emitting = true;
                pr.Play();
                rb.gravityScale = 0f;
                look.canTurn = false;
                StartCoroutine(StopDash());
            }

            if (isDashing)
            {
                life.canTakeDamage = false;
                rb.velocity = new Vector2(dashDir * dashSpeed, 0f);
                return;
            }
            else
            {
                life.canTakeDamage = true;
            }

            if (IsGrounded())
            {
                isJumping = false;
                ParticleSystem.MainModule ma = leftDust.main;
                ma.startColor = color;
                ma = rightDust.main;
                ma.startColor = color;
                lastGroundedTime = jumpCoyoteTime;
            }

            //fall quicker
            if (rb.velocity.y < 0f)
            {
                rb.gravityScale = gravityScale * fallGravityMultiplier;
            }
            else
            {
                rb.gravityScale = gravityScale;
            }

            //Mantle if touching wall but not touching ledge
            if (IsWalled() && !IsLedged() && !IsGrounded() && !attack.attacking && canMantle)
            {
                attack.attacking = false;
                attack.dashAttack = false;
                look.canTurn = false;
                anim.SetTrigger("mantle");
                canMantle = false;
                StartCoroutine(MantleCD());
            }
        }
        //Dust effects
        if (IsGrounded() && (dirX > 0f && look.isFacingRight || dirX < 0f && !look.isFacingRight))
        {
            if (!look.isFacingRight)
            {
                leftDust.transform.localScale = new Vector3(-1, 1, 1);
                rightDust.transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                leftDust.transform.localScale = new Vector3(1, 1, 1);
                rightDust.transform.localScale = new Vector3(1, 1, 1);
            }
            if (Physics2D.OverlapCircle(leftLeg.position, 0.15f, groundLayer))
            {
                if (!leftDustEmmision.enabled)
                {
                    leftDustEmmision.enabled = true;
                }
            }
            else
            {
                if (leftDustEmmision.enabled)
                {
                    leftDustEmmision.enabled = false;
                }
            }
            if (Physics2D.OverlapCircle(rightLeg.position, 0.15f, groundLayer))
            {
                if (!rightDustEmmision.enabled)
                {
                    rightDustEmmision.enabled = true;
                }
            }
            else
            {
                if (rightDustEmmision.enabled)
                {
                    rightDustEmmision.enabled = false;
                }
            }
        }
        else
        {
            if (leftDustEmmision.enabled)
            {
                leftDustEmmision.enabled = false;
            }
            if (rightDustEmmision.enabled)
            {
                rightDustEmmision.enabled = false;
            }
        }

        //Can twirl weapon or not when starting to run
        //Must be idle for more than 2.5 seconds
        if (idleTime >= 2.5f) { anim.SetBool("canTwirl", true); }
        else { anim.SetBool("canTwirl", false); }

        UpdateAnimationState(state);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(IsGrounded() && collision.CompareTag("Ground"))
        {
            color = collision.GetComponent<SpriteRenderer>().color;
        }
    }


    //Change velocity depending on user input
    private void FixedUpdate()
    {
        //Smoother movement
        targetSpeed = dirX * moveSpeed;
        speedDif = targetSpeed - rb.velocity.x;
        accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
        movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);

        rb.AddForce(movement * Vector2.right);

        //Quicker Stops
        if (lastGroundedTime > 0 && dirX == 0)
        {
            amount = Mathf.Min(Mathf.Abs(rb.velocity.x), Mathf.Abs(frictionAmount));
            amount *= Mathf.Sign(rb.velocity.x);
            rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }

        lastGroundedTime -= Time.deltaTime;
        lastJumpTime -= Time.deltaTime;

        //Coyote Jump
        if (lastGroundedTime > 0 && lastJumpTime > 0 && !isJumping)
        {
            Jump();
        }
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

    //Update Player's animation based on movement
    private void UpdateAnimationState(MovementState state)
    {
        if (dirX > 0f && look.isFacingRight || dirX < 0f && !look.isFacingRight)
        {
            state = MovementState.running;
            if (!attack.attacking)
            {
                moveSpeed = 4f;
            }
        }
        else if (dirX > 0f && !look.isFacingRight || dirX < 0f && look.isFacingRight)
        {
            state = MovementState.backrun;
            moveSpeed = 1.25f;
        }
        else
        {
            state = MovementState.idle;
        }

        if (rb.velocity.y > 0.1f && !IsGrounded())
        {
            state = MovementState.jumping;
        }
        else if (rb.velocity.y <= 0.1f && !IsGrounded())
        {
            state = MovementState.falling;
            attack.attacking = false;
        }

        if (isDashing && !attack.dashAttack)
        {
            if (dirX > 0f && look.isFacingRight || dirX < 0f && !look.isFacingRight)
            {
                state = MovementState.dash;
            }
            else if (dirX > 0f && !look.isFacingRight || dirX < 0f && look.isFacingRight)
            {
                state = MovementState.backdash;
            }
        }

        if (state == MovementState.idle) { idleTime += Time.deltaTime; }
        else { idleTime = 0; }
        anim.SetInteger("state", (int)state);
    }

    //Jump method
    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true;
        lastJumpTime = jumpBufferTime;
    }

    //Dash stuff
    private IEnumerator StopDash()
    {
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = 1f;
        tr.emitting = false;
        pr.Stop();
        isDashing = false;
        look.canTurn = true;
        canMove = true;
        yield return new WaitForSeconds(dashCD);
        dashReady = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(ledgeCheck.position, ledgeCheckRadius);
        Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
        Gizmos.DrawWireSphere(groundCheck.position, 0.125f);
    }

    public void SetStatic()
    {
        rb.bodyType = RigidbodyType2D.Static;
    }

    public void SetDynamic()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void Mantle()
    {
        rb.velocity = new Vector2(0f, 7.5f);
        look.canTurn = true;
    }

    private IEnumerator MantleCD()
    {
        yield return new WaitForSeconds(1f);
        canMantle = true;
    }
}
