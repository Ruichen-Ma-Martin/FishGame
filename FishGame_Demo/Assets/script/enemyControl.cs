using UnityEngine;

// 水面昆虫的行动逻辑。水平和垂直两个轴各管各的：
//   垂直（RandomFloating）：在活动范围高度内上下漂浮，任何状态下都在跑。
//   水平：三个状态互斥——逃跑（不受活动范围约束）、回家（被赶出去后往中心游）、游荡（范围内随机来回）。
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

    [Header("随机值")] 
    // 游荡速度的随机区间
    [SerializeField] private float _minWanderSpeed = 1f;
    [SerializeField] private float _maxWanderSpeed = 3f;
    // 以出生点为中心的活动范围：X 是左右游荡的宽度，Y 是上下漂浮的高度
    [SerializeField] private Vector2 _areaSize = new Vector2(5f, 5f);
    // 重新随机速度和方向的间隔区间，让昆虫的移动看起来没有规律
    [SerializeField] private float _minChangeInterval = 1.5f;
    [SerializeField] private float _maxChangeInterval = 3f;
    [SerializeField] private float _minFloatingSpeed = 1f;
    [SerializeField] private float _maxFloatingSpeed = 3f;
    

    [Header("逃跑")]
    // 玩家进入这个距离就开始逃跑
    [SerializeField] private float _fleeDistance = 4f;
    [SerializeField] private float _fleeSpeed = 5f;
    // 解除逃跑的额外距离：进入逃跑看 _fleeDistance，解除逃跑看 _fleeDistance + 这个值。
    // 两个阈值之间是缓冲带，维持当前状态不变，玩家正好停在阈值上时就不会每帧在逃跑/游荡之间翻转
    [SerializeField] private float _fleeExitBuffer = 1.5f;
    // 水平距离小于这个值时 Mathf.Sign 的结果不可信：玩家从正下方靠近时 Δx 会在 0 附近来回抖，
    // 每帧算出的逃跑方向就会变号。此时沿用已锁定的方向，让昆虫坚持往同一边跑
    [SerializeField] private float _fleeDirectionDeadzone = 0.5f;
    // 被赶出活动范围后，玩家比这个距离还远才敢往家的方向游。
    // 必须大于"解除逃跑距离"，否则昆虫会为了回家主动撞进逃跑范围，然后被推出来、再回头，形成来回拉锯
    [SerializeField] private float _returnSafeDistance = 7f;

    // 出生位置，作为游荡范围的中心
    private Vector2 _areaCenter;
    // 当前水平方向：1 向右，-1 向左
    private float _wanderDirection = 1f;
    private float _wanderSpeed;
    // 距离下一次重新随机的剩余时间
    private float _changeTimerWander;
    private float _changeTimerFloating;
    private float _FloatingSpeed;
    private float _FloatingDirection = 1f;

    // 是否处于逃跑状态。用状态位而不是每帧现算距离，才能实现进出两个阈值的回差
    private bool _isFleeing;
    // 本次逃跑锁定的方向：进入逃跑时定下来，之后只在玩家明确偏向某一侧时才更新
    private float _fleeDirection = 1f;

    // 回家阈值必须严格大于解除逃跑的阈值，否则"刚停止逃跑"和"可以回家了"在同一距离上成立，
    // 昆虫会立刻掉头撞回逃跑范围，回差就白加了。这里兜底修正，免得 Inspector 上随手调参调出拉锯
    private float EffectiveReturnDistance =>
        Mathf.Max(_returnSafeDistance, _fleeDistance + _fleeExitBuffer + 0.5f);

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
        RandomizeFloating();
    }

    // 速度写在 FixedUpdate 而不是 Update：物理步和渲染帧不同步，写在 Update 里时
    // 两个物理步之间的多次写入只有最后一次生效，方向稍有变化就会抖得毫无规律。
    // 注意 FixedUpdate 内的 Time.deltaTime 返回的就是固定步长，计时器照用即可
    private void FixedUpdate()
    {
        UpdateFleeState();

        if (_isFleeing)
        {
            // 逃跑不受活动范围约束：被吓到就一路冲出去，玩家走远后再自己游回来
            Flee();
        }
        else if (IsOutsideArea())
        {
            ReturnToArea();
        }
        else
        {
            Wander();
        }

        // 垂直轴和水平轴互不干扰，所以放在 if/else 外面每帧都跑。
        // 放进 else 里的话逃跑期间没人管 Y：昆虫零重力又没阻尼，会带着最后一帧的垂直速度一直飘出活动范围
        RandomFloating();
    }

    // 用进出两个阈值维护逃跑状态：靠近到 _fleeDistance 才开始逃，拉开到 _fleeDistance + _fleeExitBuffer 才解除。
    // 只用一个阈值的话，玩家悬在正好那个距离上时状态会每帧翻转，这是原来"原地抽动"的直接原因之一
    void UpdateFleeState()
    {
        if (_player == null)
        {
            _isFleeing = false;
            return;
        }

        // 玩家在水下、昆虫在水面，所以用二维距离，从下方靠近也算
        float distance = Vector2.Distance(transform.position, _player.transform.position);

        if (_isFleeing)
        {
            if (distance >= _fleeDistance + _fleeExitBuffer)
            {
                _isFleeing = false;
            }
        }
        else if (distance <= _fleeDistance)
        {
            _isFleeing = true;
            // 进入逃跑的这一刻把方向定下来，后续帧尽量沿用，避免玩家在正下方微动就来回变号
            _fleeDirection = ComputeFleeDirection();
        }
    }

    bool IsPlayerWithin(float distance)
    {
        if (_player == null)
        {
            return false;
        }
        return Vector2.Distance(transform.position, _player.transform.position) <= distance;
    }

    bool IsOutsideArea()
    {
        return Mathf.Abs(transform.position.x - _areaCenter.x) > _areaSize.x / 2f;
    }
    void RandomFloating()
    {
        _changeTimerFloating -= Time.deltaTime;
        if (_changeTimerFloating <= 0f)
        {
            RandomizeFloating();
        }
        if (transform.position.y >= _areaCenter.y + _areaSize.y / 2f)
        {
            _FloatingDirection = -1f;
        }
        else if (transform.position.y <= _areaCenter.y - _areaSize.y / 2f)
        {
            _FloatingDirection = 1f;
        }
        // linearVelocity 是属性，取出来的 Vector2 是副本，写 .y 改不到 Rigidbody 本体（编译期就会报 CS1612）。
        // Unity 6 提供了单分量属性，直接写 Y 不会动到 X
        _rb.linearVelocityY = _FloatingDirection * _FloatingSpeed;
    }

    // 游荡：以随机速度朝随机方向水平移动，到达活动范围边界折返，隔一段时间重新随机
    void Wander()
    {
        _changeTimerWander -= Time.deltaTime;
        if (_changeTimerWander <= 0f)
        {
            RandomizeWander();
        }

        // 边界折返放在随机之后，保证刚随机出的方向如果朝外会被立刻纠正回来
        if (transform.position.x >= _areaCenter.x + _areaSize.x / 2f)
        {
            _wanderDirection = -1f;
        }
        else if (transform.position.x <= _areaCenter.x - _areaSize.x / 2f)
        {
            _wanderDirection = 1f;
        }

        Move(_wanderDirection * _wanderSpeed);
    }

    // 逃跑：沿"玩家 → 自己"的水平方向远离玩家，不理活动范围，能冲多远冲多远
    void Flee()
    {
        _fleeDirection = ComputeFleeDirection();

        // 记下逃跑方向，玩家走远后就从这个方向继续游荡，不会突然回头撞上玩家
        _wanderDirection = _fleeDirection;
        Move(_fleeDirection * _fleeSpeed);
    }

    // 逃跑方向 = 玩家指向自己的水平方向。死区内维持上一次的结果，不重新算
    float ComputeFleeDirection()
    {
        float awayFromPlayer = transform.position.x - _player.transform.position.x;
        if (Mathf.Abs(awayFromPlayer) < _fleeDirectionDeadzone)
        {
            return _fleeDirection;
        }
        return Mathf.Sign(awayFromPlayer);
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
    void Move(float velocityX)
    {
        _rb.linearVelocityX = velocityX;
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
        _changeTimerWander = Random.Range(_minChangeInterval, _maxChangeInterval);
    }
    void RandomizeFloating()
    {
        _FloatingSpeed = Random.Range(_minFloatingSpeed, _maxFloatingSpeed);
        _FloatingDirection = Random.value < 0.5f ? -1f : 1f;
        _changeTimerFloating = Random.Range(_minChangeInterval, _maxChangeInterval);
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

    // 撞到墙或其他昆虫就折返，和活动范围一起构成边界处理
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall") || collision.gameObject.CompareTag("enemy"))
        {
            _wanderDirection *= -1f;
            _FloatingDirection *= -1f;
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("WaterSurface"))
        {
            _FloatingDirection *= -1f;
            _wanderDirection *= -1f;
            
        }
    }   

    private void OnDrawGizmosSelected()
    {
        // 在编辑器里画出活动范围和三个警戒圈，方便调参数。
        // 三个半径的大小关系必须是 逃跑 < 解除逃跑 < 回家，画出来才好确认没调反
        Vector3 center = Application.isPlaying ? (Vector3)_areaCenter : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(_areaSize.x, _areaSize.y, 0f));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _fleeDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _fleeDistance + _fleeExitBuffer);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, EffectiveReturnDistance);
    }
}
