using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class player : MonoBehaviour
{
    private float _health = 5f;
    [SerializeField] private TMP_Text _CoinsNumber;
    [SerializeField] private TMP_Text _HPNumber;
    public enemyattack enemyattack;

    public float _Coins = 0;

    void Start()
    {
        enemy.OnEnemyDeath += getCoins;
        _CoinsNumber.text = _Coins.ToString();
        _HPNumber.text = _health.ToString();
    }
    private void Update()
    {
        _CoinsNumber.text = _Coins.ToString();
         _HPNumber.text = _health.ToString();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("enemyhitbox"))
        {
           //Debug.Log("Player hit by enemy!");
            TakeDamage();
        }
        
    }
    void TakeDamage()
    {
        StartCoroutine(GameController.instance.GetDamageEffect.DamageEffect());
        _health -= enemyattack._damage;
        _HPNumber.text = _health.ToString();
        if (_health <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Destroy(gameObject,0.2f);
        GameController.instance.BackToMain();
    }
    public void healing()
    {
        _health += 2f;
    }

    void getCoins(enemy enemy)
    {
        if (enemy.tag == "enemy")
        {
            _Coins ++;
            
            _CoinsNumber.text = _Coins.ToString();
        }
            
    }


}
