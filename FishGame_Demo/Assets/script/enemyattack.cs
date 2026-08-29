using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyattack : MonoBehaviour
{
    // 数值配置表：伤害和攻击持续时间从这里读，需在 Inspector 里挂上 EnemyStats 资源
    [SerializeField] private EnemyStats_SO _stats;
    private float _timer = 0f;
    // 保持 _damage 名称，player.cs 通过 enemyattack._damage 读取伤害
    public float _damage => _stats.damage;

    // 每次启用时重置剩余时间，持续时间来自配置表
    private void OnEnable()
    {
        _timer = _stats.attackLifetime;
    }

    // 倒计时结束就关掉攻击碰撞体
    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
}
