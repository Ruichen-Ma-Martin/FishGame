using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 关卡配置：水流边界和摄像机跟随，只放不会在运行时变化的常量
// 墙（tag "wall"）的位置本身不放进来，由脚本在 Start 里从场景物体计算
[CreateAssetMenu(fileName = "LevelConfig", menuName = "ScriptableObjects/LevelConfig", order = 6)]
public class LevelConfig_SO : ScriptableObject
{
    [Header("水流边界")]
    public float forceRadius = 3f;      // 力场半径：玩家离墙内侧多近开始受排斥力
    public float forceStrength = 8f;    // 排斥力强度：贴墙时把它推离的速度（越大推得越狠）

    [Header("摄像机")]
    public float cameraSmoothTime = 0.2f;  // 摄像机平滑延迟（秒，SmoothDamp 的 smoothTime）
}
