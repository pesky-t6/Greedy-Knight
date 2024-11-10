using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Player_Inventory : MonoBehaviour
{

    [SerializeField] private Transform aim;

    [SerializeField] private Player_Attack attack;
    [SerializeField] private GameObject[] weapons;

    public bool[] isFull;
    public GameObject[] slots;
    private string input;
    private int currentSlot = 0;
    private Pickup pickup;
    private GameObject projectile;

    [Header("Weapons:")]

    public bool unnarmed = true;
    public bool swordEquipped = false;
    public bool axeEquipped = false;
    public bool spearEquipped = false;
    public bool shieldEquipped = false;
    public bool hammerEquipped = false;

    [SerializeField] private GameObject sword;
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject spear;
    [SerializeField] private GameObject shield;
    [SerializeField] private GameObject hammer;

    public int weaponState;
    private enum WeaponState { sword, axe, spear, hammer };

    private void Awake()
    {
        foreach (GameObject kid in weapons)
        {
            kid.SetActive(false);
        }
    }

    //Returns true if player is armed
    public bool IsArmed()
    {
        if (swordEquipped || axeEquipped || spearEquipped || hammerEquipped)
        {
            return true;
        }
        else { return false; }
    }

    //Drop the current item
    public void DropItem()
    {
        //Makes a projectile based on what you threw
        if (unnarmed)
        {
            return;
        }
        else if (swordEquipped)
        {
            projectile = Instantiate(sword, aim.position, aim.rotation);
        }
        else if (axeEquipped)
        {
            projectile = Instantiate(axe, aim.position, aim.rotation);
        }
        else if (spearEquipped)
        {
            projectile = Instantiate(spear, aim.position, aim.rotation);
        }
        else if (shieldEquipped)
        {
            projectile = Instantiate(shield, aim.position, aim.rotation);
        }
        else if (hammerEquipped)
        {
            projectile = Instantiate(hammer, aim.position, aim.rotation);
        }

        //Tell the projectile that its a thrown pickup
        pickup = projectile.GetComponent<Pickup>();
        pickup.thrown = true;
        
        //Destroy the children
        foreach (Transform child in slots[currentSlot].transform)
        {
            slots[currentSlot].transform.DetachChildren();
            GameObject.Destroy(child.gameObject);
            isFull[currentSlot] = false;
        }

        //Weapon in hand check
        CheckWeapons();
    }   

    private void Update()
    {
        //Gets user input
        input = Input.inputString;

        //If input is a valid slot number continue and not attacking
        if ((input == "1" || input == "2" || input == "3") && !attack.attacking)
        {
            //change string to int
            currentSlot = int.Parse(input) - 1;
            CheckWeapons();
        }
    }

    public void CheckWeapons()
    {
        //If no child declare as unnarmed
        if (slots[currentSlot].transform.childCount == 0)
        {
            unnarmed = true;
            swordEquipped = false;
            axeEquipped = false;
            spearEquipped = false;
            shieldEquipped = false;
            hammerEquipped = false;

            foreach (GameObject kid in weapons)
            {
                kid.SetActive(false);
            }
        }
        else
        {
            foreach (Transform child in slots[currentSlot].transform)
            {
                //checks with tag it is and equips it
                if (child.CompareTag("Sword"))
                {
                    unnarmed = false;
                    swordEquipped = true;
                    axeEquipped = false;
                    spearEquipped = false;
                    shieldEquipped = false;
                    hammerEquipped = false;
                    weaponState = (int) WeaponState.sword;
                }

                else if (child.CompareTag("Axe"))
                {
                    unnarmed = false;
                    swordEquipped = false;
                    axeEquipped = true;
                    spearEquipped = false;
                    shieldEquipped = false;
                    hammerEquipped = false;
                    weaponState = (int)WeaponState.axe;
                }

                else if (child.CompareTag("Spear"))
                {
                    unnarmed = false;
                    swordEquipped = false;
                    axeEquipped = false;
                    spearEquipped = true;
                    shieldEquipped = false;
                    hammerEquipped = false;
                    weaponState = (int)WeaponState.spear;
                }

                else if (child.CompareTag("Shield"))
                {
                    unnarmed = false;
                    swordEquipped = false;
                    axeEquipped = false;
                    spearEquipped = false;
                    shieldEquipped = true;
                    hammerEquipped = false;
                }

                else if (child.CompareTag("Hammer"))
                {
                    unnarmed = false;
                    swordEquipped = false;
                    axeEquipped = false;
                    spearEquipped = false;
                    shieldEquipped = false;
                    hammerEquipped = true;
                    weaponState = (int)WeaponState.hammer;
                }

                foreach (GameObject kid in weapons)
                {
                    //if same tag enable
                    if (child.CompareTag(kid.tag))
                    {
                        kid.SetActive(true);
                    }
                    //else disable
                    else
                    {
                        kid.SetActive(false);
                    }
                }
            }
        }
    }
}
