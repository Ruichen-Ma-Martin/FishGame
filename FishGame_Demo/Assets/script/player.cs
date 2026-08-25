using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class player : MonoBehaviour
{
    private float _health = 5f;
    [SerializeField] private TMP_Text _CoinsNumber;
    [SerializeField] private TMP_Text _HPNumber;
    public enemyattack enemyattack;

    public float _Coins = 0;

    void Start()
    {
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
    public void healing()
    {
        _health += 2f;
    }

    // 捡到一块肉：金币 +1 并刷新显示
    void GetCoinFromFlesh(Flesh flesh)
    {
        _Coins++;
        _CoinsNumber.text = _Coins.ToString();
    }


}
