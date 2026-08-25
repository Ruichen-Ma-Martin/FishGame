using UnityEngine;

// 挂在场景里名为 "WaterSurface" 的空物体上，只负责对外提供水面高度（世界坐标 Y）。
// 把这个物体上下移动就能整体调整水位，不需要改代码。
public class WaterSurface : MonoBehaviour
{
    // 当前场景的水面。子弹是运行时 Instantiate 出来的预制体，无法在 Inspector 里引用场景物体，
    // 所以这里用一个静态入口提供水面高度；这个类只存高度、不放任何游戏逻辑
    private static WaterSurface _current;
    private static bool _hasWarned;

    // 编辑器里水面参考线的绘制长度，只影响 Gizmo 显示
    [SerializeField] private float _gizmoLineWidth = 50f;

    // 水面高度：物体的 Y 大于它算水上，小于它算水下
    public static float LineY
    {
        get
        {
            if (_current != null)
            {
                return _current.transform.position.y;
            }

            // 场景里忘记放 WaterSurface 时按 Y = 0 处理，并且只提示一次，避免刷满 Console
            if (!_hasWarned)
            {
                Debug.LogWarning("场景中没有 WaterSurface，水面高度暂按 Y = 0 处理。");
                _hasWarned = true;
            }
            return 0f;
        }
    }

    // 判断某个世界坐标是否在水面之上
    public static bool IsAboveWater(Vector3 worldPosition)
    {
        return worldPosition.y > LineY;
    }

    private void Awake()
    {
        // 场景里放了多个水面时以最后一个为准，并给出提示，避免静默用错高度
        if (_current != null && _current != this)
        {
            Debug.LogWarning("场景中存在多个 WaterSurface，实际使用的是：" + name);
        }
        _current = this;
        _hasWarned = false;
    }

    private void OnDestroy()
    {
        // 只清理自己注册的引用，避免切场景时把新水面的引用误清掉
        if (_current == this)
        {
            _current = null;
        }
    }

    private void OnDrawGizmos()
    {
        // 在编辑器里画出水面线，方便对着场景摆水位（Gizmos 属于 UnityEngine，不影响打包）
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        Gizmos.DrawLine(center + Vector3.left * _gizmoLineWidth, center + Vector3.right * _gizmoLineWidth);
    }
}
