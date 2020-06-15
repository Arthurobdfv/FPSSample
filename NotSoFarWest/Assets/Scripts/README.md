# Scripts and Architecture
## Welcome to the Scripts Session!
Here I'll brefly explain the project architecture and some design decisions that I've made so this project is extensible and easy to maintain, quick disclaimer, I'm still learning about SOLID principle and techiques so it might not be perfect but I'm sure I'm going in the right way of learning! :)

## Decision Making
First of all, before even starting the implementations I've made some decisions about the end game, for sure they might change a little bit during the development process but I've got some solid decisions that are the core mechanics I need to the game.

Game mechanics:
* Multiple Weapons - The player shoud be able to use multiple weapon types;
* Primary Secondary Weapon Carrying - Just like a lot of FPS games, the player should be able to carry one primary and one secondary weapon.
* Multiple enemies - I want the game to have some different kind of enemies, flying hovering drones and some humanoid enemies;
* Item Collection - That should be a drop and collect item system so the plyer can get some ammo / life item from the ground;
* Crates and othe destroyable objects - The game or enemies will randomply spawn crates or drops on the ground such as health recovery or ammo;

With those mechanics in mind I was able to "sketch" up the game architecture, first of all let's talk about input and shooting (the core mechanic of any FPS right!? :))

### Pew Pew!

As I've mentioned earlier I had plans on adding different types of weapons in the game. Those weapons might or might not have different implementations and for that, I've decided to decouple the Input System to the Weapon carried by the player, and the solution I've thought about was using an Interface for that so, the input manager would have a reference for the IGun interface that the player is carrying and that interface has only a ```void Shoot();``` method.

Each of the guns I want to implement, should inherit from that interface and also implement it on its own way but as most of the weapons would have some exact identical steps on shooting, I've decided to go for a BaseGun class that would have some method implementations.

#### IGun Interface
<details>
 <summary>Expand...</summary>
  
<p>
    
```c#
public interface IGun {
  void Shoot();
}
```

</p>
</details>

#### Base Gun Class
<details>
 <summary>Expand...</summary>
  
<p>
  
```c#
public class BaseGun : MonoBehaviour, IGun {
    ...
    public void Shoot(Vector3 point, bool firstPress){
        if(m_delayBtnShots < m_fireRate) return;
        if((m_mode == ShootMode.Tap || m_mode == ShootMode.SemiAuto) && !firstPress) return;
        var ammoRotation = BulletSpreadRotation(point);
        InstantiateShots(point, ammoRotation);    
        m_delayBtnShots = 0f;
    }

    protected virtual void InstantiateShots(Vector3 point, Quaternion ammoRot){
        var child = Instantiate(m_bulletPrefab, m_tipOfTheGun.position,ammoRot);
    }

    void Update(){
        m_delayBtnShots += Time.deltaTime;
    }
    ...
}
```
</p>
</details>

#### Input Manager Code (Shoot Handling)

<details>
 <summary>Expand...</summary>
  
<p>
  
```c#
...
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
...
```

</p>
</details>

With that code structure is really easy to create new type of weapons, all the fields on the ```BaseGun.cs``` class are assignable on the Inspector. Down here I'll show the code from two new weapons I've implemented to show that architecture, one of them ```KA74``` is a Fully Automatic gun, that has the same implementation as the Base Gun but just with a different value on the ```ShootMode``` enum, and the  ```KVR32``` that is a Shotgun.

#### KA74.cs
```c#
public class KA74 : BaseGun {
    // protected override void InstantiateShots(Vector3 point) {
    //     Debug.Log("Calling KA75");
    // }
}
```
As you can see the KA74 class doesn't have anything in it, it's just inheriting from the BaseGun and has its fields changed in the inspector.

#### KVR32.cs
```c#
public class KVR32 : BaseGun {    
    protected override void InstantiateShots(Vector3 point, Quaternion ammoRot){
        for(int i=0; i< 12; i++){
            var bulletRotation = BulletSpreadRotation(point);
            Instantiate(m_bulletPrefab,m_tipOfTheGun.position,bulletRotation);
        }
    }
}
```
The KVR32 class needed a different implementation because the Shotgun behaves differently, it shots more than one bullet, so i just needed to override the ```InstantiateShots``` method from its parend and that's it, the BaseGun class ```Shoot``` method will call the overridden ```InstantiateShots```one.
