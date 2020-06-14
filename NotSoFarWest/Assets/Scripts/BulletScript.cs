using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float m_speed;

    public float m_selfDestructTime;

    private bool alreadyHit = false;

    float m_elapsedTime;

    void FixedUpdate(){
        if(m_elapsedTime > m_selfDestructTime) DestroyThis(false);
        transform.position += transform.up * m_speed * Time.deltaTime;
        m_elapsedTime += Time.deltaTime;
    }

    void OnTriggerEnter(Collider col){
        if(alreadyHit) return;
        alreadyHit = true;
        var hitObj = col.gameObject.GetComponent<IProjectable>();
        if(hitObj != null) hitObj.OnHit(1);
        DestroyThis(true);
    }

    void DestroyThis(bool onhit){
        if(onhit) BulletHit?.Invoke();
        Destroy(gameObject);
    }

    public delegate void OnBulletHit();

    public static event OnBulletHit BulletHit;
}
