using System.Collections;
using System.Collections.Generic;
using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class Player_Life : MonoBehaviour
{
    [SerializeField] private GameObject damageNum;
    private float currentHealth = 100;
    private float maxHealth = 100;
    public float currentBlockAmount = 50;
    private float maxBlockAmount = 50;
    private Rigidbody2D rb;
    private Coroutine healthRoutine = null;
    private Coroutine blockRoutine = null;
    private Color defColor = Color.white;
    private bool fading = false;

    public Image healthBar;
    public Image blockBar;
    private DamageNumber damageNumHolder;
    private Player_Attack pa;
    private Player_Movement pm;

    public bool canTakeDamage = true;

    // Start is called before the first frame update
    void Start()
    {
        pm = GetComponent<Player_Movement>();
        pa = GetComponent<Player_Attack>();
        rb = GetComponent<Rigidbody2D>();
        TakeDamage(50f, Color.white);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        healthBar.fillAmount = currentHealth / maxHealth;

        if (currentBlockAmount > maxBlockAmount)
        {
            currentBlockAmount = maxBlockAmount;
        }
        blockBar.fillAmount = currentBlockAmount / maxBlockAmount;

        if (currentBlockAmount == maxBlockAmount && !fading)
        {
            fading = true;
            StartCoroutine(BarFade(blockBar));
        }
    }

    public void TakeDamage(float damage, Color damageColor)
    {      
        if (canTakeDamage)
        {
            StartCoroutine(BarLerp(currentHealth, currentHealth - damage, maxHealth, healthBar));
            currentHealth -= damage;
            damageNumHolder = Instantiate(damageNum, rb.transform.position, Quaternion.identity).GetComponent<DamageNumber>();
            damageNumHolder.value = Mathf.Round(damage);
            damageNumHolder.color = damageColor;
            if (healthRoutine != null) StopCoroutine(healthRoutine);
            if (currentHealth < maxHealth)
            {
                healthRoutine = StartCoroutine(HealthRegen());
            }
        }
    }

    //Take block damage
    public void TakeBlockDamage(float damage)
    {

        StartCoroutine(BarLerp(currentBlockAmount, currentBlockAmount - damage, maxBlockAmount, blockBar));
        currentBlockAmount -= damage;

        StopCoroutine(BarFade(blockBar));
        Color c = blockBar.color;
        c.a = 1;
        blockBar.color = c;
        fading = false;

        damageNumHolder = Instantiate(damageNum, rb.transform.position, Quaternion.identity).GetComponent<DamageNumber>();
        damageNumHolder.value = Mathf.Round(damage);
        damageNumHolder.color = Color.gray;
        if (blockRoutine != null) StopCoroutine(blockRoutine);
        if (currentBlockAmount < maxBlockAmount)
        {
            blockRoutine = StartCoroutine(BlockRegen());
        }
    }

    //Edge case
    public void KeepRegeningBlock()
    {
        if (blockRoutine != null) StopCoroutine(blockRoutine);
        if (currentBlockAmount < maxBlockAmount)
        {
            blockRoutine = StartCoroutine(BlockRegen());
        }
    }

    //Lerp for smoother damage
    private IEnumerator BarLerp(float beforeValue, float afterValue, float maxValue, Image image)
    {
        float currentHealthAmount = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;

            currentHealthAmount = Mathf.Lerp(beforeValue, afterValue, (elapsedTime / 0.2f));
            image.fillAmount = currentHealthAmount / maxValue;
            yield return null;
        }
    }

    private IEnumerator BarFade(Image image)
    {
        for (float i = 1f; i >= -0.05f; i -= 0.05f)
        {
            Color c = image.color;
            c.a = i;
            image.color = c;
            yield return new WaitForSeconds(0.02f);
        }
    }

    public void Knockback(Vector3 pos, float value)
    {
        //Calculates unit vector (Im a genius)
        Vector3 dirVector = new Vector3(transform.position.x, transform.position.y, 0f) - new Vector3(pos.x, pos.y, 0f);
        Vector3 unitVector = (dirVector) / (dirVector.magnitude);
        rb.velocity = new Vector2(rb.velocity.x + unitVector.x * value, rb.velocity.y + unitVector.y * value);
    }

    public void Knockback(Vector3 pos, float value, int type)
    {
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

    //Easier access to the method (dont need to use start coroutine)
    public void Hit(float damage, Color color, Vector3 pos, float value)
    {
        StartCoroutine(HitCoroutine(damage, color, pos, value));
    }

    //Method for freeze framing and taking damage and knockback
    private IEnumerator HitCoroutine(float damage, Color color, Vector3 pos, float value)
    {
        pm.anim.speed = 0f;
        yield return new WaitForSeconds(0.05f);
        pm.anim.speed = 1f;
        TakeDamage(damage, color);
        Knockback(pos, value);
    }

    private IEnumerator HealthRegen()
    {
        yield return new WaitForSeconds(2.5f);
        for (int i = 0; i < 100; i++)
        {
            if (currentHealth < maxHealth)
            {
                currentHealth++;
                yield return new WaitForSeconds(0.05f);
            }
            else { break; } 
        }
    }

    private IEnumerator BlockRegen()
    {
        yield return new WaitForSeconds(5f);
        for (int i = 0; i < 100; i++)
        {
            if (pa.blocking)
            {
                KeepRegeningBlock();
                break;
            }
            if (currentBlockAmount < maxBlockAmount)
            {
                currentBlockAmount++;
                yield return new WaitForSeconds(0.05f);
            }
            else { break; }
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
    }
}
