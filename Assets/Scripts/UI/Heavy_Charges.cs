using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Heavy_Charges : MonoBehaviour
{
    public int heavyCharges = 3;
    private int maxHeavyCharges = 3;
    private float heavyChargeTimer = 5f;
    public bool charging = false;

    private Material[] heavyIconMat;
    private Material heavyGlow;
    private Coroutine heavyFlasher;
    private Player_Attack pa;

    [ColorUsage(true, true)]
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashTime = 0.25f;
    [SerializeField] private Material defMat;
    [SerializeField] private GameObject heavyFlash;
    [SerializeField] private Image[] heavyIcon;

    // Start is called before the first frame update
    void Start()
    {
        pa = GetComponent<Player_Attack>();

        heavyIconMat = new Material[heavyIcon.Length];
        for (int i = 0; i < heavyIcon.Length; i++)
        {
            heavyIconMat[i] = heavyIcon[i].material;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (heavyCharges < maxHeavyCharges)
        {
            ShowHeavyIcon();
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

    //Updates heavy charge
    private void ShowHeavyIcon()
    {
        heavyIcon[heavyCharges].fillAmount += 1 / (heavyChargeTimer) * Time.deltaTime;
    }

    //Uses a heavy charge
    public void HeavyUpdate()
    {
        pa.heavying = true;
        heavyCharges--;
        heavyIcon[heavyCharges].fillAmount = 0;
        if (charging)
        {
            heavyIcon[heavyCharges].fillAmount = heavyIcon[heavyCharges + 1].fillAmount;
            heavyIcon[heavyCharges + 1].fillAmount = 0;
        }
        ChargeHeavy();
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

    //Red flash to indicate heavy attack
    private void HeavyFlash()
    {
        if (pa.heavying)
        {
            Instantiate(heavyFlash, pa.heavyPoint.position, Quaternion.identity, pa.heavyTrail.transform);
        }
    }
}
