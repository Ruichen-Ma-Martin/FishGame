using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemyattack : MonoBehaviour
{
    // 数值配置表：伤害和攻击持续时间从这里读，需在 Inspector 里挂上 EnemyStats 资源
    [SerializeField] private EnemyStats_SO _stats;
    private float _timer = 0f;

    // 攻击命中玩家时广播，参数是本次伤害值。
    // 用静态事件而不是让玩家反向读 enemyattack 的字段：双方只通过一个事件耦合，
    // 攻击方也不需要持有玩家引用，和 enemy.OnEnemyDeath、Flesh.OnCollected 是同一套写法
    public static Action<float> OnPlayerHit;

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

    // 攻击碰撞体命中玩家：把伤害值广播出去，由玩家自己决定要不要吃这次伤害（无敌帧）。
    // 这里用组件判断而不是比对标签：标签拼错只会静默失效，组件判断有类型保证
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out player _))
        {
            OnPlayerHit?.Invoke(_stats.damage);
        }
    }
}
