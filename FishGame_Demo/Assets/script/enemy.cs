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
    // 死亡时掉落的肉块预制体，玩家捡到它才获得金币
    [SerializeField] private GameObject _fleshPrefab;
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
        DropFlesh();
        Destroy(gameObject);
        OnEnemyDeath?.Invoke(this);
    }

    // 在死亡位置生成肉块。金币不再由击杀直接给出，必须由玩家游过去捡
    private void DropFlesh()
    {
        if (_fleshPrefab == null)
        {
            Debug.LogWarning("enemy 预制体没有设置 _fleshPrefab，死亡不会掉落肉块。", this);
            return;
        }

        Instantiate(_fleshPrefab, transform.position, Quaternion.identity);
    }
}
