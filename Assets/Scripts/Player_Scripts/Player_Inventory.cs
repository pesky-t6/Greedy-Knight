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

    private string input;
    private GameObject projectile;

    [Header("Weapons:")]

    public bool unnarmed = true;
    public bool swordEquipped = false;
    public bool shieldEquipped = false;

    private GameObject sword;
    private GameObject shield;

    public int weaponState;
    public enum WeaponState { sword, axe, spear, hammer };

    private void Awake()
    {
        sword = weapons[0];
        shield = weapons[1];
        shield.SetActive(false);
    }

    private void Update()
    {
        if (!attack.swordThrown && !attack.blocking)
        {
            unnarmed = false;
            swordEquipped = true;
            shieldEquipped = false;
        }
        else if (attack.blocking)
        {
            unnarmed = false;
            swordEquipped = false;
            shieldEquipped = true;
        }
        else
        {
            unnarmed = true;
            swordEquipped = false;
            shieldEquipped = false;
        }
        sword.SetActive(swordEquipped);
        shield.SetActive(shieldEquipped);
    }

    //Returns true if player is armed
    public bool IsArmed()
    {
        if (swordEquipped)
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
        else if (shieldEquipped)
        {
            projectile = Instantiate(shield, aim.position, aim.rotation);
        } 
    }
}
