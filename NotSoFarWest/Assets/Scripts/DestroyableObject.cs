using UnityEngine;
public class DestroyableObject : MonoBehaviour, IProjectable
{
    private int m_health;

    public int Health {
        get { return m_health; }
        private set {
            m_health = value;
            HealthChanged?.Invoke(m_health);
        }
    }
    public int m_maxHealth;

    void Awake(){
        m_health = m_maxHealth;
    }

    public void OnHit(int Damage)
    {
        Health -= Damage;
        if(m_health <= 0) DestroyThis();
    }

    void DestroyThis(){
        Destroy(gameObject);
    }

    public delegate void OnHealthChange(int newValue);

    public event OnHealthChange HealthChanged;
} 