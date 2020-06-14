using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float m_sensibility;

    public Transform m_playerBody;

    float xRotation = 0f;
    
    IGun m_currentGun;

    void Start(){
        Physics.IgnoreLayerCollision(9,9,true);
        SetUp();
    }


    void LateUpdate()
    {
        HorizontalMouse();
    }

    void Update()
    {
        Shooting();
    }
    private void Shooting()
    {
        bool firstPress = Input.GetMouseButtonDown(0);
        bool holding = Input.GetMouseButton(0);
        if(firstPress || holding) {
            var ray = Camera.main.ViewportPointToRay(new Vector3(.5f,.5f, 0f));
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit)){
                m_currentGun?.Shoot(hit.point, firstPress);
            }
            else{
                m_currentGun?.Shoot(transform.forward * 1000f + transform.position, firstPress);
            }
        }
    }

    void HorizontalMouse(){
        float mouseX = Input.GetAxis("Mouse X") * m_sensibility * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * m_sensibility * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation,-90f,90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        m_playerBody.Rotate(Vector3.up, mouseX);
    }

    private void SetUp()
    {
        Cursor.lockState = CursorLockMode.Locked;
        m_currentGun = GetComponentInChildren<IGun>();
    }
}
