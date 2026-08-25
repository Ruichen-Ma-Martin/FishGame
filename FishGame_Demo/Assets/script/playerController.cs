using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [SerializeField] private GameObject _hand;

    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _moveSpeed = 5f;
    // 速度变化率：越大越"干脆"，越小越像在水里滑行（松手后的漂移距离更长）
    [SerializeField] private float _acceleration = 40f;
    private bool _isfaceright = true;

    private float _shootCooldown = 0.5f;
    private float _lastShootTime = 0f;

    void Awake()
    {
        // 水中悬浮：关掉重力，玩家的垂直位置完全由输入决定，不会自己往下掉
        if (_rb != null)
        {
            _rb.gravityScale = 0f;
        }
    }

    void Update()
    {
        HandleMouse();
        Shoot();
        Movement();

        _lastShootTime += Time.deltaTime;
    }

    // 让手（枪口）朝向鼠标：把鼠标屏幕坐标转成世界坐标后求角度
    void HandleMouse()
    {

        //Vector3 handposition = _hand.transform.position;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3 dir = mouseWorldPos - _hand.transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _hand.transform.rotation = Quaternion.Euler(0, 0, angle-90);
        
    }
    void Shoot()
    {
        if (Input.GetMouseButtonDown(0) && _lastShootTime >= _shootCooldown)
        {
            GameController.instance.weapon.Shoot();
            _lastShootTime = 0f; 
        }
    }
    
    void Movement()
    {
        // 水平：A/D 或 ←/→；垂直：W/S 或 ↑/↓。用 Raw 取值，松手立刻归零，衰减由下面统一处理
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // 斜向输入要归一化，否则对角线移动会比直线快约 1.41 倍
        Vector2 inputDirection = new Vector2(horizontalInput, verticalInput);
        if (inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }

        // 没有输入时目标速度为 0，MoveTowards 让速度逐渐衰减到停下，形成短距离水中漂移
        Vector2 targetVelocity = inputDirection * _moveSpeed;
        _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetVelocity, _acceleration * Time.deltaTime);

        // 记录朝向，供后续翻转贴图/动画使用；输入为 0 时保持上一次朝向
        if ( horizontalInput > 0 && !_isfaceright)
        {
            _isfaceright = true;
            //Debug.Log("face right");
        }
        else if (horizontalInput < 0 && _isfaceright)
        {
            _isfaceright = false;
            //Debug.Log("face left");
        }
    }
}
