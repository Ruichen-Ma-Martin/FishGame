using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 敌人数值配置：只放不会在运行时变化的常量，方便在 Inspector 里调参而不用改代码
// 运行时状态（当前血量、游荡方向、逃跑标记）仍留在脚本里，不要放进来
[CreateAssetMenu(fileName = "EnemyStats", menuName = "ScriptableObjects/EnemyStats", order = 5)]
public class EnemyStats_SO : ScriptableObject
{
    [Header("生命")]
    public float maxHealth = 3f;               // 最大血量

    [Header("游荡（水平）")]
    public float minWanderSpeed = 1f;          // 游荡速度下限
    public float maxWanderSpeed = 3f;          // 游荡速度上限
    public float minChangeInterval = 1.5f;     // 重新随机间隔下限（秒）
    public float maxChangeInterval = 3f;       // 重新随机间隔上限（秒）

    [Header("漂浮（垂直）")]
    public float minFloatingSpeed = 1f;        // 垂直漂浮速度下限
    public float maxFloatingSpeed = 3f;        // 垂直漂浮速度上限

    [Header("逃跑")]
    public float fleeDistance = 4f;            // 玩家进入此距离开始逃跑
    public float fleeSpeed = 5f;               // 逃跑速度
    public float fleeExitBuffer = 1.5f;        // 解除逃跑要多拉开的缓冲距离
    public float fleeDirectionDeadzone = 0.5f; // 逃跑方向死区（水平距离小于此值时沿用旧方向）

    [Header("攻击")]
    public float damage = 1f;                  // 攻击伤害
    public float attackLifetime = 0.5f;        // 攻击碰撞体持续时间（秒）
}
