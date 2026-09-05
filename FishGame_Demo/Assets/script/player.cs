using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class player : MonoBehaviour
{
    // 数值配置表：血量上限、治疗量等常量都从这里读，需在 Inspector 里挂上 PlayerStats 资源
    [SerializeField] private PlayerStats_SO _stats;
    private float _health;   // 当前血量：运行时状态，初始值在 Start 里由配置表赋予
    public enemyattack enemyattack;

    public float _Coins = 0;   // 当前金币：运行时状态，不进配置表

    // 理智（SAN）：两个都是运行时状态，不写回 SO。上限做成变量而不是直读 SO，
    // 是为了让商店以后能永久提升上限，又不会把升级效果存进配置表资源里
    private float _maxSan;      // 当前 SAN 上限（运行时，初始 = SO 的 maxSan，商店可增长）
    private float _currentSan;  // 当前 SAN（运行时）

    // 无敌帧剩余时间：冲刺期间由 playerController 调用 SetInvincible 设置
    private float _invincibleTimer;

    // 无敌帧尚未走完时不受伤
    public bool IsInvincible => _invincibleTimer > 0f;

    // 给 HUD 用的只读入口：UI 只能读数值，改不了状态，
    // 免得有人绕过受伤 / 治疗 / 拾取逻辑直接改血量或货币
    public float CurrentHealth => _health;
    public float MaxHealth => _stats.maxHealth;
    public float CurrentFlesh => _Coins;   // 血肉（货币）：类型跟随 _Coins，是 float
    public float CurrentSan => _currentSan;
    public float MaxSan => _maxSan;

    // 由外部（冲刺）设置无敌时长；取较大值避免覆盖尚未结束的无敌
    public void SetInvincible(float duration)
    {
        _invincibleTimer = Mathf.Max(_invincibleTimer, duration);
    }

    void Start()
    {
        // 血量上限来自配置表，避免把数值写死在代码里
        _health = _stats.maxHealth;

        // SAN 上限同样从配置表拷一份到运行时变量，开局给满。
        // 目前只提供给 HUD 显示，消耗 / 回复机制是后续任务
        _maxSan = _stats.maxSan;
        _currentSan = _maxSan;

        // 金币来源改为拾取肉块，不再在昆虫死亡的瞬间直接结算
        Flesh.OnCollected += GetCoinFromFlesh;
    }

    private void OnDestroy()
    {
        // 静态事件必须反注册：玩家死亡重载场景后，旧的处理函数还挂在事件上，
        // 下次触发就会访问到已经销毁的对象
        Flesh.OnCollected -= GetCoinFromFlesh;
    }
    private void Update()
    {
        // 无敌帧倒计时：到 0 后恢复可受伤。
        // 血量 / 血肉的显示已经交给 PlayerUI 每帧读属性刷新，这里不再碰 UI
        if (_invincibleTimer > 0f)
        {
            _invincibleTimer -= Time.deltaTime;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("enemyhitbox"))
        {
           //Debug.Log("Player hit by enemy!");
            TakeDamage();
        }
        
    }
    void TakeDamage()
    {
        if (IsInvincible)
        {
            return;   // 无敌帧内不受伤
        }

        StartCoroutine(GameController.instance.GetDamageEffect.DamageEffect());
        _health -= enemyattack._damage;
        if (_health <= 0)
        {
            Die();
        }
    }
    private void Die()
    {
        Destroy(gameObject,0.2f);
        GameController.instance.BackToMain();
    }
    // 回血：回复量由配置表决定
    public void healing()
    {
        _health += _stats.healingAmount;
    }

    // 捡到一块肉：血肉 +1。显示由 PlayerUI 每帧读 CurrentFlesh 刷新，这里不用管
    void GetCoinFromFlesh(Flesh flesh)
    {
        _Coins++;
    }


}
