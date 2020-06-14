using UnityEngine;
using System.Collections.Generic;

public class KVR32 : BaseGun {
    
    protected override void InstantiateShots(Vector3 point, Quaternion ammoRot){
        for(int i=0; i< 12; i++){
            var bulletRotation = BulletSpreadRotation(point);
            Instantiate(m_bulletPrefab,m_tipOfTheGun.position,bulletRotation);
        }
    }
}