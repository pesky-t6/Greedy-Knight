using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamController : MonoBehaviour
{
    [SerializeField] private Transform player;
    private Transform cameraFocus;

    private void Start()
    {
        cameraFocus = player;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(cameraFocus.position.x, cameraFocus.position.y, transform.position.z);
    }

    public void Focus(Transform pos)
    {
        cameraFocus = pos;
    }
}
