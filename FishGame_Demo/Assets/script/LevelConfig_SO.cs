using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 关卡配置：水流边界和摄像机跟随，只放不会在运行时变化的常量
// 墙（tag "wall"）的位置本身不放进来，由脚本在 Start 里从场景物体计算
[CreateAssetMenu(fileName = "LevelConfig", menuName = "ScriptableObjects/LevelConfig", order = 6)]
public class LevelConfig_SO : ScriptableObject
{
    [Header("水流边界")]
    public float softBoundaryWidth = 1f;   // 水流带宽度：从墙向内延伸多宽算"水流带"
    public float pushStrength = 8f;        // 水流推力强度（越大越推不动）

    [Header("摄像机")]
    public float cameraSmoothTime = 0.2f;  // 摄像机平滑延迟（秒，SmoothDamp 的 smoothTime）
}
