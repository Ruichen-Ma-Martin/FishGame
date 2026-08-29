using UnityEngine;

<<<<<<< Updated upstream
// 水面昆虫的行动逻辑：只在水平方向活动，平时随机游荡，玩家靠近时朝反方向逃跑。
=======
// 水面昆虫的行动逻辑。水平和垂直两个轴各管各的：
//   垂直（RandomFloating）：随机上下漂浮，下界是水面、上界是天花板，任何状态下都在跑。
//   水平：逃跑（玩家靠近时远离）和游荡（随机来回）二选一。
// 活动边界不再由脚本里的范围参数决定，而是靠场景里 wall / WaterSurface 这些 trigger 兜着，
// 统一在 HandleBoundary 里处理。墙是 trigger 而非实体，所以只影响昆虫，玩家能自由穿过。
>>>>>>> Stashed changes
// 本脚本不含任何攻击行为；血量和死亡由 enemy.cs 负责。
public class enemyControl : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Rigidbody2D _rb;
    // 昆虫由 SpawnEnemy 在运行时生成，预制体无法在 Inspector 里引用场景中的玩家，
    // 所以留空时在 Awake 里按 Tag 查找一次（只在出生时找一次，不是每帧查找）
    [SerializeField] private GameObject _player;

    public SpriteRenderer _enmeySprite;
    public Animator _enemyAnim;

    [Header("游荡")]
    // 游荡速度的随机区间
    [SerializeField] private float _minWanderSpeed = 1f;
    [SerializeField] private float _maxWanderSpeed = 3f;
<<<<<<< Updated upstream
    // 以出生点为中心的活动范围，只用到 X 方向的宽度
    [SerializeField] private Vector2 _areaSize = new Vector2(5f, 5f);
=======
    // 以出生点为中心的活动范围：X 是左右游荡的宽度，Y 是上下漂浮的高度
   // [SerializeField] private Vector2 _areaSize = new Vector2(5f, 5f);
>>>>>>> Stashed changes
    // 重新随机速度和方向的间隔区间，让昆虫的移动看起来没有规律
    [SerializeField] private float _minChangeInterval = 1.5f;
    [SerializeField] private float _maxChangeInterval = 3f;

    [Header("逃跑")]
    // 玩家进入这个距离就开始逃跑
    [SerializeField] private float _fleeDistance = 4f;
    [SerializeField] private float _fleeSpeed = 5f;

    // 出生位置，作为游荡范围的中心
    private Vector2 _areaCenter;
    // 当前水平方向：1 向右，-1 向左
    private float _wanderDirection = 1f;
    private float _wanderSpeed;
    // 距离下一次重新随机的剩余时间
<<<<<<< Updated upstream
    private float _changeTimer;
=======
    private float _changeTimerWander;
    private float _changeTimerFloating;
    private float _FloatingSpeed;
    private float _FloatingDirection = 1f;

    // 是否处于逃跑状态。用状态位而不是每帧现算距离，才能实现进出两个阈值的回差
    private bool _isFleeing;
    // 本次逃跑锁定的方向：进入逃跑时定下来，之后只在玩家明确偏向某一侧时才更新
    private float _fleeDirection = 1f;

    // 当前被竖墙堵住的水平方向：+1 表示右边是墙，-1 表示左边是墙，0 表示没被堵。
    // 之所以要单独存这个状态，而不是在碰撞回调里直接改 _fleeDirection：
    // Flee 每个物理步都按玩家位置重算方向，回调里改的值下一步就被覆盖了，折返根本留不住
    private float _blockedDirection;

    // 回家阈值必须严格大于解除逃跑的阈值，否则"刚停止逃跑"和"可以回家了"在同一距离上成立，
    // 昆虫会立刻掉头撞回逃跑范围，回差就白加了。这里兜底修正，免得 Inspector 上随手调参调出拉锯
    private float EffectiveReturnDistance =>
        Mathf.Max(_returnSafeDistance, _fleeDistance + _fleeExitBuffer + 0.5f);
