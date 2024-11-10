using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private BatAI batAI;
    [SerializeField] private Transform playerPos;
    public GameObject[] totalBats;
    public GameObject[] totalEnemies;
    public float currentAttackers = 0;
    private float maxAttackers = 3;
    public float batsWaiting = 0;

    // Start is called before the first frame update
    void Start()
    {
        totalBats = GameObject.FindGameObjectsWithTag("Bat");
        totalEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (totalBats.Length != 0)
        {
            batAI = GameObject.FindGameObjectWithTag("Bat").GetComponent<BatAI>();
        }
        StartCoroutine(BatReposition());
        InvokeRepeating("ReShuffleAttackers", 0f, 10f);
        ReShuffleAttackers();
    }

    // Update is called once per frame
    void Update()
    {
        if (batsWaiting > totalBats.Length)
        {
            batsWaiting = totalBats.Length;
        }

        totalBats = GameObject.FindGameObjectsWithTag("Bat");
        totalEnemies = GameObject.FindGameObjectsWithTag("Enemy");
    }

    private IEnumerator BatReposition()
    {
        yield return new WaitForSeconds(30f);
        if (batAI != null)
        {
            batAI.fly = GameObject.FindGameObjectWithTag("Flyer Target").GetComponent<Transform>();
            batAI.fly.position = new Vector2(playerPos.position.x + Random.Range(-10, 10), playerPos.position.y + Random.Range(10, 20));
        }
        StartCoroutine(BatReposition());
    }

    private void ReShuffleAttackers()
    {
        currentAttackers = 0;
        if (totalEnemies.Length < maxAttackers)
        {
            foreach (GameObject obj in totalEnemies)
            {
                Enemy enemy = obj.GetComponent<Enemy>();
                currentAttackers++;
                enemy.watching = false;
            }
        }
        else
        {
            foreach (GameObject obj in totalEnemies)
            {
                Enemy enemy = obj.GetComponent<Enemy>();
                if (currentAttackers < maxAttackers && enemy.watching)
                {
                    currentAttackers++;
                    enemy.watching = false;
                }
                else
                {
                    enemy.watching = true;
                }
            }
        }
    }
}
