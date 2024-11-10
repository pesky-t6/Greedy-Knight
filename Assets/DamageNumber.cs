using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.Texture2DShaderProperty;

public class DamageNumber : MonoBehaviour
{
    private TextMeshPro text;
    private float rotation;
    public float value;
    public Color color;

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshPro>();
        text.text = value.ToString();
        text.color = color;
        if (color == Color.red)
        {
            text.fontSize *= 2;
        }
        rotation = Random.Range(-15f, 15f);
        transform.position = new Vector3(transform.position.x + Random.Range(-0.1f, 0.1f), transform.position.y + Random.Range(-0.1f, 0.1f), 1);
        transform.Rotate(0f, 0f, rotation);
        StartCoroutine(Fade());
        StartCoroutine(Fall());
    }

    //Causes the number to fade away
    private IEnumerator Fade()
    {
        yield return new WaitForSeconds(0.5f);
        for (float i = 1f; i >= -0.05f; i -= 0.05f)
        {
            Color c = text.color;
            c.a = i;
            text.color = c;
            yield return new WaitForSeconds(0.02f);
        }
        Destroy(gameObject);
    }

    //Makes the numbers fall (looks cool)
    private IEnumerator Fall()
    {
        yield return new WaitForSeconds(0.1f);

        float currentPos = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;

            currentPos = Mathf.Lerp(transform.position.y, transform.position.y - 0.001f, (elapsedTime));
            transform.position = new Vector3(transform.position.x ,currentPos, transform.position.z);

            yield return null;
        }
    }
}
