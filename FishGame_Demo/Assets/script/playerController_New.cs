using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class playerController_New : MonoBehaviour
{
    [SerializeField] private GameObject _hand;

    [SerializeField] private Rigidbody2D _rb;
    // 数值配置表：移动速度、加速度、转向速度、最大倾斜角、射击冷却都从这里读，需在 Inspector 里挂上 PlayerStats 资源
    [SerializeField] private PlayerStats_SO _stats;

    // 朝向：由 A/D 决定，按 D 为 true、按 A 为 false。松手保持上一次朝向。
    private bool _isFaceRight = true;
    // 当前鱼头倾斜角（度）：等于实际写到 rotation.z 的角，不是每帧累加的增量
    private float _currentTilt;
    // 鱼头方向（前进箭头）：移动和射击都用它，保证"看到的朝向"和"实际飞的方向"一致
    private Vector2 _forward = Vector2.right;
    // 原始缩放：镜像时只翻转 X 的正负，避免把美术尺寸改成 1 而缩放走形
    private Vector3 _baseScale = Vector3.one;

    private float _lastShootTime = 0f;   // 距上次射击的累计时间：运行时状态，不进配置表
    // 拿不到武器时只报一次错，避免每次点击都刷满 Console
    private bool _hasReportedMissingWeapon;

    private bool _isDashing;          // 是否正在冲刺
    private float _dashTimer;         // 冲刺剩余时间
    private float _dashCooldownTimer; // 冲刺冷却剩余时间
    private Vector2 _dashDirection;   // 冲刺方向（触发瞬间锁定）

    void Awake()
    {
        // 记下场景里配置的原始缩放，翻转朝向时以它为基准
        _baseScale = transform.localScale;

        // 水中悬浮：关掉重力，玩家的垂直位置完全由输入决定，不会自己往下掉
        if (_rb != null)
        {
            _rb.gravityScale = 0f;
        }

        // 没有配置表就整个停掉：移动、转向、射击全都要读它，继续跑只会得到一堆无头绪的空引用报错
        if (_stats == null)
        {
            Debug.LogError("playerController_New 的 _stats 没有赋值，请在 Inspector 里挂上 PlayerStats 资源。脚本已停用。", this);
            enabled = false;
            return;
        }

        if (_rb == null)
        {
            Debug.LogError("playerController_New 的 _rb 没有赋值，请在 Inspector 里挂上玩家的 Rigidbody2D。脚本已停用。", this);
            enabled = false;
            return;
        }

        // 计时器从冷却时间起算，否则进场后头一个冷却周期内的点击会被冷却判断吞掉
        _lastShootTime = _stats.shootCooldown;
    }

    void Update()
    {
        // 顺序有讲究：先按输入定左右，HandleAiming 才能算出本帧正确的 _forward，Shoot / HandleDash / Movement 再用它
        UpdateFacing();
        HandleAiming();
        Shoot();
        HandleDash();
        Movement();

        _lastShootTime += Time.deltaTime;
    }

    // 左右朝向由移动输入决定：按 D 朝右，按 A 朝左，松手时保持上一次的朝向
    void UpdateFacing()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        if (moveInput > 0f)
        {
            _isFaceRight = true;
        }
        else if (moveInput < 0f)
        {
            _isFaceRight = false;
        }
    }

    // 鱼头瞄准：A/D 决定左右镜像；鼠标在身前时瞄上下；鼠标在身后时保持朝右那套倾角
    void HandleAiming()
    {
        // 0) 没有 MainCamera 就没法把鼠标换算到世界坐标，此时保持上一帧朝向
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        // 1) 鼠标相对鱼的方向（世界坐标）
        Vector2 mouseWorldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorldPos - (Vector2)transform.position;

        // 2) 目标倾角：永远夹在 ±maxTiltAngle，不要用 180-maxTilt
        //    _currentTilt 就是最终写到 rotation.z 的角
        float worldAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float maxTilt = _stats.maxTiltAngle;
        float targetTilt;
        if (_isFaceRight)
        {
            targetTilt = Mathf.Clamp(worldAngle, -maxTilt, maxTilt);
        }
        else if (dir.x <= 0f)
        {
            // 朝左且鼠标在左（身前）：局部倾角取负，镜像后鱼头上下跟鼠标一致
            float localAngle = Mathf.Atan2(dir.y, -dir.x) * Mathf.Rad2Deg;
            targetTilt = -Mathf.Clamp(localAngle, -maxTilt, maxTilt);
        }
        else
        {
            // 朝左且鼠标在右（身后）：保持朝右那套 ±maxTilt，不取负、不追鼠标往左上
            targetTilt = Mathf.Clamp(worldAngle, -maxTilt, maxTilt);
        }

        // 3) 平滑旋转：匀速靠近目标角（值都在 ±maxTilt 内，不会碰到 ±180 绕远路）
        _currentTilt = Mathf.MoveTowards(_currentTilt, targetTilt, _stats.turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, _currentTilt);

        // 4) 左右镜像：只翻 scale.x，旋转角已经是世界 z
        float scaleX = Mathf.Abs(_baseScale.x);
        float signX = _isFaceRight ? 1f : -1f;
        transform.localScale = new Vector3(signX * scaleX, _baseScale.y, _baseScale.z);

        // 5) 前进方向 = 先缩放再旋转后，局部 +X（鱼头）的世界方向
        //    朝右：(cos, sin)；朝左且保持 +θ：(-cos, -sin)，与视觉头一致
        float tiltRad = _currentTilt * Mathf.Deg2Rad;
        _forward = new Vector2(signX * Mathf.Cos(tiltRad), signX * Mathf.Sin(tiltRad));

        // 6) 枪口对齐鱼头方向：子弹沿 _forward 飞，贴图局部朝上所以减 90 度
        if (_hand != null)
        {
            float forwardAngle = Mathf.Atan2(_forward.y, _forward.x) * Mathf.Rad2Deg;
            _hand.transform.rotation = Quaternion.Euler(0f, 0f, forwardAngle - 90f);
        }
    }

    // 开火：冷却时间由配置表决定，子弹沿鱼头方向飞出
    void Shoot()
    {
        if (!Input.GetMouseButtonDown(0) || _lastShootTime < _stats.shootCooldown)
        {
            return;
        }

        Weapon weapon = GameController.instance != null ? GameController.instance.weapon : null;
        if (weapon == null)
        {
            if (!_hasReportedMissingWeapon)
            {
                Debug.LogError("开火失败：场景里没有挂 GameController 的物体，或者它的 weapon 字段没连上武器。", this);
                _hasReportedMissingWeapon = true;
            }
            return;
        }

        weapon.Shoot(_forward);
        _lastShootTime = 0f;
    }
    
    // 冲刺：按 Shift 沿当前鱼头方向爆发加速，冷却期间不能再触发
    void HandleDash()
    {
        // 冷却递减
        if (_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer -= Time.deltaTime;
        }

        // 触发：按 Shift，且冷却结束
        bool shiftPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        if (shiftPressed && _dashCooldownTimer <= 0f)
        {
            _isDashing = true;
            _dashTimer = _stats.dashDuration;
            _dashCooldownTimer = _stats.dashCooldown;
            _dashDirection = _forward;   // 朝鱼头方向冲，方向在触发瞬间锁定

            // 触发无敌帧（player 和 playerController 在同一物体上）
            player p = GetComponent<player>();
            if (p != null)
            {
                p.SetInvincible(_stats.invincibleTime);
            }
        }

        // 冲刺计时：持续时间到就结束
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                _isDashing = false;
            }
        }
    }

    // 只沿鱼头方向游动：没有横向平移，也没有 W/S 上下移动
    void Movement()
    {
        // 冲刺中：速度直接锁定为冲刺速度，方向保持触发时的鱼头方向，不随鼠标再转
        if (_isDashing)
        {
            _rb.linearVelocity = _dashDirection * _stats.dashSpeed;
            return;
        }

        // 1) 输入：A/D 只决定"游不游"，往左还是往右已经由 UpdateFacing 折进 _forward 里了。
        //    这里取绝对值，否则按 A 会变成"面朝左、却沿反方向往右飘"
        float moveInput = Mathf.Abs(Input.GetAxisRaw("Horizontal"));

        // 2) 移动方向 = 鱼头方向 × 输入（只有前进，没有横向）
        Vector2 move = _forward * moveInput;

        // 3) 平滑过渡到目标速度（保留原有 acceleration 手感，松手后仍会滑行一小段）
        _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, move * _stats.moveSpeed, _stats.acceleration * Time.deltaTime);
    }
}
