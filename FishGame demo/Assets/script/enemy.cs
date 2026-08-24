using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class enemy : MonoBehaviour
{
    private float _health = 3f;
    public static Action<enemy> OnEnemyDeath;
    [SerializeField] private TMP_Text _HPtext;
        void Update()
        {
            _HPtext.text = _health.ToString();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("bullet"))
        {
            //Debug.Log("hit enemy");
            TakeDamage();
        }
    }
    public void TakeDamage()
    {
        _health -= GameController.instance.bullet._damage;
        if (_health <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Destroy(gameObject);
        OnEnemyDeath?.Invoke(this);
    }
}
