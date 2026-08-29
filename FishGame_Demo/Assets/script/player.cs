using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class player : MonoBehaviour
{
    // 数值配置表：血量上限、治疗量等常量都从这里读，需在 Inspector 里挂上 PlayerStats 资源
    [SerializeField] private PlayerStats_SO _stats;
    private float _health;   // 当前血量：运行时状态，初始值在 Start 里由配置表赋予
    [SerializeField] private TMP_Text _CoinsNumber;
    [SerializeField] private TMP_Text _HPNumber;
    public enemyattack enemyattack;

    public float _Coins = 0;   // 当前金币：运行时状态，不进配置表

    // 无敌帧剩余时间：冲刺期间由 playerController 调用 SetInvincible 设置
    private float _invincibleTimer;

    // 无敌帧尚未走完时不受伤
    public bool IsInvincible => _invincibleTimer > 0f;

    // 由外部（冲刺）设置无敌时长；取较大值避免覆盖尚未结束的无敌
    public void SetInvincible(float duration)
    {
        _invincibleTimer = Mathf.Max(_invincibleTimer, duration);
    }

    void Start()
    {
        // 血量上限来自配置表，避免把数值写死在代码里
        _health = _stats.maxHealth;

        // 金币来源改为拾取肉块，不再在昆虫死亡的瞬间直接结算
        Flesh.OnCollected += GetCoinFromFlesh;
        _CoinsNumber.text = _Coins.ToString();
        _HPNumber.text = _health.ToString();
    }

    private void OnDestroy()
    {
        // 静态事件必须反注册：玩家死亡重载场景后，旧的处理函数还挂在事件上，
        // 会去访问已经销毁的 UI 文本而报空引用
        Flesh.OnCollected -= GetCoinFromFlesh;
    }
    private void Update()
    {
        // 无敌帧倒计时：到 0 后恢复可受伤
        if (_invincibleTimer > 0f)
        {
            _invincibleTimer -= Time.deltaTime;
        }

        _CoinsNumber.text = _Coins.ToString();
        _HPNumber.text = _health.ToString();
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
        _HPNumber.text = _health.ToString();
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

    // 捡到一块肉：金币 +1 并刷新显示
    void GetCoinFromFlesh(Flesh flesh)
    {
        _Coins++;
        _CoinsNumber.text = _Coins.ToString();
    }


}
