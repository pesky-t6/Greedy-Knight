using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Player_Look : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform pivot;
    public bool isFacingRight = true;
    public float xValue;
    public float yValue;
    public float maxZRot = 20f;
    public float minZRot = -20f;
    public bool canTurn = true;
    public int dir;

    // Update is called once per frame
    void Update()
    {
        if (isFacingRight)
        {
            dir = 1;
        }
        else
        {
            dir = -1;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        xValue = mousePosition.x - pivot.transform.position.x;
        yValue = mousePosition.y - pivot.transform.position.y;

        Vector2 direction = new Vector2(xValue, yValue);

        transform.up = direction;
        LimitRot();
        Flip();
    }

    private void Flip()
    {
        if (canTurn)
        {
            if (isFacingRight && xValue < -0.00000000001 || !isFacingRight && xValue > 0)
            {
                isFacingRight = !isFacingRight;


                Vector3 localScale = player.transform.localScale;
                localScale.x *= -1f;
                player.transform.localScale = localScale;
            }
        }
    }

    private void LimitRot()
    {
        Vector3 playerEulerAngles = transform.localRotation.eulerAngles;

        playerEulerAngles.z = (playerEulerAngles.z > 540) ? playerEulerAngles.z - 360 : playerEulerAngles.z;
        playerEulerAngles.z = Mathf.Clamp(playerEulerAngles.z, minZRot, maxZRot);

        transform.localRotation = Quaternion.Euler(playerEulerAngles);
    }
}
