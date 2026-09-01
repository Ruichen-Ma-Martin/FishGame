using UnityEngine;

// 挂在每面墙上：玩家靠近墙内侧（力场半径内）时被水流向外推，越近推力越大
// 只处理 tag "Player"；昆虫、水面都不管。数值来自 LevelConfig_SO
public class WallBoundary : MonoBehaviour
{
    [SerializeField] private LevelConfig_SO _levelConfig;   // 水流参数来源
    private Collider2D _col;                                // 本墙的碰撞体（墙内表面，不算力场）
    // 当前叠在这面墙上的玩家刚体：Stay 用来发现，LateUpdate 里再推一次
    // （玩家的 Movement 在 Update 里会改写速度，只在 Stay 里推会被盖掉）
    private Rigidbody2D _overlappingPlayerRb;

    // 缓存本墙碰撞体，并沿短轴伸出排斥力场 Trigger，靠近墙就会进 Stay
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

        CreateRepulsionFieldTrigger();
    }

    // 在墙短轴两侧各伸出力场半径：玩家靠近但还没贴墙时也会进 Trigger
    // 力场物体不带 wall 标签，摄像机和昆虫仍只用原墙碰撞体
    void CreateRepulsionFieldTrigger()
    {
        if (_col == null || _levelConfig == null)
        {
            return;
        }

        float radius = _levelConfig.forceRadius;
        if (radius <= 0f)
        {
            return;
        }

        BoxCollider2D wallBox = _col as BoxCollider2D;
        if (wallBox == null)
        {
            Debug.LogError("WallBoundary 需要 BoxCollider2D 才能生成力场 Trigger。", this);
            return;
        }

        GameObject fieldObj = new GameObject("RepulsionField");
        fieldObj.transform.SetParent(transform, false);
        fieldObj.transform.localPosition = Vector3.zero;
        fieldObj.transform.localRotation = Quaternion.identity;
        fieldObj.transform.localScale = Vector3.one;
        fieldObj.layer = gameObject.layer;

        BoxCollider2D fieldCol = fieldObj.AddComponent<BoxCollider2D>();
        fieldCol.isTrigger = true;
        fieldCol.offset = wallBox.offset;

        Bounds worldBounds = _col.bounds;
        bool isHorizontal = worldBounds.size.x >= worldBounds.size.y;
        Vector3 worldInflate = isHorizontal
            ? new Vector3(0f, radius, 0f)
            : new Vector3(radius, 0f, 0f);
        Vector3 localInflate = transform.InverseTransformVector(worldInflate);
        Vector2 extra = new Vector2(Mathf.Abs(localInflate.x), Mathf.Abs(localInflate.y)) * 2f;
        fieldCol.size = wallBox.size + extra;
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
    // 力场比墙本体大：只离开墙、还在力场里时不能清
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && cols[i].enabled && cols[i].IsTouching(other))
            {
                return;
            }
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

    // 排斥力场：玩家离墙内侧越近，向内的排斥力越大；距离超过力场半径就不再推
    void ApplyWaterFlowPush(Rigidbody2D playerRb)
    {
        if (_levelConfig == null || _col == null || playerRb == null)
        {
            return;
        }

        float radius = _levelConfig.forceRadius;
        if (radius <= 0f)
        {
            return;
        }

        Bounds bounds = _col.bounds;
        Vector2 playerPos = playerRb.position;

        // 1) 判断墙是横墙还是竖墙（与昆虫边界判定一致：横向更长视为天花板/地面）
        bool isHorizontal = bounds.size.x >= bounds.size.y;

        // 2) 向内方向：从墙中心指向玩家，只取短轴（竖墙只推水平，横墙只推垂直）
        Vector2 inward;
        if (isHorizontal)
        {
            inward = new Vector2(0f, Mathf.Sign(playerPos.y - bounds.center.y));
        }
        else
        {
            inward = new Vector2(Mathf.Sign(playerPos.x - bounds.center.x), 0f);
        }

        // 3) 玩家到墙内表面的距离（玩家在内侧时为正；贴墙为 0，越往里越大）
        float distance;
        if (isHorizontal)
        {
            // 地板（inward.y>0，玩家在上）：到墙顶面的距离；天花板（inward.y<0）：到墙底面的距离
            distance = inward.y > 0f
                ? playerPos.y - bounds.max.y
                : bounds.min.y - playerPos.y;
        }
        else
        {
            // 左墙（inward.x>0，玩家在右）：到墙右面的距离；右墙（inward.x<0）：到墙左面的距离
            distance = inward.x > 0f
                ? playerPos.x - bounds.max.x
                : bounds.min.x - playerPos.x;
        }

        // 4) 距离超过力场半径，或者玩家已经在墙外（distance<0），不推
        if (distance < 0f || distance >= radius)
        {
            return;
        }

        // 5) 排斥强度：越近越大。贴墙(distance=0)时 t=1，刚进入力场(distance=radius)时 t=0
        float t = 1f - distance / radius;

        // 6) 速度修正：把垂直于墙的速度分量，向"远离墙"方向推到 pushSpeed
        //    玩家朝墙游（分量小或为负）会被推回；已经在远离（分量够大）就不额外加力
        float pushSpeed = _levelConfig.forceStrength * t;
        Vector2 velocity = playerRb.linearVelocity;
        float alongInward = Vector2.Dot(velocity, inward);
        float newAlong = Mathf.Lerp(alongInward, pushSpeed, t);
        playerRb.linearVelocity = velocity + inward * (newAlong - alongInward);
    }
}
