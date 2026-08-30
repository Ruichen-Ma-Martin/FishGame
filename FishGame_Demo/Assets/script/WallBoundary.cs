using UnityEngine;

// 挂在每面墙上：玩家游进 Trigger 后，从墙内表面起算水流带，回推力从 0 增到 pushStrength
// 只处理 tag "Player"；昆虫、水面都不管。数值来自 LevelConfig_SO
public class WallBoundary : MonoBehaviour
{
    [SerializeField] private LevelConfig_SO _levelConfig;   // 水流参数来源
    private Collider2D _col;                                // 本墙的碰撞体
    // 当前叠在这面墙上的玩家刚体：Stay 用来发现，LateUpdate 里再推一次
    // （玩家的 Movement 在 Update 里会改写速度，只在 Stay 里推会被盖掉）
    private Rigidbody2D _overlappingPlayerRb;

    // 缓存本墙碰撞体，后面算包围盒不用每帧再找
    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (_col == null)
        {
            Debug.LogError("WallBoundary 所在物体没有 Collider2D，水流推无法生效。", this);
        }

        if (_levelConfig == null)
        {
            Debug.LogError("WallBoundary 的 _levelConfig 没有赋值，请在 Inspector 里挂上 LevelConfig 资源。", this);
        }
    }

    // 玩家还在墙的 Trigger 里：记下刚体，真正的推力放在 LateUpdate
    private void OnTriggerStay2D(Collider2D other)
    {
        if (_levelConfig == null || _col == null)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        Rigidbody2D playerRb = other.attachedRigidbody != null
            ? other.attachedRigidbody
            : other.GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            return;
        }

        _overlappingPlayerRb = playerRb;
    }

    // 玩家离开这面墙时清掉引用，停止推
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _overlappingPlayerRb = null;
    }

    // 玩家 Movement 写完速度之后再推，水流才不会被盖掉
    private void LateUpdate()
    {
        if (_overlappingPlayerRb == null)
        {
            return;
        }

        ApplyWaterFlowPush(_overlappingPlayerRb);
    }

    // 水流软边界：刚进入带宽时不推，越往墙内力越大，顶满时回推速度达到 pushStrength
    private void ApplyWaterFlowPush(Rigidbody2D playerRb)
    {
        if (_levelConfig == null || _col == null || playerRb == null)
        {
            return;
        }

        float width = _levelConfig.softBoundaryWidth;
        if (width <= 0f)
        {
            return;
        }

        Bounds bounds = _col.bounds;
        Vector2 playerPos = playerRb.position;

        // 1) 判断墙是横墙还是竖墙（与昆虫边界判定一致：横向更长视为天花板/地面）
        bool isHorizontal = bounds.size.x >= bounds.size.y;

        // 2) 向内方向：从墙中心指向玩家，只取短轴
        //    竖墙只推水平，横墙只推垂直；玩家在关卡内侧，这个方向就是把鱼推回场内
        Vector2 inward;
        if (isHorizontal)
        {
            inward = new Vector2(0f, Mathf.Sign(playerPos.y - bounds.center.y));
        }
        else
        {
            inward = new Vector2(Mathf.Sign(playerPos.x - bounds.center.x), 0f);
        }

        // 3) 深入程度：从墙的内表面（玩家先碰到的那一面）往墙内算
        //    刚跨进内表面 depth=0，顶进 softBoundaryWidth 后 depth=width → t=1
        float depth;
        if (isHorizontal)
        {
            depth = inward.y > 0f
                ? bounds.max.y - playerPos.y   // 地板：玩家在上，往下顶进去
                : playerPos.y - bounds.min.y;  // 天花板：玩家在下，往上顶进去
        }
        else
        {
            depth = inward.x > 0f
                ? bounds.max.x - playerPos.x   // 左墙：玩家在右，往左顶进去
                : playerPos.x - bounds.min.x;  // 右墙：玩家在左，往右顶进去
        }

        float t = Mathf.Clamp01(depth / width);
        if (t <= 0f)
        {
            return;
        }

        // 4) 回推力从 0 线性增到 pushStrength，只改垂直于墙的速度
        //    不用瞬间清掉往里的速度，否则一进带就像撞上硬墙
        float flowSpeed = _levelConfig.pushStrength * t;
        Vector2 velocity = playerRb.linearVelocity;
        float alongInward = Vector2.Dot(velocity, inward);
        float newAlong = Mathf.Lerp(alongInward, flowSpeed, t);
        playerRb.linearVelocity = velocity + inward * (newAlong - alongInward);
    }
}
