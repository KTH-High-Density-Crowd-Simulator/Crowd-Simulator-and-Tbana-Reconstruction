using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSmovement : MonoBehaviour
{
    public float speed = 5f;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;        
    }
    public float mouseSensitivity = 200f;

    float xRotation = 2.0f;
    float yRotation = 2.0f;
    
    float yaw = 0.0f;
    float pitch = 0.0f;
    // Update is called once per frame
    void Update()
    {
        yaw += xRotation * Input.GetAxis("Mouse X");
        pitch -= yRotation * Input.GetAxis("Mouse Y");
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);     
        
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        Vector3 move = new Vector3(moveX, 0f, moveZ);

        transform.Translate(move * speed * Time.deltaTime);   
    }
}
