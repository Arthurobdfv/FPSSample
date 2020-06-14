using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public DestroyableObject m_healthContext; 

    TMPro.TMP_Text m_healthText;
    Image m_healthBarSlider;

    Transform m_target;
    void Start()
    {
        SetUpFields();
    }

    void Update() {
        RotateBarToFacePlayer();
    }


    void OnHealthChange(int newValue){
        if(newValue <= 0){
            m_healthContext.HealthChanged -= OnHealthChange;
            Destroy(gameObject);
        }
        else{
            m_healthText.text = $"{m_healthContext.Health}/{m_healthContext.m_maxHealth}";
            m_healthBarSlider.fillAmount = (float)m_healthContext.Health / (float)m_healthContext.m_maxHealth;
        }
    }

    void SetUpFields(){
        m_target = Camera.main.transform;
        m_healthContext = GetComponentInParent<DestroyableObject>();
        m_healthText = GetComponentInChildren<TMPro.TMP_Text>();
        m_healthBarSlider = GetComponentInChildren<Image>();
        if(m_healthContext == null) {
            Debug.LogError($"HealthContext is null - {gameObject}");
        }
        else{
            m_healthContext.HealthChanged += OnHealthChange;
            OnHealthChange(m_healthContext.m_maxHealth);
        }
    }
    private void RotateBarToFacePlayer()
    {
        var positionToFace = transform.position - m_target.position;
        positionToFace.y = 0f;
        transform.rotation = Quaternion.LookRotation(positionToFace);
    }

}
