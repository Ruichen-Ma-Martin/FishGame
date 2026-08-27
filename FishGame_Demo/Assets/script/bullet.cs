using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 弹道完全靠 Rigidbody2D 的速度驱动，缺了刚体子弹只会原地不动，所以在这里强制要求
[RequireComponent(typeof(Rigidbody2D))]
public class bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 100f;

    [Header("水下：无重力，靠阻力慢慢停住")]
    // 水下每秒衰减掉的速度值（线性衰减），越大停得越快、射程越短
    [SerializeField] private float _underwaterDrag = 12f;
    // 速度衰减到该值以下就认为水流已经把水弹打散，直接销毁
    [SerializeField] private float _stopSpeed = 1.5f;

    [Header("水上：有重力，无阻力")]
    // 出水后的重力倍率，决定抛物线的弯曲程度
    [SerializeField] private float _airGravityScale = 1f;

    // 保险时间：任何情况下子弹都不会永久留在场景里
    [SerializeField] private float _maxLifeTime = 5f;

    private Rigidbody2D _rb;
    // 上一帧是否在水面之上，用来检测"穿过水面"这一瞬间
    private bool _wasAboveWater;
    // 是否已经由 Weapon 指定过发射方向，避免 Start 里的兜底逻辑覆盖掉
    private bool _isLaunched;

    public float _damage = 1f;
    public static Action<bullet> BulletExplosion;

    // 预制体缺组件时每颗子弹都会报一次，用静态标记压成一次
    private static bool _hasReportedMissingCollider;

    void Awake()
    {
        // 在 Awake 里缓存，保证 Instantiate 之后立刻调用 Launch 也能拿到刚体
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
        {
            Debug.LogError("子弹预制体上没有 Rigidbody2D，无法发射。请在预制体上补一个 Rigidbody2D。", this);
            enabled = false;
            Destroy(gameObject);
            return;
        }

        // 命中判定走 OnTriggerEnter2D，没有勾了 Is Trigger 的碰撞体就永远打不中敌人
        Collider2D hitBox = GetComponent<Collider2D>();
        if ((hitBox == null || !hitBox.isTrigger) && !_hasReportedMissingCollider)
        {
            Debug.LogWarning("子弹预制体上没有勾选 Is Trigger 的 Collider2D，子弹能飞但打不中敌人。", this);
            _hasReportedMissingCollider = true;
        }
    }

    void Start()
    {
        // 兜底：如果不是通过 Weapon.Shoot 生成的（没人调用 Launch），仍按自身 up 方向飞出去
        if (!_isLaunched)
        {
            Launch(transform.up);
        }

        Destroy(gameObject, _maxLifeTime);
    }

    // 由武器调用：按给定方向发射，并根据出生点在水上还是水下设置初始物理状态
    public void Launch(Vector2 direction)
    {
        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        // 仍然没有刚体说明预制体缺组件，Awake 里已经报过错并安排销毁，这里静默退出，
        // 不要再抛一次空引用把真正有用的提示淹掉
        if (_rb == null)
        {
            return;
        }

        // 方向为零时（例如鼠标正好压在枪口上）退回自身 up，避免子弹原地不动
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.up;
        }

        _rb.linearVelocity = direction.normalized * _speed;
        _isLaunched = true;

        _wasAboveWater = transform.position.y > WaterSurface.LineY;
        ApplyEnvironment(_wasAboveWater);
        FaceVelocity();
    }

    void FixedUpdate()
    {
        bool isAboveWater = transform.position.y > WaterSurface.LineY;

        // 从水上落回水面：视为溅落入水，子弹在这里消失
        if (_wasAboveWater && !isAboveWater)
        {
            Destroy(gameObject);
            return;
        }

        // 跨过水面线的那一帧立刻切换物理表现（水下无重力 / 水上有重力）
        if (isAboveWater != _wasAboveWater)
        {
            ApplyEnvironment(isAboveWater);
            _wasAboveWater = isAboveWater;
        }

        if (!isAboveWater)
        {
            // 水下：把速度线性衰减到 0，速度足够小就销毁，形成"射程有限"的直线弹道
            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, Vector2.zero, _underwaterDrag * Time.fixedDeltaTime);
            if (_rb.linearVelocity.magnitude <= _stopSpeed)
            {
                Destroy(gameObject);
                return;
            }
        }

        FaceVelocity();
    }

    // 水上给重力走抛物线，水下关掉重力走直线（阻力在 FixedUpdate 里手动施加）
    void ApplyEnvironment(bool isAboveWater)
    {
        _rb.gravityScale = isAboveWater ? _airGravityScale : 0f;
    }

    // 让贴图朝向当前飞行方向，水上抛物线下落时才不会看起来是横着飞
    void FaceVelocity()
    {
        Vector2 velocity = _rb.linearVelocity;
        if (velocity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 命中敌人：水上水下都一样处理，先广播命中事件再销毁
        if (other.CompareTag("enemy"))
        {
            BulletExplosion?.Invoke(this);
            Destroy(gameObject);
        }
    }


}
