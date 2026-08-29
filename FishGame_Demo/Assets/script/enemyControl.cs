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

    [Header("游荡")]
    [SerializeField] private float _minWanderSpeed = 1f;
    [SerializeField] private float _maxWanderSpeed = 3f;
    [SerializeField] private float _minChangeInterval = 1.5f;
    [SerializeField] private float _maxChangeInterval = 3f;
    [SerializeField] private float _minFloatingSpeed = 1f;
    [SerializeField] private float _maxFloatingSpeed = 3f;

    [Header("逃跑")]
    // 进入逃跑的距离
    [SerializeField] private float _fleeDistance = 4f;
    [SerializeField] private float _fleeSpeed = 5f;
    // 解除逃跑要再拉开这么多：进入看 _fleeDistance，离开看 _fleeDistance + 这个值
    [SerializeField] private float _fleeExitBuffer = 1.5f;
    // 水平距离小于这个值时沿用已锁定的逃跑方向，避免玩家从正下方靠近时每帧变号
    [SerializeField] private float _fleeDirectionDeadzone = 0.5f;

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

    private float FleeExitDistance => _fleeDistance + Mathf.Max(0f, _fleeExitBuffer);

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

    void TryFindPlayer()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    // 靠近 _fleeDistance 才开始逃；拉开到 FleeExitDistance 才停。中间维持原状态。
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
        else if (distance <= _fleeDistance)
        {
            _isFleeing = true;
            // 正下方死区里用当前游荡朝向垫底，避免默认往右
            _fleeDirection = _wanderDirection;
            _fleeDirection = ComputeFleeDirection();
        }
    }

    void RandomFloating()
    {
        _changeTimerFloating -= Time.deltaTime;
        if (_changeTimerFloating <= 0f)
        {
            RandomizeFloating();
        }

        _rb.linearVelocityY = _FloatingDirection * _FloatingSpeed;
    }

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

    void Flee()
    {
        _fleeDirection = ComputeFleeDirection();
        _wanderDirection = _fleeDirection;
        Move(_fleeDirection * _fleeSpeed);
    }

    float ComputeFleeDirection()
    {
        float awayFromPlayer = transform.position.x - _player.transform.position.x;
        float direction = Mathf.Abs(awayFromPlayer) < _fleeDirectionDeadzone
            ? _fleeDirection
            : Mathf.Sign(awayFromPlayer);

        return AvoidBlocked(direction);
    }

    void Move(float velocityX)
    {
        _rb.linearVelocityX = velocityX;
        FlipSprite(velocityX);

        if (_enemyAnim != null)
        {
            _enemyAnim.SetBool("isStop", false);
        }
    }

    void RandomizeWander()
    {
        _wanderSpeed = Random.Range(_minWanderSpeed, _maxWanderSpeed);
        _wanderDirection = Random.value < 0.5f ? -1f : 1f;
        _changeTimerWander = Random.Range(_minChangeInterval, _maxChangeInterval);
    }

    void RandomizeFloating()
    {
        _FloatingSpeed = Random.Range(_minFloatingSpeed, _maxFloatingSpeed);
        _FloatingDirection = Random.value < 0.5f ? -1f : 1f;
        _changeTimerFloating = Random.Range(_minChangeInterval, _maxChangeInterval);
    }

    void FlipSprite(float velocityX)
    {
        if (_enmeySprite == null || Mathf.Approximately(velocityX, 0f))
        {
            return;
        }

        _enmeySprite.flipX = velocityX < 0f;
    }

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
        if (other.CompareTag("wall") && !IsHorizontalBoundary(other))
        {
            _blockedDirection = 0f;
        }
    }

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

    static bool IsHorizontalBoundary(Collider2D other)
    {
        Bounds bounds = other.bounds;
        return bounds.size.x >= bounds.size.y;
    }

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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _fleeDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, FleeExitDistance);
    }
}