>>>>>>> Stashed changes

    private void Awake()
    {
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        // 水面昆虫贴着水面漂，不受重力影响
        _rb.gravityScale = 0f;

        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void Start()
    {
        // 出生点就是游荡范围的中心
        _areaCenter = transform.position;
        RandomizeWander();
    }

    private void Update()
    {
        // 逃跑优先级高于游荡：玩家一靠近就中断游荡
        if (IsPlayerTooClose())
        {
            Flee();
        }
<<<<<<< Updated upstream
=======
        //else if (IsOutsideArea())
        //{
            //ReturnToArea();
        //}
>>>>>>> Stashed changes
        else
        {
            Wander();
        }
    }

    // 判断玩家是否进入警戒距离（玩家在水下，所以用二维距离，从下方靠近也算）
    bool IsPlayerTooClose()
    {
        if (_player == null)
        {
            return false;
        }
<<<<<<< Updated upstream
        return Vector2.Distance(transform.position, _player.transform.position) <= _fleeDistance;
=======
        return Vector2.Distance(transform.position, _player.transform.position) <= distance;
    }

    bool IsOutsideArea()
    {
        //return Mathf.Abs(transform.position.x - _areaCenter.x) > _areaSize.x / 2f;
        return false;
    }
    void RandomFloating()
    {
        _changeTimerFloating -= Time.deltaTime;
        if (_changeTimerFloating <= 0f)
        {
            RandomizeFloating();
        }
        
        // linearVelocity 是属性，取出来的 Vector2 是副本，写 .y 改不到 Rigidbody 本体（编译期就会报 CS1612）。
        // Unity 6 提供了单分量属性，直接写 Y 不会动到 X
        _rb.linearVelocityY = _FloatingDirection * _FloatingSpeed;
>>>>>>> Stashed changes
    }

    // 游荡：以随机速度朝随机方向水平移动，碰到墙折返，隔一段时间重新随机
    void Wander()
    {
        _changeTimer -= Time.deltaTime;
        if (_changeTimer <= 0f)
        {
            RandomizeWander();
        }

        // 绕墙放在随机之后，保证刚随机出的方向如果朝着墙会被立刻纠正回来
        _wanderDirection = AvoidBlocked(_wanderDirection);

        Move(_wanderDirection * _wanderSpeed);
    }

    // 逃跑：沿"玩家 → 自己"的水平方向远离玩家
    void Flee()
    {
        float awayFromPlayer = transform.position.x - _player.transform.position.x;
        // 正好和玩家在同一条竖线上时沿用原方向，避免 Sign(0) 返回 0 让昆虫卡住不动
        float direction = Mathf.Approximately(awayFromPlayer, 0f)
            ? _wanderDirection
            : Mathf.Sign(awayFromPlayer);

        // 记下逃跑方向，玩家走远后就从这个方向继续游荡，不会突然回头撞上玩家
        _wanderDirection = direction;
        Move(direction * _fleeSpeed);
    }

<<<<<<< Updated upstream
    // 统一的水平移动出口：垂直速度恒为 0，保证昆虫始终停在水面高度
=======
    // 逃跑方向 = 玩家指向自己的水平方向。死区内维持上一次的结果，不重新算
    float ComputeFleeDirection()
    {
        float awayFromPlayer = transform.position.x - _player.transform.position.x;
        float direction = Mathf.Abs(awayFromPlayer) < _fleeDirectionDeadzone
            ? _fleeDirection
            : Mathf.Sign(awayFromPlayer);

        // 被逼到墙角时宁可从玩家的另一侧窜过去，也不要顶着墙原地推
        return AvoidBlocked(direction);
    }

    // 回家：被逃跑推出活动范围后，往中心方向游回去。
    // 关键是玩家还在附近时绝不回头——原来 Wander 的边界折返不管玩家在哪就硬把昆虫往范围内拉，
    // 而范围内正是玩家所在的方向，于是"折返"和"逃跑"每帧互相抵消，看起来就是原地抽动
    void ReturnToArea()
    {
        if (IsPlayerWithin(EffectiveReturnDistance))
        {
            // 危险还没解除，就在范围外待着，只保留上下漂浮
            Halt();
            return;
        }

        float towardCenter = Mathf.Sign(_areaCenter.x - transform.position.x);
        _wanderDirection = towardCenter;
        Move(towardCenter * _wanderSpeed);
    }

    // 水平方向停住，垂直漂浮不受影响。贴图保持上一次的朝向，不做翻转
    void Halt()
    {
        _rb.linearVelocityX = 0f;

        if (_enemyAnim != null)
        {
            _enemyAnim.SetBool("isStop", true);
        }
    }

    // 统一的水平移动出口：只写 X，Y 由 RandomFloating 负责，两个轴互不覆盖
>>>>>>> Stashed changes
    void Move(float velocityX)
    {
        _rb.linearVelocity = new Vector2(velocityX, 0f);
        FlipSprite(velocityX);

        if (_enemyAnim != null)
        {
            _enemyAnim.SetBool("isStop", false);
        }
    }

    // 重新随机游荡的速度和方向
    void RandomizeWander()
    {
        _wanderSpeed = Random.Range(_minWanderSpeed, _maxWanderSpeed);
        _wanderDirection = Random.value < 0.5f ? -1f : 1f;
        _changeTimer = Random.Range(_minChangeInterval, _maxChangeInterval);
    }

    // 按移动方向翻转贴图，沿用原素材"向左走时 flipX = true"的约定
    void FlipSprite(float velocityX)
    {
        if (_enmeySprite == null || Mathf.Approximately(velocityX, 0f))
        {
            return;
        }
        _enmeySprite.flipX = velocityX < 0f;
    }

    // 撞到别的昆虫：按相对位置往反方向让开。
    // 这里也用赋值而不是 *= -1——两只虫子同时取反会同步翻转，可能反而卡在一起
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("enemy"))
        {
<<<<<<< Updated upstream
            _wanderDirection *= -1f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 在编辑器里画出游荡范围和逃跑警戒圈，方便调参数
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? (Vector3)_areaCenter : transform.position;
        Gizmos.DrawLine(center + Vector3.left * _areaSize.x / 2f, center + Vector3.right * _areaSize.x / 2f);

=======
            return;
        }

        Vector2 away = (Vector2)transform.position - (Vector2)collision.transform.position;

        if (!Mathf.Approximately(away.x, 0f))
        {
            _wanderDirection = Mathf.Sign(away.x);
        }
        if (!Mathf.Approximately(away.y, 0f))
        {
            _FloatingDirection = Mathf.Sign(away.y);
        }
    }

    // 墙和水面都是 trigger（墙不是实体，所以玩家可以直接穿过去，不受影响），
    // Enter 和 Stay 都接同一个处理函数：Enter 只在进入那一帧触发一次，
    // 虫子还在 trigger 里面时若恰好重新摇到朝边界的方向就会漏出去，Stay 能把这个洞补上
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleBoundary(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        HandleBoundary(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // 只有离开竖墙才解除水平封锁；水面的进出不该影响水平方向
        if (other.CompareTag("wall") && !IsHorizontalBoundary(other))
        {
            _blockedDirection = 0f;
        }
    }

    // 边界处理的统一入口。两个要点：
    //   1) 一律用"赋值"而不是 *= -1。取反不知道边界在自己的哪一侧，虫子一旦漏到边界另一边
    //      就会被越推越远（原来漏到水面以下就永久沉底，就是这个原因）。赋值是幂等的，
    //      重复执行或漏掉事件都不会出错，所以 Enter 和 Stay 才能共用同一个函数。
    //   2) 按碰撞体的世界包围盒形状判断这是横边界还是竖边界，只动对应的那一个轴。
    //      天花板和左右墙共用 wall 标签，光看 tag 是分不出该翻哪个轴的。
    void HandleBoundary(Collider2D other)
    {
        if (other.CompareTag("WaterSurface"))
        {
            // 水面是硬性下界：永远往上推，不看虫子当前在哪一侧。
            // 这样即使某一帧漏到了水下，下次接触也能被捞回来，不会变成单向陷阱
            _FloatingDirection = 1f;
            return;
        }

        if (!other.CompareTag("wall"))
        {
            return;
        }

        if (IsHorizontalBoundary(other))
        {
            // 横边界（天花板）：只改垂直方向，往远离它的那一侧推
            _FloatingDirection = transform.position.y >= other.bounds.center.y ? 1f : -1f;
            return;
        }

        // 竖墙：只改水平方向。记下被堵住的是哪一侧，游荡和逃跑都要绕开它
        _blockedDirection = transform.position.x >= other.bounds.center.x ? -1f : 1f;
        _wanderDirection = -_blockedDirection;
        _fleeDirection = -_blockedDirection;
    }

    // 用世界包围盒的长宽比判断边界朝向。bounds 已经把旋转算进去了，
    // 所以场景里旋转了 90° 的竖墙在这里会正确地表现为"高大于宽"
    static bool IsHorizontalBoundary(Collider2D other)
    {
        Bounds bounds = other.bounds;
        return bounds.size.x >= bounds.size.y;
    }

    // 把朝向墙的方向掰回来。游荡和逃跑都要过这一道，
    // 否则 Flee 按玩家位置算出的方向会直接把虫子顶到墙里去
    float AvoidBlocked(float direction)
    {
        if (_blockedDirection != 0f && Mathf.Approximately(direction, _blockedDirection))
        {
            return -_blockedDirection;
        }
        return direction;
    }

    private void OnDrawGizmosSelected()
    {
        // 在编辑器里画出三个警戒圈，方便调参数。
        // 三个半径的大小关系必须是 逃跑 < 解除逃跑 < 回家，画出来才好确认没调反
>>>>>>> Stashed changes
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _fleeDistance);
    }
}
