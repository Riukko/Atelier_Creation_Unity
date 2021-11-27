using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour
{

    public float yScrollSpeed = 0.2f;
    public bool mouseMovement = false;
    public Transform otter;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (mouseMovement)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 2;
            mousePos = Camera.main.ScreenToWorldPoint(mousePos);
            transform.position = new Vector3(mousePos.x, transform.position.y, mousePos.z);

            if (Input.mouseScrollDelta.y != 0)
            {
                transform.position += Input.mouseScrollDelta.y < 0 ? new Vector3(0, -yScrollSpeed, 0) : new Vector3(0, yScrollSpeed, 0);
            }
        }

        transform.LookAt(otter);

        
    }
}
