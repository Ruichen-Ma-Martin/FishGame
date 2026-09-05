using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class player : MonoBehaviour
{
    // 数值配置表：血量上限、治疗量等常量都从这里读，需在 Inspector 里挂上 PlayerStats 资源
    [SerializeField] private PlayerStats_SO _stats;
    private float _health;   // 当前血量：运行时状态，初始值在 Start 里由配置表赋予

    // 受伤的画面效果（暗角）。改成 Inspector 注入，不再通过 GameController 中转
    [SerializeField] private GetDamageEffect _damageEffect;

    // 血肉（货币）：运行时状态，不进配置表。改名后加 FormerlySerializedAs，
    // 是为了让场景里原本存在 _Coins 名下的数值不会因为改名而丢失
    [SerializeField, FormerlySerializedAs("_Coins")] private float _flesh;

    // 玩家死亡时广播，由 GameController 决定后续流程（回主菜单）。
    // 玩家自己不该知道"死了要加载哪个场景"，那是场景流程的职责
    public static Action OnPlayerDeath;

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
    public float CurrentFlesh => _flesh;   // 血肉（货币）
    public float CurrentSan => _currentSan;
    public float MaxSan => _maxSan;

    // 由外部（冲刺）设置无敌时长；取较大值避免覆盖尚未结束的无敌
    public void SetInvincible(float duration)
    {
        _invincibleTimer = Mathf.Max(_invincibleTimer, duration);
    }

    // 依赖检查：漏连引用等到受伤那一刻才空引用就很难查，开局先报清楚
    private void Awake()
    {
        if (_stats == null)
        {
            Debug.LogError("player 的 _stats 没有赋值，请在 Inspector 里挂上 PlayerStats 资源。脚本已停用。", this);
            enabled = false;
            return;
        }

        if (_damageEffect == null)
        {
            // 只是画面效果缺失，扣血照常，所以报警告而不是停用脚本
            Debug.LogWarning("player 的 _damageEffect 没有赋值，受伤时不会有暗角效果。", this);
        }
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

        // 受伤改由敌人攻击方广播：伤害值随事件传进来，玩家不再持有 enemyattack 引用
        enemyattack.OnPlayerHit += TakeDamage;
    }

    private void OnDestroy()
    {
        // 静态事件必须反注册：玩家死亡重载场景后，旧的处理函数还挂在事件上，
        // 下次触发就会访问到已经销毁的对象
        Flesh.OnCollected -= GetCoinFromFlesh;
        enemyattack.OnPlayerHit -= TakeDamage;
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
    // 受伤：伤害值由 enemyattack.OnPlayerHit 事件传入，不再自己去读攻击方的字段。
    // 碰撞检测也统一在攻击方那一侧，玩家不再监听 enemyhitbox，避免一次攻击被结算两遍
    void TakeDamage(float damage)
    {
        if (IsInvincible)
        {
            return;   // 无敌帧内不受伤
        }

        // 受伤效果是可选的，没连线也不该影响扣血
        if (_damageEffect != null)
        {
            StartCoroutine(_damageEffect.DamageEffect());
        }

        _health -= damage;
        if (_health <= 0)
        {
            Die();
        }
    }

    // 死亡：只负责销毁自己并广播事件，回主菜单交给监听方（GameController）
    private void Die()
    {
        Destroy(gameObject, 0.2f);
        OnPlayerDeath?.Invoke();
    }
    // 回血：回复量由配置表决定
    public void healing()
    {
        _health += _stats.healingAmount;
    }

    // 捡到一块肉：血肉 +1。显示由 PlayerUI 每帧读 CurrentFlesh 刷新，这里不用管
    void GetCoinFromFlesh(Flesh flesh)
    {
        _flesh++;
    }

    // 花掉血肉：扣除逻辑收在 player 内部，商店只管调用，不直接改字段。
    // 夹到 0 起步，避免调用方漏做余额检查时把血肉扣成负数
    public void SpendFlesh(float amount)
    {
        _flesh = Mathf.Max(0f, _flesh - amount);
    }


}
