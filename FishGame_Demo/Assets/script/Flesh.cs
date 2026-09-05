using System;
using UnityEngine;

// 昆虫死亡后掉落的肉块：先移动到水面高度，然后浮在水面上随水流缓慢漂移，
// 玩家碰到就消失并广播拾取事件（加金币由 player.cs 负责）。
// 位置必须通过 Rigidbody2D 驱动，原因见 FixedUpdate 里的注释
[RequireComponent(typeof(Rigidbody2D))]
public class Flesh : MonoBehaviour
{
    // 被拾取时广播。用静态事件而不是自己去找玩家对象，
    // 和 enemy.OnEnemyDeath、enemyattack.OnPlayerHit 保持同一套写法
    public static Action<Flesh> OnCollected;

    [Header("浮到水面")]
    // 从掉落位置移动到水面的速度（在水下就上浮，在空中就下落）
    [SerializeField] private float _settleSpeed = 3f;

    [Header("水面漂浮")]
    // 浮在水面后的水平漂移速度
    [SerializeField] private float _driftSpeed = 0.5f;
    // 上下起伏的幅度和频率，只影响观感
    [SerializeField] private float _bobAmplitude = 0.1f;
    [SerializeField] private float _bobFrequency = 1.5f;

    [Header("清理")]
    // 超过这个时间没被捡走就自动消失；填 0 表示永不消失
    [SerializeField] private float _lifeTime = 0f;

    // 是否已经到达水面，到达前只做垂直靠近，到达后才开始起伏
    private bool _isFloating;
    // 漂移方向：1 向右，-1 向左，出生时随机决定
    private float _driftDirection = 1f;
    private float _bobTimer;

    private Rigidbody2D _rb;

    // 预制体缺组件时每块肉都会报一次，用静态标记压成一次
    private static bool _hasReportedMissingCollider;

    private void Awake()
    {
        // 漂浮位置完全由脚本算，不希望物理再施加重力或速度，所以设成运动学刚体
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            Debug.LogError("肉块预制体上没有 Rigidbody2D，无法漂浮也无法被拾取。请在预制体上补一个 Rigidbody2D。", this);
            enabled = false;
            return;
        }

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;
        // 物理是固定 50Hz 步进，插值让贴图在两步之间平滑过渡，否则慢速漂移会看出轻微顿挫
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 拾取判定走 OnTriggerEnter2D，没有勾了 Is Trigger 的碰撞体就永远捡不到
        Collider2D pickupBox = GetComponent<Collider2D>();
        if ((pickupBox == null || !pickupBox.isTrigger) && !_hasReportedMissingCollider)
        {
            Debug.LogWarning("肉块预制体上没有勾选 Is Trigger 的 Collider2D，肉块能漂浮但玩家捡不到。", this);
            _hasReportedMissingCollider = true;
        }

        _driftDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
    }

    private void Start()
    {
        // 只有设置了正的存活时间才启用自动清理，避免肉块无限堆积
        if (_lifeTime > 0f)
        {
            Destroy(gameObject, _lifeTime);
        }
    }

    // 漂浮移动放在 FixedUpdate：位置要通过刚体驱动，而刚体是按物理步长更新的
    private void FixedUpdate()
    {
        float waterLineY = WaterSurface.LineY;
        // 以刚体的位置为基准而不是 transform.position：关掉自动同步后，
        // 刚体位置才是物理世界里的真实位置
        Vector2 position = _rb.position;

        // 水平方向始终缓慢漂移，看起来像被水流推着走
        position.x += _driftDirection * _driftSpeed * Time.fixedDeltaTime;

        if (_isFloating)
        {
            // 已经浮在水面：贴着水面线做小幅正弦起伏
            _bobTimer += Time.fixedDeltaTime;
            position.y = waterLineY + Mathf.Sin(_bobTimer * _bobFrequency) * _bobAmplitude;
        }
        else
        {
            // 掉落点可能在水下也可能在空中，先把肉块送到水面高度
            position.y = Mathf.MoveTowards(position.y, waterLineY, _settleSpeed * Time.fixedDeltaTime);
            _isFloating = Mathf.Approximately(position.y, waterLineY);
        }

        // 关键：必须用 MovePosition 而不是写 transform.position。
        // 本项目 Physics2D 的 Auto Sync Transforms 是关闭的，直接改 transform 只会移动贴图，
        // 碰撞体会留在出生点不动 —— 表现就是肉块看着浮上来了，游过去却怎么都捡不到
        _rb.MovePosition(position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 玩家碰到肉块：广播拾取事件后销毁自己，金币在事件处理里增加
        if (other.CompareTag("Player"))
        {
            OnCollected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
