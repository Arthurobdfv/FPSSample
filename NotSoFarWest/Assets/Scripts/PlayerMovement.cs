using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController m_thisController;
    public float m_speed;
    public float m_jumpHeight;
    public float m_gravity = -Physics.gravity.magnitude;

    Vector3 m_velocity;

    public Transform m_groundCheck;
    public float m_sphereRadius;
    public LayerMask m_groundMask;
    public bool m_isGrounded;

    void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        m_isGrounded = Physics.CheckSphere(m_groundCheck.position,m_sphereRadius,m_groundMask);
        if(m_isGrounded && m_velocity.y < 0f) m_velocity.y = -2f;

        var x = Input.GetAxis("Horizontal");
        var y = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * y;
        m_thisController.Move(move * m_speed * Time.deltaTime);


        if(Input.GetButton("Jump") && m_isGrounded) m_velocity.y = Mathf.Sqrt(m_jumpHeight * -2f * m_gravity);
        m_velocity.y += m_gravity * Time.deltaTime;

        m_thisController.Move(m_velocity * Time.deltaTime);
        
    }
}
