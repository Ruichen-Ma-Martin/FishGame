using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 玩家数值配置：只放不会在运行时变化的常量，方便在 Inspector 里调参而不用改代码
// 运行时状态（当前血量、金币数）仍留在脚本里，不要放进来
[CreateAssetMenu(fileName = "PlayerStats", menuName = "ScriptableObjects/PlayerStats", order = 4)]
public class PlayerStats_SO : ScriptableObject
{
    [Header("生命")]
    public float maxHealth = 5f;       // 初始/最大血量
    public float healingAmount = 2f;   // 治疗量（买治疗商品回的血）

    [Header("移动")]
    public float moveSpeed = 5f;       // 移动速度
    public float acceleration = 40f;   // 加速度（速度变化率，越大越"干脆"，越小越像在水里滑行）

    [Header("转向")]
    public float turnSpeed = 270f;     // 鱼头转向鼠标的速度（度/秒），越大转身越快
    public float maxTiltAngle = 60f;   // 鱼头最大倾斜角度（度），限制它防止倒立

    [Header("射击")]
    public float shootCooldown = 0.5f; // 射击冷却（秒）
}
