using System;
using UnityEngine;

// 昆虫死亡后掉落的肉块：先移动到水面高度，然后浮在水面上随水流缓慢漂移，
// 玩家碰到就消失并广播拾取事件（加金币由 player.cs 负责）。
public class Flesh : MonoBehaviour
{
    // 被拾取时广播。用静态事件而不是直接调 GameController.instance.player，
    // 和 enemy.OnEnemyDeath、bullet.BulletExplosion 保持同一套写法
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

    private void Awake()
    {
        // 位置由脚本直接控制。预制体上如果挂了刚体，就改成运动学并清掉重力，
        // 否则物理模拟会和脚本设置的位置互相打架
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
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

    private void Update()
    {
        float waterLineY = WaterSurface.LineY;
        Vector3 position = transform.position;

        // 水平方向始终缓慢漂移，看起来像被水流推着走
        position.x += _driftDirection * _driftSpeed * Time.deltaTime;

        if (_isFloating)
        {
            // 已经浮在水面：贴着水面线做小幅正弦起伏
            _bobTimer += Time.deltaTime;
            position.y = waterLineY + Mathf.Sin(_bobTimer * _bobFrequency) * _bobAmplitude;
        }
        else
        {
            // 掉落点可能在水下也可能在空中，先把肉块送到水面高度
            position.y = Mathf.MoveTowards(position.y, waterLineY, _settleSpeed * Time.deltaTime);
            _isFloating = Mathf.Approximately(position.y, waterLineY);
        }

        transform.position = position;
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
