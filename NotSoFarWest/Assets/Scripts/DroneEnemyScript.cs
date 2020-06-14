using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneEnemyScript : DestroyableObject
{
    public Transform m_target;

    public bool shouldMove => Vector3.SqrMagnitude(m_target.position - transform.position) > (m_hoverDistance*m_hoverDistance);

    float m_angularspeed = 60f;
    float m_flySpeed = 5f;

    public float m_hoverDistance, m_minRadius;

    bool m_onknockBack, m_moving;

    Vector3 newPoint = Vector3.negativeInfinity;

    public Animator m_thisAnim;

    void Start(){
        m_target = FindObjectOfType<PlayerMovement>()?.transform;
        m_thisAnim = GetComponent<Animator>();
    }

    void Update(){
        HoverFacingPlayer();
        MoveCloserToPlayer();
    }

    private void HoverFacingPlayer()
    {
            if(m_target != null && !m_onknockBack){
                var direction = (m_target.position - transform.position);
                direction.y = 0f;
                direction.Normalize();
                float rotateAmount = Vector3.Cross(direction,transform.right).y;
                var turnAnimParam = Mathf.Clamp((rotateAmount*90) / m_angularspeed, -1,1);
                if(Mathf.Abs(rotateAmount) > 0.05f){
                    m_thisAnim.SetFloat("Blend",turnAnimParam);
                    transform.Rotate(new Vector3(0f,m_angularspeed * Time.deltaTime * (rotateAmount > 0 ? 1 : -1), 0f));
                }
            } 
    }

    void MoveCloserToPlayer(){
        var infi = Vector3.negativeInfinity;
        if(shouldMove){
            if(newPoint.Equals(infi) ||(m_target.position - newPoint).sqrMagnitude > (m_hoverDistance*m_hoverDistance)) newPoint = m_target.transform.position + UnityEngine.Random.insideUnitSphere * m_minRadius + new Vector3(0f,3f,0f);
            m_thisAnim.SetFloat("Forward", Mathf.Lerp(m_thisAnim.GetFloat("Forward"),1,.3f));
        }
        else {
            if(newPoint != Vector3.negativeInfinity && (transform.position - newPoint).sqrMagnitude < 1f){
                newPoint = Vector3.negativeInfinity;
            }     
        }
        if(!newPoint.Equals(infi)) {
            transform.position += (newPoint - transform.position).normalized *Time.deltaTime * m_flySpeed;
        } else m_thisAnim.SetFloat("Forward", Mathf.Lerp(m_thisAnim.GetFloat("Forward"),0,.3f));
    }

    IEnumerator Knockback(){
        m_onknockBack = true;
        yield return null;
    }
}
