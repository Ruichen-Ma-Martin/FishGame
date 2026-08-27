using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [SerializeField] private GameObject _hand;

    [SerializeField] private Rigidbody2D _rb;
    // 数值配置表：移动速度、加速度、转向速度、最大倾斜角、射击冷却都从这里读，需在 Inspector 里挂上 PlayerStats 资源
    [SerializeField] private PlayerStats_SO _stats;

    // 朝向：鼠标在鱼右边为 true。保留此标记，供后续翻转贴图/动画使用
    private bool _isFaceRight = true;
    // 当前鱼头倾斜角（度）：从水平方向算起的"绝对角度"，不是每帧累加的增量
    private float _currentTilt;
    // 鱼头方向（前进箭头）：移动和射击都用它，保证"看到的朝向"和"实际飞的方向"一致
    private Vector2 _forward = Vector2.right;
    // 原始缩放：镜像时只翻转 X 的正负，避免把美术尺寸改成 1 而缩放走形
    private Vector3 _baseScale = Vector3.one;

    private float _lastShootTime = 0f;   // 距上次射击的累计时间：运行时状态，不进配置表
    // 拿不到武器时只报一次错，避免每次点击都刷满 Console
    private bool _hasReportedMissingWeapon;

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
            Debug.LogError("playerController 的 _stats 没有赋值，请在 Inspector 里挂上 PlayerStats 资源。脚本已停用。", this);
            enabled = false;
            return;
        }

        // 计时器从冷却时间起算，否则进场后头一个冷却周期内的点击会被冷却判断吞掉
        _lastShootTime = _stats.shootCooldown;
    }

    void Update()
    {
        // 顺序有讲究：先算朝向，Shoot 和 Movement 才能用到本帧最新的 _forward
        HandleAiming();
        Shoot();
        Movement();

        _lastShootTime += Time.deltaTime;
    }

    // 鱼头朝向鼠标：上下只在 +/- maxTiltAngle 内倾斜，左右靠镜像实现，永远不会倒立
    void HandleAiming()
    {
        // 0) 没有 MainCamera 就没法把鼠标换算到世界坐标，此时保持上一帧朝向
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        // 1) 鼠标在鱼的位置（世界坐标）
        Vector2 mouseWorldPos = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorldPos - (Vector2)transform.position;

        // 2) 判断鼠标在鱼左边还是右边：右边朝右，左边朝左（镜像）
        _isFaceRight = dir.x >= 0f;

        // 3) 计算目标倾斜角
        //    atan2 像"指南针"，告诉你鼠标在哪个方向（返回一个角度）
        //    用 Mathf.Abs(dir.x) 是让角度始终算在"朝右"的范围，左右交给第 2 步的镜像
        float rawAngle = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;

        // 4) 限位：把角度夹在 -maxTiltAngle ~ +maxTiltAngle 之间（防止倒立）
        //    重要：rawAngle 是"从水平方向算起的绝对角度"，不是"相对当前角度的增量"。
        //    所以鱼头转到 60° 就是上限，绝不会"转 30° 后再累加 60° 变成 90°"。
        float targetTilt = Mathf.Clamp(rawAngle, -_stats.maxTiltAngle, _stats.maxTiltAngle);

        // 5) 平滑旋转：鱼头"慢慢转过去"，不是瞬间指向
        //    MoveTowards 从当前值"匀速靠近"目标值（目标值永远是绝对角度，不会越转越多）
        _currentTilt = Mathf.MoveTowards(_currentTilt, targetTilt, _stats.turnSpeed * Time.deltaTime);

        // 6) 应用：旋转倾斜角 + 左右镜像
        //    朝左时旋转角要取负：Unity 的变换是"先缩放再旋转"，镜像后若仍用 +tilt，
        //    上下会颠倒（鼠标在左上，鱼头却指向左下）。取负后鱼头才真正指着鼠标，鱼背保持朝上。
        float appliedTilt = _isFaceRight ? _currentTilt : -_currentTilt;
        transform.rotation = Quaternion.Euler(0, 0, appliedTilt);

        float scaleX = Mathf.Abs(_baseScale.x);
        transform.localScale = new Vector3(_isFaceRight ? scaleX : -scaleX, _baseScale.y, _baseScale.z);

        // 7) 鱼头方向（前进箭头）
        //    cos/sin 把"角度"翻译成"箭头指向哪"（向右走多少、向上走多少）
        //    朝左时只把水平分量取反，垂直分量保持不变，才能和第 6 步的镜像结果完全一致
        float tiltRad = _currentTilt * Mathf.Deg2Rad;
        _forward = new Vector2(Mathf.Cos(tiltRad), Mathf.Sin(tiltRad));
        if (!_isFaceRight)
        {
            _forward.x = -_forward.x;
        }

        // 8) 枪口对齐鱼头方向：子弹是沿 _forward 飞的，枪口也跟着转才不会"指向和弹道不一致"
        //    贴图局部朝上，所以要减 90 度
        if (_hand != null)
        {
            float forwardAngle = Mathf.Atan2(_forward.y, _forward.x) * Mathf.Rad2Deg;
            _hand.transform.rotation = Quaternion.Euler(0, 0, forwardAngle - 90f);
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
    
    // 只沿鱼头方向前后游动：没有横向平移，也没有 W/S 上下移动
    void Movement()
    {
        // 1) 输入：D = 前进(+1)，A = 后退(-1)
        float moveInput = Input.GetAxisRaw("Horizontal");

        // 2) 移动方向 = 鱼头方向 × 输入（只有前后，没有横向）
        Vector2 move = _forward * moveInput;

        // 3) 平滑过渡到目标速度（保留原有 acceleration 手感，松手后仍会滑行一小段）
        _rb.linearVelocity = Vector2.MoveTowards(
            _rb.linearVelocity, move * _stats.moveSpeed, _stats.acceleration * Time.deltaTime);
    }
}
