using UnityEngine;

// 水面昆虫的行动逻辑。水平和垂直两个轴各管各的：
//   垂直（RandomFloating）：随机上下漂浮，下界是水面、上界是天花板，任何状态下都在跑。
//   水平：逃跑（玩家靠近时远离）和游荡（随机来回）二选一。
// 活动边界靠场景里 wall / WaterSurface 这些 trigger，统一在 HandleBoundary 里处理。
// 墙是 trigger 而非实体，所以只影响昆虫，玩家能自由穿过。
// 本脚本不含任何攻击行为；血量和死亡由 enemy.cs 负责。
public class enemyControl : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Rigidbody2D _rb;
    // 昆虫由 SpawnEnemy 在运行时生成，预制体无法在 Inspector 里引用场景中的玩家，
    // 所以留空时按 Tag 查找一次
    [SerializeField] private GameObject _player;

    public SpriteRenderer _enmeySprite;
    public Animator _enemyAnim;

    // 数值配置表：游荡、漂浮、逃跑参数都从这里读，需在 Inspector 里挂上 EnemyStats 资源
    [SerializeField] private EnemyStats_SO _stats;

    private float _wanderDirection = 1f;
    private float _wanderSpeed;
    private float _changeTimerWander;
    private float _changeTimerFloating;
    private float _FloatingSpeed;
    private float _FloatingDirection = 1f;

    private bool _isFleeing;
    private float _fleeDirection = 1f;
    // 当前被竖墙堵住的水平方向：+1 右边是墙，-1 左边是墙，0 没被堵
    private float _blockedDirection;

    // 解除逃跑的距离 = 进入距离 + 缓冲，缓冲小于 0 时按 0 算
    private float FleeExitDistance => _stats.fleeDistance + Mathf.Max(0f, _stats.fleeExitBuffer);

    // 初始化刚体并尝试找到玩家
    private void Awake()
    {
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        _rb.gravityScale = 0f;
        TryFindPlayer();
    }

    private void Start()
    {
        // Awake 时玩家可能还没进场，Start 再找一次
        TryFindPlayer();
        RandomizeWander();
        RandomizeFloating();
    }

    // 每帧物理更新：先判定逃跑，再水平移动，垂直漂浮始终运行
    private void FixedUpdate()
    {
        UpdateFleeState();

        if (_isFleeing)
        {
            Flee();
        }
        else
        {
            Wander();
        }

        RandomFloating();
    }

    // 预制体无法引用场景玩家，运行时按 Tag 查找
    void TryFindPlayer()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    // 靠近 fleeDistance 才开始逃；拉开到 FleeExitDistance 才停。中间维持原状态。
    void UpdateFleeState()
    {
        if (_player == null)
        {
            TryFindPlayer();
        }

        if (_player == null)
        {
            _isFleeing = false;
            return;
        }

        float distance = Vector2.Distance(transform.position, _player.transform.position);

        if (_isFleeing)
        {
            if (distance >= FleeExitDistance)
            {
                _isFleeing = false;
            }
        }
        else if (distance <= _stats.fleeDistance)
        {
            _isFleeing = true;
            // 正下方死区里用当前游荡朝向垫底，避免默认往右
            _fleeDirection = _wanderDirection;
            _fleeDirection = ComputeFleeDirection();
        }
    }

    // 随机上下漂浮，到时重新抽速度和方向
    void RandomFloating()
    {
        _changeTimerFloating -= Time.deltaTime;
        if (_changeTimerFloating <= 0f)
        {
            RandomizeFloating();
        }

        _rb.linearVelocityY = _FloatingDirection * _FloatingSpeed;
    }

    // 水平游荡：到时重新随机速度和方向，避开被堵住的一侧
    void Wander()
    {
        _changeTimerWander -= Time.deltaTime;
        if (_changeTimerWander <= 0f)
        {
            RandomizeWander();
        }

        _wanderDirection = AvoidBlocked(_wanderDirection);
        Move(_wanderDirection * _wanderSpeed);
    }

    // 水平逃跑：远离玩家，速度来自配置表
    void Flee()
    {
        _fleeDirection = ComputeFleeDirection();
        _wanderDirection = _fleeDirection;
        Move(_fleeDirection * _stats.fleeSpeed);
    }

    // 根据玩家相对位置决定逃跑方向，死区内沿用上次方向
    float ComputeFleeDirection()
    {
        float awayFromPlayer = transform.position.x - _player.transform.position.x;
        float direction = Mathf.Abs(awayFromPlayer) < _stats.fleeDirectionDeadzone
            ? _fleeDirection
            : Mathf.Sign(awayFromPlayer);

        return AvoidBlocked(direction);
    }

    // 设置水平速度并翻转贴图
    void Move(float velocityX)
    {
        _rb.linearVelocityX = velocityX;
        FlipSprite(velocityX);

        if (_enemyAnim != null)
        {
            _enemyAnim.SetBool("isStop", false);
        }
    }

    // 重新随机游荡速度、方向和切换间隔
    void RandomizeWander()
    {
        _wanderSpeed = Random.Range(_stats.minWanderSpeed, _stats.maxWanderSpeed);
        _wanderDirection = Random.value < 0.5f ? -1f : 1f;
        _changeTimerWander = Random.Range(_stats.minChangeInterval, _stats.maxChangeInterval);
    }

    // 重新随机垂直漂浮速度、方向和切换间隔
    void RandomizeFloating()
    {
        _FloatingSpeed = Random.Range(_stats.minFloatingSpeed, _stats.maxFloatingSpeed);
        _FloatingDirection = Random.value < 0.5f ? -1f : 1f;
        _changeTimerFloating = Random.Range(_stats.minChangeInterval, _stats.maxChangeInterval);
    }

    // 根据水平速度方向翻转精灵
    void FlipSprite(float velocityX)
    {
        if (_enmeySprite == null || Mathf.Approximately(velocityX, 0f))
        {
            return;
        }

        _enmeySprite.flipX = velocityX < 0f;
    }

    // 撞到其他昆虫时互相弹开
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("enemy"))
        {
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

    // 碰到边界时立刻处理
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleBoundary(other);
    }

    // 持续贴着边界时每帧纠正方向
    void OnTriggerStay2D(Collider2D other)
    {
        HandleBoundary(other);
    }

    // 离开竖墙时解除水平阻挡
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("wall") && !IsHorizontalBoundary(other))
        {
            _blockedDirection = 0f;
        }
    }

    // 水面把昆虫往上推；墙按横竖分别限制垂直或水平
    void HandleBoundary(Collider2D other)
    {
        if (other.CompareTag("WaterSurface"))
        {
            _FloatingDirection = 1f;
            return;
        }

        if (!other.CompareTag("wall"))
        {
            return;
        }

        if (IsHorizontalBoundary(other))
        {
            _FloatingDirection = transform.position.y >= other.bounds.center.y ? 1f : -1f;
            return;
        }

        _blockedDirection = transform.position.x >= other.bounds.center.x ? -1f : 1f;
        _wanderDirection = -_blockedDirection;
        _fleeDirection = -_blockedDirection;
    }

    // 横向更长的碰撞体视为天花板/地面
    static bool IsHorizontalBoundary(Collider2D other)
    {
        Bounds bounds = other.bounds;
        return bounds.size.x >= bounds.size.y;
    }

    // 如果当前方向正被墙堵住，改走反方向
    float AvoidBlocked(float direction)
    {
        if (_blockedDirection != 0f && Mathf.Approximately(direction, _blockedDirection))
        {
            return -_blockedDirection;
        }

        return direction;
    }

    // 选中时画出进入逃跑和解除逃跑的范围
    private void OnDrawGizmosSelected()
    {
        if (_stats == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _stats.fleeDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, FleeExitDistance);
    }
}
