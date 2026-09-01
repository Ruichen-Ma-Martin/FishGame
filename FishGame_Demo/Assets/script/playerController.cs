using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    // 数值配置表：移动速度、加速度、鼠标灵敏度、最大倾斜角、射击冷却都从这里读，需在 Inspector 里挂上 PlayerStats 资源
    [SerializeField] private PlayerStats_SO _stats;

    // 朝向：由 A/D 决定，按 D 为 true、按 A 为 false。松手保持上一次朝向。
    private bool _isFaceRight = true;
    // 当前鱼头倾斜角（度）：物体真实的 Z 轴旋转值，夹在 ±maxTiltAngle。
    // 朝左时鼠标增量按相反方向累加，保证两个朝向下都是"鼠标往上、鱼头就抬起"
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

        // 开局先按水下悬浮关重力；之后每帧由 HandleWaterPhysics 按水面上下切换
        if (_rb != null)
        {
            _rb.gravityScale = 0f;
        }

        // 没有配置表就整个停掉：移动、转向、射击全都要读它，继续跑只会得到一堆无头绪的空引用报错
        if (_stats == null)
        {
            Debug.LogError("playerController 的 _stats 没有赋值，请在 Inspector 里挂上 PlayerStats 资源。脚本已停用。", this);
            enabled = false;
            return;
        }

        if (_rb == null)
        {
            Debug.LogError("playerController 的 _rb 没有赋值，请在 Inspector 里挂上玩家的 Rigidbody2D。脚本已停用。", this);
            enabled = false;
            return;
        }

        // 计时器从冷却时间起算，否则进场后头一个冷却周期内的点击会被冷却判断吞掉
        _lastShootTime = _stats.shootCooldown;

        // 鼠标隐藏并锁定：上下移动只用来抬头/低头，不显示光标（FPS 式）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 顺序有讲究：先按输入定左右，HandleAiming 才能算出本帧正确的 _forward，Shoot / HandleDash / Movement 再用它
        UpdateFacing();
        HandleAiming();
        Shoot();
        HandleDash();
        HandleWaterPhysics();
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

    // 鱼头瞄准：鼠标上下移动（增量）累加到倾斜角，左右镜像仍由 A/D 决定
    void HandleAiming()
    {
        // 1) 读取鼠标上下移动的增量（忽略左右，左右归 A/D 管）
        //    Mouse Y 向上为正，向下为负
        float mouseY = Input.GetAxis("Mouse Y");

        // 2) 累加到当前倾角（直接累加，FPS 手感：鼠标动多少、头转多少）
        //    鼠标本身是连续输入，所以不需要额外的 MoveTowards 平滑
        if(_isFaceRight)
        {
            _currentTilt += mouseY * _stats.mouseSensitivity;
        }
        else{
            _currentTilt -= mouseY * _stats.mouseSensitivity;
        }
        //_currentTilt += mouseY * _stats.mouseSensitivity;

        // 3) 限位：夹在 -maxTiltAngle ~ +maxTiltAngle，防止倒立
        _currentTilt = Mathf.Clamp(_currentTilt, -_stats.maxTiltAngle, _stats.maxTiltAngle);

        // 4) 应用旋转：倾角写到 rotation.z
        //    Unity 是先缩放再旋转。朝左 scale.x 为负，若仍用 +tilt，抬头会变成低头，所以朝左取负
        //float appliedTilt = _isFaceRight ? _currentTilt : -_currentTilt;
        transform.rotation = Quaternion.Euler(0f, 0f, _currentTilt);

        // 5) 左右镜像：瞬间翻转 scale.x（A/D 已经更新 _isFaceRight）
       float scaleX = Mathf.Abs(_baseScale.x);
        float signX = _isFaceRight ? 1f : -1f;
        transform.localScale = new Vector3(signX * scaleX, _baseScale.y, _baseScale.z);
        
         //float yRot = _isFaceRight ? 0f : 180f;
        //transform.rotation = Quaternion.Euler(0f, yRot, _currentTilt);

        // 6) 前进方向 = 鱼头贴图真正指向的方向，子弹、游动、冲刺都用它
        //    Unity 先缩放再旋转：朝左时 scale.x 为负，等于把鼻子方向整体转了 180°，
        //    所以水平和垂直分量都要乘 signX；只翻水平分量会让朝左时上下瞄准反掉
        float tiltRad = _currentTilt * Mathf.Deg2Rad;
        _forward = signX * new Vector2(Mathf.Cos(tiltRad), Mathf.Sin(tiltRad));
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
            // 用 TryGetComponent 而不是 GetComponent：后者取不到组件时仍会产生一次托管分配
            if (TryGetComponent(out player p))
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

    // 水面物理切换：水上开重力（把鱼拉回水面），水下关重力（悬浮）
    void HandleWaterPhysics()
    {
        bool isAboveWater = transform.position.y > WaterSurface.LineY;
        _rb.gravityScale = isAboveWater ? _stats.waterAirGravity : 0f;
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

        // 水面以上：不响应 A/D，只受重力做抛物线，自然落回水面
        if (transform.position.y > WaterSurface.LineY)
        {
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
