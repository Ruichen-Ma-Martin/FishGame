using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemy : MonoBehaviour
{
    // 数值配置表：最大血量从这里读，需在 Inspector 里挂上 EnemyStats 资源
    [SerializeField] private EnemyStats_SO _stats;
    private float _health;   // 当前血量：运行时状态，初始值在 Start 里由配置表赋予
    // 血量上限：运行时变量而不是每次直读 SO，和玩家的体力 / SAN 上限同理。
    // 以后要做"精英怪血量翻倍"之类的成长，改这个字段即可，不会污染配置表资源
    private float _maxHealth;
    public static Action<enemy> OnEnemyDeath;
    // 死亡时掉落的肉块预制体，玩家捡到它才获得金币
    [SerializeField] private GameObject _fleshPrefab;

    // 给血条用的只读入口：UI 只能读数值，改不了状态，
    // 免得有人绕过 TakeDamage 直接改血量
    public float CurrentHealth => _health;
    public float MaxHealth => _maxHealth;

    // 血量上限从配置表拷一份到运行时变量，开局给满
    void Start()
    {
        _maxHealth = _stats.maxHealth;
        _health = _maxHealth;
    }

    // 被子弹碰到时扣血
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("bullet"))
        {
            //Debug.Log("hit enemy");
            TakeDamage();
        }
    }

    // 按子弹伤害扣血，掉到 0 就死亡
    public void TakeDamage()
    {
        _health -= GameController.instance.bullet._damage;
        if (_health <= 0)
        {
            Die();
        }
    }

    // 掉肉、销毁自身，并通知监听者
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
