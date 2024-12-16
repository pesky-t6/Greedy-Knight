using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

public class Execute : MonoBehaviour
{
    [SerializeField] private Transform enemyDummies;
    [SerializeField] private Transform playerDummy;
    private string[] anims = { "Ogre_sword" };
    private Animator anim;
    public string enemyName = null;
    public string weaponType = null;
    public float enemySizex = 1;
    public float enemySizey = 1;
    public float exeDirection = 1;
    private Transform enemyDummy = null;
    public GameObject enemy;
    private Enemy enemyScript;
    private GameObject player;
    private Player_Movement pm;
    private Player_Life pl;
    private Game_Manager gm;
    private bool executed;

    // Start is called before the first frame update
    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Game_Manager>();
        player = GameObject.FindGameObjectWithTag("Player");
        pm = player.GetComponent<Player_Movement>();
        pl = player.GetComponent<Player_Life>();
        enemyScript = enemy.GetComponent<Enemy>();
        anim = GetComponent<Animator>();

        if (exeDirection < 0) { exeDirection = -1; }
        else if (exeDirection > 0) { exeDirection = 1; }

        //Actually plays the animation if it exists
        string animName = enemyName + "_" + weaponType;
        if (anims.Contains(animName))
        {
            pm.SetStatic();
            pl.canTakeDamage = false;
            pl.Hide();
            gm.ExecutionStart(playerDummy);
            enemy.SetActive(false);
            anim.Play(animName);
            foreach (Transform child in enemyDummies)
            {
                foreach (Transform kid in child)
                {
                    if (kid.name != enemyName)
                    {
                        kid.gameObject.SetActive(false);
                    }
                    else
                    {
                        kid.gameObject.SetActive(true);
                        enemyDummy = kid;
                    }
                }
            }
            enemyDummy.transform.localScale = new Vector3(enemySizex, enemySizey, transform.localScale.z);
            transform.localScale = new Vector3(-(int)exeDirection, 1f, 1f);
            executed = true;
        }
        else
        {
            Finished();
        }
    }

    private void Finished()
    {
        if (executed)
        {
            player.gameObject.transform.position = playerDummy.position;
            pm.SetDynamic();
            pl.canTakeDamage = true;
            pl.Show();
            gm.ExecutionStop();
            enemy.SetActive(true);
        }
        enemyScript.DeathFinish();
        Destroy(gameObject);
    }
}
