using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class enemy : MonoBehaviour
{
    // 数值配置表：最大血量从这里读，需在 Inspector 里挂上 EnemyStats 资源
    [SerializeField] private EnemyStats_SO _stats;
    private float _health;   // 当前血量：运行时状态，初始值在 Start 里由配置表赋予
    public static Action<enemy> OnEnemyDeath;
    [SerializeField] private TMP_Text _HPtext;
    // 死亡时掉落的肉块预制体，玩家捡到它才获得金币
    [SerializeField] private GameObject _fleshPrefab;

    // 血量上限来自配置表，避免把数值写死在代码里
    void Start()
    {
        _health = _stats.maxHealth;
    }

    // 每帧刷新血量显示
    void Update()
    {
        _HPtext.text = _health.ToString();
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
