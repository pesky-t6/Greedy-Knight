using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Runtime.CompilerServices;

public class BatAI : MonoBehaviour
{
    public Transform target;
    private GameManager gameManager;

    public Transform player;
    public Transform fly;

    private Bat bat;

    public float speed = 400f;
    public float nextWaypointDistance = 3f;
    
    Path path;
    int currentWaypoint = 0;
    bool reachedEndOfPath = false;

    Seeker seeker;
    Rigidbody2D rb;

    //Variables for changing size of bat
    private float x;
    private float y;

    private void Start()
    {
        bat = GetComponent<Bat>();

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        fly = GameObject.FindGameObjectWithTag("Flyer Target").GetComponent<Transform>();
        fly.position = new Vector2(player.position.x + Random.Range(-10, 10), player.position.y + Random.Range(10, 20));

        target = fly;

        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();

        //Calculate randomized speeds
        speed = Random.Range(speed -10f, speed + 10f);

        float rando = Random.Range(-0.1f, 0.1f);
        x = transform.localScale.x + rando;
        y = transform.localScale.y + rando;
        transform.localScale = new Vector3(transform.localScale.x + rando, transform.localScale.y + rando, transform.localScale.z);

        InvokeRepeating("UpdatePath", 0f, 0.5f);
    }

    void UpdatePath()
    {
        if (seeker.IsDone())
        {
            seeker.StartPath(rb.position, target.position, OnPathComplete);
        }
    }

    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    private void FixedUpdate()
    {
        if (reachedEndOfPath && target == fly && rb.velocity.magnitude < 0.5f)
        {
            gameManager.batsWaiting++;
        }

        if (gameManager.batsWaiting >= gameManager.totalBats.Length)
        {
            target = player;
        }

        if (target != fly && !bat.canAttack)
        {
            gameManager.batsWaiting--;
            target = fly;
        }

        if (path == null)
            return;

        if(currentWaypoint >= path.vectorPath.Count)
        {
            reachedEndOfPath = true;
            return;
        }
        else
        {
            reachedEndOfPath = false;
        }

        Vector2 direction = ((Vector2)path.vectorPath[currentWaypoint] - rb.position).normalized;
        Vector2 force = direction * speed * Time.deltaTime;

        rb.AddForce(force);

        float distance = Vector2.Distance(rb.position, path.vectorPath[currentWaypoint]);

        if (distance < nextWaypointDistance)
        {
            currentWaypoint++;
        }

        if(force.x >= 0.01f)
        {
            transform.localScale = new Vector3(x, y, 1f);
        }
        else if (force.x <= 0.01f)
        {
            transform.localScale = new Vector3(-x, y, 1f);
        }
    }
}
