using UnityEngine;

// 水面昆虫的行动逻辑：只在水平方向活动，平时随机游荡，玩家靠近时朝反方向逃跑。
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
    // 以出生点为中心的活动范围，只用到 X 方向的宽度
    [SerializeField] private Vector2 _areaSize = new Vector2(5f, 5f);
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
    private float _changeTimer;

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
        return Vector2.Distance(transform.position, _player.transform.position) <= _fleeDistance;
    }

    // 游荡：以随机速度朝随机方向水平移动，到达活动范围边界折返，隔一段时间重新随机
    void Wander()
    {
        _changeTimer -= Time.deltaTime;
        if (_changeTimer <= 0f)
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

    // 统一的水平移动出口：垂直速度恒为 0，保证昆虫始终停在水面高度
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

    // 撞到墙或其他昆虫就折返，和活动范围一起构成边界处理
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall") || collision.gameObject.CompareTag("enemy"))
        {
            _wanderDirection *= -1f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 在编辑器里画出游荡范围和逃跑警戒圈，方便调参数
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? (Vector3)_areaCenter : transform.position;
        Gizmos.DrawLine(center + Vector3.left * _areaSize.x / 2f, center + Vector3.right * _areaSize.x / 2f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _fleeDistance);
    }
}
