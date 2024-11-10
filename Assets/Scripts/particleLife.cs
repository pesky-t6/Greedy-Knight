using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class particleLife : MonoBehaviour
{
    [SerializeField] private float particleTime;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Lifetime());
    }

    private IEnumerator Lifetime()
    {
        yield return new WaitForSeconds(particleTime);
        Destroy(gameObject);
    }
}
