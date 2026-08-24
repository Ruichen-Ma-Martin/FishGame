using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyattack : MonoBehaviour
{
    private float _lifetime = 0.5f;
    private float _timer = 0f;
    public float _damage = 1f;

    private void OnEnable()
    {
        _timer = _lifetime;
    }
    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
    
}
