using UnityEngine;
public class BaseGun : MonoBehaviour, IGun {

    public Transform m_tipOfTheGun;
    public GameObject m_bulletPrefab;
    public float m_fireRate;
    float m_delayBtnShots;
    public ShootMode m_mode;
    public float m_shootSpread;
    public void Shoot(Vector3 point, bool firstPress){
        if(m_delayBtnShots < m_fireRate) return;
        if((m_mode == ShootMode.Tap || m_mode == ShootMode.SemiAuto) && !firstPress) return;
        var ammoRotation = BulletSpreadRotation(point);
        InstantiateShots(point, ammoRotation);    
        m_delayBtnShots = 0f;
    }

    protected virtual void InstantiateShots(Vector3 point, Quaternion ammoRot){
        Debug.Log("Calling base");
        var child = Instantiate(m_bulletPrefab, m_tipOfTheGun.position,ammoRot);
    }

    void Update(){
        m_delayBtnShots += Time.deltaTime;
    }

    protected Quaternion BulletSpreadRotation(Vector3 point){
        var randomness = Random.insideUnitSphere*m_shootSpread;
        Debug.Log(randomness);
        var fullrot = Quaternion.Euler(Quaternion.LookRotation(point-m_tipOfTheGun.position + randomness).eulerAngles + new Vector3(90f,0f,0f));
        return fullrot;
    }
}