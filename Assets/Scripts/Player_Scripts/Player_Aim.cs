using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Aim : MonoBehaviour
{
    public float xValue;
    public float yValue;

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        xValue = mousePosition.x - transform.position.x;
        yValue = mousePosition.y - transform.position.y;


        Vector2 direction = new Vector2(xValue, yValue);

        transform.up = direction;
    }
}
